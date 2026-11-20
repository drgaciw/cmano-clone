using System.Text.Json;
using ProjectAegis.Data.Scenario.Policy;
using NUnit.Framework;

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

    [Test]
    public void Full_gauntlet_whitelist_binds_without_key_loss()
    {
        var dto = JsonSerializer.Deserialize<ScenarioPolicyJsonDto>(FullGauntletPolicy, Options);
        Assert.That(dto, Is.Not.Null);
        var g = dto!.Gauntlet;
        Assert.That(g, Is.Not.Null);

        Assert.That(g!.Intent, Is.EqualTo("patrol"));
        Assert.That(g.Oracle, Is.EqualTo("blue wins"));
        Assert.That(g.CatalogRefs, Is.EqualTo(["k-31-visby-2009"]));
        Assert.That(g.RunId, Is.EqualTo("run-1"));
        Assert.That(g.Tier, Is.EqualTo(2));
        Assert.That(g.DimensionsClaimed, Is.EqualTo(["emcon", "roe"]));

        var unit = ProjectAegis.Data.Tests.AssertEx.Single(g.Units!);
        Assert.That(unit.UnitId, Is.EqualTo("u1"));
        Assert.That(unit.PlatformId, Is.EqualTo("k-31-visby-2009"));
        Assert.That(unit.Domain, Is.EqualTo("surface"));
        Assert.That(unit.Side, Is.EqualTo("blue"));

        Assert.That(g.Expect, Is.Not.Null);
        Assert.That(g.Expect!.Side, Is.EqualTo("BLUE"));
        Assert.That(g.Expect.MinKills, Is.EqualTo(1));
        Assert.That(g.Expect.MaxMissilesFired, Is.EqualTo(5));
        Assert.That(g.Expect.MinDenials, Is.EqualTo(2));
        Assert.That(g.Expect.MaxDenials, Is.EqualTo(10));
        Assert.That(g.Expect.MinScore, Is.EqualTo(50.0));
        Assert.That(g.Expect.MaxScore, Is.EqualTo(90.0));
        Assert.That(g.Expect.RequireNonEmptyFingerprint, Is.True);
        Assert.That(g.Expect.RequireFingerprintSubstrings, Is.EqualTo(["CommsStateChange"]));
        Assert.That(g.Expect.RequireTrueLaunchedShooters, Is.EqualTo(["u1"]));

        Assert.That(g.ExpectCi, Is.Not.Null);
        Assert.That(g.ExpectCi!.Side, Is.EqualTo("BLUE"));
        Assert.That(g.ExpectCi.MinKills, Is.EqualTo(1));
        Assert.That(g.ExpectCi.RequireNonEmptyFingerprint, Is.False);

        Assert.That(g.ExpectProvenance!.Value.ValueKind, Is.EqualTo(JsonValueKind.String));
        Assert.That(g.ExpectProvenance.Value.GetString(), Is.EqualTo("derived from batch CSV"));
        Assert.That(g.ExpectCiProvenance!.Value.ValueKind, Is.EqualTo(JsonValueKind.Object));
        Assert.That(g.ExpectCiProvenance.Value.GetProperty("csv").GetString(), Is.EqualTo("ci.csv"));
        Assert.That(g.Forge!.Value.ValueKind, Is.EqualTo(JsonValueKind.Object));
        Assert.That(g.Forge.Value.GetProperty("recipe").GetString(), Is.EqualTo("r1"));
    }
}
