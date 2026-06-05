namespace ProjectAegis.Delegation.UnityAdapter.Tests.Baltic;

using ProjectAegis.Delegation.UnityAdapter.Baltic;
using NUnit.Framework;

[TestFixture]
public sealed class ReplayGoldenBalticEngageTests
{
    private static string GoldenPath =>
        ReplayGoldenAssertions.ResolveRegressionGoldenPath("replay-golden-baltic-engage-2026-06-02.txt");

    [Test]
    public void Baltic_patrol_matches_golden_world_hash_and_outcome_lines()
    {
        var golden = File.ReadAllLines(GoldenPath);
        var result = BalticReplayHarness.Run(42, "baltic-patrol", ticks: 4);
        ReplayGoldenAssertions.AssertPinnedHashes(result, golden);
        var actualOutcomes = result.Fingerprint
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Where(l => l.StartsWith("EngagementOutcome|", StringComparison.Ordinal))
            .ToArray();

        Assert.That(actualOutcomes.Length, Is.GreaterThanOrEqualTo(1));
        Assert.That(actualOutcomes[0], Does.Contain("|Kill|"));
        Assert.That(result.Fingerprint, Does.Contain("Engage:1:High"));
        Assert.That(result.Fingerprint, Does.Contain("ContactChange|"));
        Assert.That(result.Fingerprint, Does.Contain("|Detected|Lost"));
    }
}