using ProjectAegis.Delegation.UnityAdapter.Baltic;
using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Baltic;

public sealed class BalticReplayHarnessFuelTests
{
    [Test]
    public void Comms_burn_scenario_emits_fuel_state_changes_when_ticks_cross_bands()
    {
        var a = BalticReplayHarness.Run(7, "baltic-patrol-comms", ticks: 100, mvpEngagement: true);
        var b = BalticReplayHarness.Run(7, "baltic-patrol-comms", ticks: 100, mvpEngagement: true);
        Assert.That(a.Fingerprint, Is.EqualTo(b.Fingerprint));
        Assert.That(a.FingerprintSha256, Is.EqualTo(b.FingerprintSha256));
        Assert.That(a.FingerprintSha256, Has.Length.EqualTo(64));
        Assert.That(a.Fingerprint, Does.Contain("FuelStateChange"));
        Assert.That(a.Messages.Count(m => m.Category == "FUEL"), Is.GreaterThanOrEqualTo(1));
    }

    [Test]
    public void Patrol_without_burn_model_has_no_fuel_state_change_rows()
    {
        var shortRun = BalticReplayHarness.Run(42, "baltic-patrol", ticks: 6, mvpEngagement: true);
        Assert.That(shortRun.Fingerprint, Does.Not.Contain("FuelStateChange"));
    }
}