using System.Text.Json;
using ProjectAegis.Data.Scenario;
using ProjectAegis.Data.Scenario.Policy;
using Xunit;

namespace ProjectAegis.Data.Tests.Scenario;

/// <summary>
/// BUG-t2-escort-passive-emcon-claim-unimplemented: the shipped T2 escort-passive
/// policy must not advertise a passive-EMCON stand-in while every detection trial
/// is perfect <c>(basePd=1.0, envMask=1.0)</c>.
/// </summary>
public sealed class GauntletT2EscortPassiveEmconStandinTests
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private const string AllOnesPolicy = """
        {
          "id": "gauntlet-t2-escort-passive",
          "detection": [
            { "observerId": "k-22-gavle-ex-goteborg-class", "sensorId": "radar-1", "targetId": "mrk-shkval-pr-22800-karakurt", "contactId": "c-0322-1", "basePd": 1.0, "envMask": 1.0 },
            { "observerId": "k-21-goteborg", "sensorId": "radar-1", "targetId": "skr-admiral-grigorovich-pr-1135-6m", "contactId": "c-0322-2", "basePd": 1.0, "envMask": 1.0 }
          ]
        }
        """;

    [Fact]
    public void All_perfect_detection_pairs_are_not_a_passive_emcon_standin()
    {
        var dto = JsonSerializer.Deserialize<ScenarioPolicyJsonDto>(AllOnesPolicy, Options);
        Assert.NotNull(dto);
        Assert.False(
            HasReducedPassiveStandIn(dto!),
            "origin/main defect shape: every (basePd, envMask) pair is (1.0, 1.0)");
    }

    [Fact]
    public void Shipped_gauntlet_t2_escort_passive_is_not_all_perfect_detection()
    {
        var dir = ScenarioDataPaths.TryResolveScenariosDirectory();
        Assert.NotNull(dir);
        var path = Path.Combine(dir!, "gauntlet-t2-escort-passive.policy.json");
        Assert.True(File.Exists(path), path);

        var dto = JsonSerializer.Deserialize<ScenarioPolicyJsonDto>(File.ReadAllText(path), Options);
        Assert.NotNull(dto);
        Assert.Equal("gauntlet-t2-escort-passive", dto!.Id);
        Assert.NotNull(dto.Detection);
        Assert.NotEmpty(dto.Detection);

        Assert.True(
            HasReducedPassiveStandIn(dto),
            "gauntlet-t2-escort-passive still ships only (basePd=1.0, envMask=1.0) — BUG-t2-escort-passive-emcon-claim-unimplemented");

        Assert.Contains(
            dto.Detection!,
            t => t.BasePd is >= 0.3 and <= 0.45 && t.EnvMask is >= 0.3 and <= 0.45);
    }

    private static bool HasReducedPassiveStandIn(ScenarioPolicyJsonDto dto)
    {
        if (dto.Detection is null || dto.Detection.Count == 0)
        {
            return false;
        }

        var pairs = dto.Detection
            .Select(t => (t.BasePd, t.EnvMask))
            .Distinct()
            .ToArray();
        return pairs.Length > 0 && !pairs.All(p => p.BasePd == 1.0 && p.EnvMask == 1.0);
    }
}
