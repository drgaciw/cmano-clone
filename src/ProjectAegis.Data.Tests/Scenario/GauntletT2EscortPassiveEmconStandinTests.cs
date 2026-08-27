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
    public void Shipped_gauntlet_t2_escort_passive_has_reduced_detection_on_every_blue_trial()
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

        var blueTrials = GetBlueDetectionTrials(dto);
        Assert.Equal(4, blueTrials.Length);

        Assert.True(
            HasReducedPassiveStandIn(dto),
            "gauntlet-t2-escort-passive still ships perfect (basePd=1.0, envMask=1.0) on one or more blue trials — BUG-t2-escort-passive-emcon-claim-unimplemented");

        Assert.All(
            blueTrials,
            t => Assert.True(
                IsReducedPassiveStandInPair(t.BasePd, t.EnvMask),
                $"blue trial observer={t.ObserverId} must use reduced basePd/envMask (~0.3–0.45), got ({t.BasePd}, {t.EnvMask})"));
    }

    private const double ReducedStandInMin = 0.3;
    private const double ReducedStandInMax = 0.45;

    private static bool IsReducedPassiveStandInPair(double basePd, double envMask) =>
        basePd is >= ReducedStandInMin and <= ReducedStandInMax &&
        envMask is >= ReducedStandInMin and <= ReducedStandInMax;

    private static ScenarioDetectionJsonDto[] GetBlueDetectionTrials(ScenarioPolicyJsonDto dto)
    {
        if (dto.Detection is null || dto.Detection.Count == 0)
        {
            return Array.Empty<ScenarioDetectionJsonDto>();
        }

        var blueObservers = dto.Gauntlet?.Units?
            .Where(u => string.Equals(u.Side, "blue", StringComparison.OrdinalIgnoreCase))
            .Select(u => u.UnitId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToHashSet(StringComparer.Ordinal);

        return blueObservers is { Count: > 0 }
            ? dto.Detection.Where(t => blueObservers.Contains(t.ObserverId)).ToArray()
            : dto.Detection.ToArray();
    }

    private static bool HasReducedPassiveStandIn(ScenarioPolicyJsonDto dto)
    {
        var blueTrials = GetBlueDetectionTrials(dto);
        return blueTrials.Length > 0 &&
               blueTrials.All(t => IsReducedPassiveStandInPair(t.BasePd, t.EnvMask));
    }
}
