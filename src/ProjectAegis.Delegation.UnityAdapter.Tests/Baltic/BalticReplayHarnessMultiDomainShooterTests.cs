using ProjectAegis.Delegation.UnityAdapter.Baltic;
using ProjectAegis.Sim.Scenario;
using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Baltic;

/// <summary>
/// True multi-domain engage: catalog air + subsurface blues must launch at catalog red
/// (not detection-only CATALOG_UNIT registration).
/// </summary>
[TestFixture]
public sealed class BalticReplayHarnessMultiDomainShooterTests
{
    private const string PolicyId = "gauntlet-multidomain-shooters";
    private const string AirId = "jas-39c-gripen-2005";
    private const string SubId = "a-19-gotland-2022";
    private const string SurfaceId = "k-31-visby-2009";
    private const string RedId = "em-sovremenny-i-pr-956-sarych";

    [Test]
    public void Multi_domain_air_and_sub_launch_at_catalog_red_side_correct()
    {
        ScenarioPolicyRepository.EnsureDefaultJsonLoaded();
        Assert.That(
            ScenarioPolicyRepository.TryGet(PolicyId),
            Is.Not.Null,
            $"policy {PolicyId} must load from data/scenarios");

        var result = BalticReplayHarness.Run(
            seed: 42,
            scenarioPolicyId: PolicyId,
            ticks: 10,
            mvpEngagement: true);

        Assert.That(result.Fingerprint, Is.Not.Null.And.Not.Empty);
        var tokens = SplitFingerprint(result.Fingerprint);
        var fireOrder = BalticReplayHarness.ResolveFireOrder(null, result.DecisionLog);
        var events = string.Join(" ", fireOrder);

        Assert.That(events, Does.Contain($"CATALOG_UNIT:{AirId}:air"));
        Assert.That(events, Does.Contain($"CATALOG_UNIT:{SubId}:subsurface"));
        Assert.That(events, Does.Contain($"CATALOG_UNIT:{SurfaceId}:surface"));
        Assert.That(events, Does.Contain($"MAGAZINE_SEED:{AirId}:"));
        Assert.That(events, Does.Contain($"MAGAZINE_SEED:{SubId}:"));

        var airLaunches = 0;
        var subLaunches = 0;
        var surfaceLaunches = 0;
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
            Assert.That(GauntletCatalogSides.IsRed(victim!), Is.True,
                $"blue launch must hit catalog red: {shooter}->{victim}");

            if (shooter == AirId)
            {
                airLaunches++;
                Assert.That(victim, Is.EqualTo("mpk-steregushchiy-pr-20380-2018"));
            }
            else if (shooter == SubId)
            {
                subLaunches++;
                Assert.That(victim, Is.EqualTo("mrk-buyan-pr-21630-buyan-2007"));
            }
            else if (shooter == SurfaceId)
            {
                surfaceLaunches++;
                Assert.That(victim, Is.EqualTo(RedId));
            }
        }

        Assert.That(airLaunches, Is.GreaterThan(0),
            "air catalog unit must produce ≥1 True|Launched against red");
        Assert.That(subLaunches, Is.GreaterThan(0),
            "subsurface catalog unit must produce ≥1 True|Launched against red");
        Assert.That(surfaceLaunches, Is.GreaterThanOrEqualTo(0)); // surface may also shoot
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
