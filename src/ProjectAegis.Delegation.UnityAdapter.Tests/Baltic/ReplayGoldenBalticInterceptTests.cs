namespace ProjectAegis.Delegation.UnityAdapter.Tests.Baltic;

using ProjectAegis.Delegation.UnityAdapter.Baltic;
using NUnit.Framework;
using ProjectAegis.Delegation.Projection;

[TestFixture]
public sealed class ReplayGoldenBalticInterceptTests
{
    private static string GoldenPath =>
        Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "..",
            "tests", "regression", "replay-golden-baltic-intercept-2026-06-02.txt"));

    [Test]
    public void Intercept_scenario_matches_golden_hashes_and_outcome_lines()
    {
        var golden = File.ReadAllLines(GoldenPath);
        var expectedWorldHash = ulong.Parse(
            golden.First(l => l.StartsWith("WORLD_HASH=", StringComparison.Ordinal))["WORLD_HASH=".Length..]);
        var expectedDetectionHash = ulong.Parse(
            golden.First(l => l.StartsWith("DETECTION_WORLD_HASH=", StringComparison.Ordinal))[
                "DETECTION_WORLD_HASH=".Length..]);
        var expectedOutcomes = golden
            .Where(l => l.StartsWith("EngagementOutcome|", StringComparison.Ordinal))
            .ToArray();

        var result = BalticReplayHarness.Run(42, "baltic-patrol-intercept", ticks: 4);
        var actualOutcomes = result.Fingerprint
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Where(l => l.StartsWith("EngagementOutcome|", StringComparison.Ordinal))
            .ToArray();

        Assert.That(result.WorldHash, Is.EqualTo(expectedWorldHash));
        Assert.That(result.DetectionWorldHash, Is.EqualTo(expectedDetectionHash));
        Assert.That(actualOutcomes, Is.EqualTo(expectedOutcomes));
        Assert.That(actualOutcomes, Has.All.Contains("|Intercept|"));
        Assert.That(result.Fingerprint, Does.Not.Contain("|Kill|"));
        Assert.That(result.Fingerprint, Does.Contain("Engage:1:High"));
        Assert.That(result.Messages.Any(m => m.Category == "INTERCEPT_SUCCESS"), Is.True);
        Assert.That(result.Messages.Any(m => m.Category == "KILL_CONFIRMED"), Is.False);
    }
}