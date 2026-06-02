namespace ProjectAegis.Delegation.UnityAdapter.Tests.Baltic;

using ProjectAegis.Delegation.UnityAdapter.Baltic;
using NUnit.Framework;

[TestFixture]
public sealed class ReplayGoldenBalticEngageTests
{
    private static string GoldenPath =>
        Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "..",
            "tests", "regression", "replay-golden-baltic-engage-2026-06-02.txt"));

    [Test]
    public void Baltic_patrol_matches_golden_world_hash_and_outcome_lines()
    {
        var golden = File.ReadAllLines(GoldenPath);
        var worldHashLine = golden.First(l => l.StartsWith("WORLD_HASH=", StringComparison.Ordinal));
        var expectedWorldHash = ulong.Parse(worldHashLine["WORLD_HASH=".Length..]);
        var expectedDetectionHash = ulong.Parse(
            golden.First(l => l.StartsWith("DETECTION_WORLD_HASH=", StringComparison.Ordinal))["DETECTION_WORLD_HASH=".Length..]);

        var result = BalticReplayHarness.Run(42, "baltic-patrol", ticks: 4);
        var actualOutcomes = result.Fingerprint
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Where(l => l.StartsWith("EngagementOutcome|", StringComparison.Ordinal))
            .ToArray();

        Assert.That(result.WorldHash, Is.EqualTo(expectedWorldHash));
        Assert.That(result.DetectionWorldHash, Is.EqualTo(expectedDetectionHash));
        Assert.That(actualOutcomes.Length, Is.GreaterThanOrEqualTo(1));
        Assert.That(actualOutcomes[0], Does.Contain("|Kill|"));
        Assert.That(result.Fingerprint, Does.Contain("Engage:1:High"));
        Assert.That(result.Fingerprint, Does.Contain("ContactChange|"));
        Assert.That(result.Fingerprint, Does.Contain("|Detected|Lost"));
    }
}