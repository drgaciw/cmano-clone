namespace ProjectAegis.Delegation.UnityAdapter.Tests.Baltic;

using ProjectAegis.Delegation.UnityAdapter.Baltic;
using NUnit.Framework;

[TestFixture]
public sealed class BalticReplayHarnessScenarioGoldenTests
{
    [TestCase("baltic-patrol-catalog", 4, "replay-golden-baltic-catalog-2026-06-02.txt")]
    [TestCase("baltic-patrol-mission", 4, "replay-golden-baltic-mission-2026-06-02.txt")]
    [TestCase("baltic-patrol-replay", 4, "replay-golden-baltic-replay-2026-06-02.txt")]
    public void Scenario_fingerprint_and_world_hash_are_stable(string scenario, int ticks, string? goldenFile)
    {
        var a = BalticReplayHarness.Run(42, scenario, ticks);
        var b = BalticReplayHarness.Run(42, scenario, ticks);

        Assert.That(b.Fingerprint, Is.EqualTo(a.Fingerprint));
        Assert.That(b.WorldHash, Is.EqualTo(a.WorldHash));
        Assert.That(b.DetectionWorldHash, Is.EqualTo(a.DetectionWorldHash));
        Assert.That(a.Fingerprint, Is.Not.Empty);

        if (goldenFile == null)
        {
            return;
        }

        var golden = File.ReadAllLines(ResolveGoldenPath(goldenFile));
        var expectedWorld = ulong.Parse(golden.First(l => l.StartsWith("WORLD_HASH=", StringComparison.Ordinal))["WORLD_HASH=".Length..]);
        var expectedDetection = ulong.Parse(golden.First(l => l.StartsWith("DETECTION_WORLD_HASH=", StringComparison.Ordinal))["DETECTION_WORLD_HASH=".Length..]);
        Assert.That(a.WorldHash, Is.EqualTo(expectedWorld));
        Assert.That(a.DetectionWorldHash, Is.EqualTo(expectedDetection));
    }

    private static string ResolveGoldenPath(string fileName) =>
        Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "..",
            "tests", "regression", fileName));
}