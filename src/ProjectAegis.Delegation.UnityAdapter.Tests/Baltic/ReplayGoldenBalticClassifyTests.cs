namespace ProjectAegis.Delegation.UnityAdapter.Tests.Baltic;

using ProjectAegis.Delegation.UnityAdapter.Baltic;
using NUnit.Framework;

[TestFixture]
public sealed class ReplayGoldenBalticClassifyTests
{
    private static string GoldenPath =>
        Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "..",
            "tests", "regression", "replay-golden-baltic-classify-2026-06-02.txt"));

    [Test]
    public void Classify_scenario_emits_classified_and_identified_without_engage()
    {
        var a = BalticReplayHarness.Run(42, "baltic-patrol-classify", ticks: 4, mvpEngagement: false);
        var b = BalticReplayHarness.Run(42, "baltic-patrol-classify", ticks: 4, mvpEngagement: false);

        Assert.That(b.Fingerprint, Is.EqualTo(a.Fingerprint));
        Assert.That(b.WorldHash, Is.EqualTo(a.WorldHash));
        Assert.That(a.Fingerprint, Does.Contain("|Classified|Identified"));
        Assert.That(a.Fingerprint, Does.Contain("|Detected|Classified"));
        Assert.That(a.EngagementCount, Is.EqualTo(0));

        var golden = File.ReadAllLines(GoldenPath);
        var expectedWorld = ulong.Parse(
            golden.First(l => l.StartsWith("WORLD_HASH=", StringComparison.Ordinal))["WORLD_HASH=".Length..]);
        var expectedDetection = ulong.Parse(
            golden.First(l => l.StartsWith("DETECTION_WORLD_HASH=", StringComparison.Ordinal))[
                "DETECTION_WORLD_HASH=".Length..]);
        Assert.That(a.WorldHash, Is.EqualTo(expectedWorld));
        Assert.That(a.DetectionWorldHash, Is.EqualTo(expectedDetection));
    }
}