using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Baltic;
using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Bridge;

[TestFixture]
public sealed class SensorC2PanelBinderIntegrationTests
{
    [Test]
    public void Baltic_patrol_binder_produces_contact_row_for_hud()
    {
        var result = BalticReplayHarness.Run(42, "baltic-patrol", ticks: 2, mvpEngagement: false);
        var panel = SensorC2PanelBinder.Bind(result.SensorC2);

        Assert.That(panel.ContactRows, Is.Not.Empty);
        Assert.That(panel.EmconLabel, Does.StartWith("EMCON:"));
    }

    [Test]
    public void Baltic_patrol_classify_binder_shows_lifecycle_states()
    {
        var result = BalticReplayHarness.Run(42, "baltic-patrol-classify", ticks: 4, mvpEngagement: false);
        var panel = SensorC2PanelBinder.Bind(result.SensorC2);

        Assert.That(panel.ContactRows, Is.Not.Empty);
        Assert.That(
            panel.ContactRows.Select(r => r.LifecycleState),
            Does.Contain("Classified").Or.Contain("Identified"));
        Assert.That(panel.ContactRows[0].DisplayLine, Does.Contain(panel.ContactRows[0].LifecycleState));
    }
}