using System.Text.Json;
using ProjectAegis.Data.Scenario.Policy;
using Xunit;

namespace ProjectAegis.Data.Tests.Scenario;

/// <summary>
/// A1 — full gauntlet.* whitelist must bind onto <see cref="ScenarioGauntletJsonDto"/>
/// (key ownership; provenance/forge stay loosely typed).
/// </summary>
public sealed class ScenarioGauntletJsonDtoTests
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private const string FullGauntletPolicy = """
        {
          "id": "gauntlet-schema-bind",
          "friendlyRoe": "WeaponsFree",
          "gauntlet": {
            "intent": "patrol",
            "oracle": "blue wins",
            "catalogRefs": ["k-31-visby-2009"],
            "units": [
              {
                "unitId": "u1",
                "platformId": "k-31-visby-2009",
                "domain": "surface",
                "side": "blue"
              }
            ],
            "expect": {
              "side": "BLUE",
              "minKills": 1,
              "maxMissilesFired": 5,
              "minDenials": 2,
              "maxDenials": 10,
              "minScore": 50.0,
              "maxScore": 90.0,
              "requireNonEmptyFingerprint": true,
              "requireFingerprintSubstrings": ["CommsStateChange"],
              "requireTrueLaunchedShooters": ["u1"]
            },
            "expectCi": {
              "side": "BLUE",
              "minKills": 1,
              "maxMissilesFired": 3,
              "minDenials": 0,
              "maxDenials": 4,
              "minScore": 40.0,
              "maxScore": 80.0,
              "requireNonEmptyFingerprint": false
            },
            "runId": "run-1",
            "tier": 2,
            "expectProvenance": "derived from batch CSV",
            "expectCiProvenance": { "csv": "ci.csv", "note": "smoke" },
            "dimensionsClaimed": ["emcon", "roe"],
            "forge": { "recipe": "r1", "candidateId": "c1" }
          }
        }
        """;

    [Fact]
    public void Full_gauntlet_whitelist_binds_without_key_loss()
    {
        var dto = JsonSerializer.Deserialize<ScenarioPolicyJsonDto>(FullGauntletPolicy, Options);
        Assert.NotNull(dto);
        var g = dto!.Gauntlet;
        Assert.NotNull(g);

        Assert.Equal("patrol", g!.Intent);
        Assert.Equal("blue wins", g.Oracle);
        Assert.Equal(["k-31-visby-2009"], g.CatalogRefs);
        Assert.Equal("run-1", g.RunId);
        Assert.Equal(2, g.Tier);
        Assert.Equal(["emcon", "roe"], g.DimensionsClaimed);

        var unit = Assert.Single(g.Units!);
        Assert.Equal("u1", unit.UnitId);
        Assert.Equal("k-31-visby-2009", unit.PlatformId);
        Assert.Equal("surface", unit.Domain);
        Assert.Equal("blue", unit.Side);

        Assert.NotNull(g.Expect);
        Assert.Equal("BLUE", g.Expect!.Side);
        Assert.Equal(1, g.Expect.MinKills);
        Assert.Equal(5, g.Expect.MaxMissilesFired);
        Assert.Equal(2, g.Expect.MinDenials);
        Assert.Equal(10, g.Expect.MaxDenials);
        Assert.Equal(50.0, g.Expect.MinScore);
        Assert.Equal(90.0, g.Expect.MaxScore);
        Assert.True(g.Expect.RequireNonEmptyFingerprint);
        Assert.Equal(["CommsStateChange"], g.Expect.RequireFingerprintSubstrings);
        Assert.Equal(["u1"], g.Expect.RequireTrueLaunchedShooters);

        Assert.NotNull(g.ExpectCi);
        Assert.Equal("BLUE", g.ExpectCi!.Side);
        Assert.Equal(1, g.ExpectCi.MinKills);
        Assert.False(g.ExpectCi.RequireNonEmptyFingerprint);

        Assert.Equal(JsonValueKind.String, g.ExpectProvenance!.Value.ValueKind);
        Assert.Equal("derived from batch CSV", g.ExpectProvenance.Value.GetString());
        Assert.Equal(JsonValueKind.Object, g.ExpectCiProvenance!.Value.ValueKind);
        Assert.Equal("ci.csv", g.ExpectCiProvenance.Value.GetProperty("csv").GetString());
        Assert.Equal(JsonValueKind.Object, g.Forge!.Value.ValueKind);
        Assert.Equal("r1", g.Forge.Value.GetProperty("recipe").GetString());
    }
}
