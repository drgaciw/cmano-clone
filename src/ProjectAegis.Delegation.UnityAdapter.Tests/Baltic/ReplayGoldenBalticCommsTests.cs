namespace ProjectAegis.Delegation.UnityAdapter.Tests.Baltic;

using ProjectAegis.Delegation.UnityAdapter.Baltic;
using NUnit.Framework;

[TestFixture]
public sealed class ReplayGoldenBalticCommsTests
{
    private static string GoldenPath =>
        Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "..",
            "tests", "regression", "replay-golden-baltic-comms-2026-06-02.txt"));

    [Test]
    public void Comms_scenario_fingerprint_and_denials_are_stable()
    {
        var a = BalticReplayHarness.Run(42, "baltic-patrol-comms", ticks: 6, mvpEngagement: true);
        var b = BalticReplayHarness.Run(42, "baltic-patrol-comms", ticks: 6, mvpEngagement: true);

        Assert.That(b.Fingerprint, Is.EqualTo(a.Fingerprint));
        Assert.That(b.WorldHash, Is.EqualTo(a.WorldHash));
        Assert.That(a.Fingerprint, Does.Contain("CommsStateChange"));
        Assert.That(a.Fingerprint, Does.Contain("CommsDenied"));
        Assert.That(a.Messages.Count(m => m.Category == "COMMS"), Is.EqualTo(2));

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