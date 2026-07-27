using ProjectAegis.Delegation.UnityAdapter.Baltic;
using ProjectAegis.Sim.Scenario;
using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Baltic;

/// <summary>
/// Slice 2: joint-orbat-smoke + tier-3 ladder policy multi-domain True|Launched.
/// </summary>
[TestFixture]
public sealed class BalticReplayHarnessLadderMultiDomainTests
{
    private const string AirId = "jas-39c-gripen-2005";
    private const string SubId = "a-19-gotland-2022";

    // Expanded gauntlet ORBATs may pair air/sub shooters at any catalog red (Russia nationality).
    private static bool IsCatalogRed(string id) => GauntletCatalogSides.IsRed(id);

    [TestCase("gauntlet-joint-orbat-smoke")]
    [TestCase("gauntlet-t3-emcon-phases")]
    public void Ladder_or_joint_policy_air_and_sub_launch_at_distinct_catalog_reds(string scenarioId)
    {
        ScenarioPolicyRepository.EnsureDefaultJsonLoaded();
        Assert.That(ScenarioPolicyRepository.TryGet(scenarioId), Is.Not.Null, scenarioId);

        var result = BalticReplayHarness.Run(42, scenarioId, ticks: 10, mvpEngagement: true);
        var tokens = SplitFingerprint(result.Fingerprint);
        var fireOrder = BalticReplayHarness.ResolveFireOrder(null, result.DecisionLog);
        var events = string.Join(" ", fireOrder);

        Assert.That(events, Does.Contain($"CATALOG_UNIT:{AirId}:air"));
        Assert.That(events, Does.Contain($"CATALOG_UNIT:{SubId}:subsurface"));
        Assert.That(events, Does.Contain($"MAGAZINE_SEED:{AirId}:"));
        Assert.That(events, Does.Contain($"MAGAZINE_SEED:{SubId}:"));

        var airLaunches = 0;
        var subLaunches = 0;
        string? airVictim = null;
        string? subVictim = null;
        for (var i = 0; i < tokens.Count; i++)
        {
            var e = tokens[i];
            if (!e.StartsWith("Engagement|", StringComparison.Ordinal)
                || !e.Contains("|True|Launched", StringComparison.Ordinal))
            {
                continue;
            }

            var shooter = e.Split('|')[4];
            var victim = FindNextOutcomeVictim(tokens, i + 1);
            Assert.That(victim, Is.Not.Null, e);
            Assert.That(IsCatalogRed(victim!), Is.True, $"{shooter}->{victim}");

            if (shooter == AirId)
            {
                airLaunches++;
                airVictim = victim;
            }
            else if (shooter == SubId)
            {
                subLaunches++;
                subVictim = victim;
            }
        }

        Assert.That(airLaunches, Is.GreaterThan(0), $"{scenarioId}: air must True|Launched");
        Assert.That(subLaunches, Is.GreaterThan(0), $"{scenarioId}: sub must True|Launched");
        Assert.That(airVictim, Is.Not.EqualTo(subVictim),
            "air and sub must engage distinct reds (salvo deconfliction)");
    }

    private static IReadOnlyList<string> SplitFingerprint(string fingerprint) =>
        fingerprint.Split(['\n', '\r', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static string? FindNextOutcomeVictim(IReadOnlyList<string> tokens, int from)
    {
        for (var j = from; j < tokens.Count; j++)
        {
            if (tokens[j].StartsWith("EngagementOutcome|", StringComparison.Ordinal))
            {
                var parts = tokens[j].Split('|');
                return parts.Length >= 6 ? parts[5] : null;
            }

            if (tokens[j].StartsWith("Engagement|", StringComparison.Ordinal))
            {
                break;
            }
        }

        return null;
    }
}
