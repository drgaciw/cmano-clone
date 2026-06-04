namespace ProjectAegis.Delegation.UnityAdapter.Tests.Baltic;

using ProjectAegis.Delegation.UnityAdapter.Baltic;
using NUnit.Framework;

[TestFixture]
public sealed class ReplayGoldenBalticCommsTests
{
    private static string GoldenPath =>
        ReplayGoldenAssertions.ResolveRegressionGoldenPath("replay-golden-baltic-comms-2026-06-02.txt");

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

        ReplayGoldenAssertions.AssertPinnedHashes(a, File.ReadAllLines(GoldenPath));
    }
}