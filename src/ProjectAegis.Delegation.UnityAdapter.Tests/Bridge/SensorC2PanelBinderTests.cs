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
}