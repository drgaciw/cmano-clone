using ProjectAegis.Delegation.UnityAdapter.Baltic;
using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Baltic;

public sealed class BalticReplayHarnessCommsTests
{
    [Test]
    public void Comms_scenario_logs_denied_and_blocks_repeat_fingerprint()
    {
        var a = BalticReplayHarness.Run(7, "baltic-patrol-comms", ticks: 6, mvpEngagement: true);
        var b = BalticReplayHarness.Run(7, "baltic-patrol-comms", ticks: 6, mvpEngagement: true);
        Assert.That(a.Fingerprint, Is.EqualTo(b.Fingerprint));
        Assert.That(a.Fingerprint, Does.Contain("CommsStateChange"));
        Assert.That(a.Messages.Count(m => m.Category == "COMMS"), Is.EqualTo(2));
    }

    [Test]
    public void Comms_denied_appends_policy_denial_for_engage()
    {
        var result = BalticReplayHarness.Run(7, "baltic-patrol-comms", ticks: 6, mvpEngagement: true);
        Assert.That(result.Fingerprint, Does.Contain("PolicyDenial"));
        Assert.That(result.Fingerprint, Does.Contain("CommsDenied").Or.Contains("RoeHoldFire"));
    }
}