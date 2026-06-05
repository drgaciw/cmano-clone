using ProjectAegis.Delegation.UnityAdapter.Baltic;
using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Bridge;

[TestFixture]
public sealed class SensorC2BridgeTests
{
    [Test]
    public void Baltic_patrol_sensor_c2_lists_detected_contact_with_emcon_active()
    {
        var result = BalticReplayHarness.Run(42, "baltic-patrol", ticks: 2, mvpEngagement: false);
        Assert.That(result.SensorC2.ActiveContactCount, Is.GreaterThan(0));
        Assert.That(result.SensorC2.Contacts, Is.Not.Empty);
        Assert.That(result.SensorC2.Contacts[0].LifecycleState, Is.EqualTo("Detected"));
        Assert.That(result.SensorC2.ObserverRadarEmconActive, Is.True);
        Assert.That(result.SensorC2.HasFireControlTrackOnPrimary, Is.True);
    }
}