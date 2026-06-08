using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Baltic;
using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Bridge;

/// <summary>
/// SensorC2 HUD strip vs C2LeftDrawer Contacts tab both bind the same LastSensorC2 snapshot;
/// selection is presentation-only via contact id (no duplicate selection state).
/// </summary>
public sealed class C2ContactsOverlapTests
{
    [Test]
    public void Classify_scenario_drawer_and_hud_binders_share_contact_ids()
    {
        var result = BalticReplayHarness.Run(7, "baltic-patrol-classify", ticks: 10, mvpEngagement: false);
        var hud = SensorC2PanelBinder.Bind(result.SensorC2);
        var drawer = SensorC2PanelBinder.Bind(result.SensorC2);

        Assert.That(hud.ContactRows.Select(r => r.ContactId), Is.EqualTo(drawer.ContactRows.Select(r => r.ContactId)));
        Assert.That(hud.ContactRows, Is.Not.Empty);

        var contactId = hud.ContactRows[0].ContactId;
        var summary = ContactSummaryProjection.Project(contactId, result.SensorC2.Contacts);
        Assert.That(summary, Is.Not.Null);
    }
}