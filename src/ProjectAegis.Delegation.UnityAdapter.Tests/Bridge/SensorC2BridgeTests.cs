using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Baltic;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
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

    [Test]
    public void Baltic_patrol_classify_sensor_c2_shows_classified_then_identified()
    {
        var result = BalticReplayHarness.Run(42, "baltic-patrol-classify", ticks: 4, mvpEngagement: false);

        Assert.That(result.SensorC2.Contacts, Is.Not.Empty);
        Assert.That(result.SensorC2.Contacts.Select(c => c.LifecycleState), Does.Contain("Classified").Or.Contain("Identified"));
        Assert.That(result.SensorC2.ObserverRadarEmconActive, Is.True);
        Assert.That(result.SensorC2.HasFireControlTrackOnPrimary, Is.True);
    }

    [Test]
    public void BindPanel_matches_binder_for_baltic_patrol_snapshot()
    {
        var result = BalticReplayHarness.Run(42, "baltic-patrol", ticks: 2, mvpEngagement: false);
        var expected = SensorC2PanelBinder.Bind(result.SensorC2);
        var viaBridge = SensorC2Bridge.BindPanel(result.SensorC2);

        Assert.That(viaBridge.EmconLabel, Is.EqualTo(expected.EmconLabel));
        Assert.That(viaBridge.TrackLabel, Is.EqualTo(expected.TrackLabel));
        Assert.That(viaBridge.ContactCountLabel, Is.EqualTo(expected.ContactCountLabel));
        Assert.That(viaBridge.ContactRows.Select(r => r.DisplayLine), Is.EqualTo(expected.ContactRows.Select(r => r.DisplayLine)));
    }

    [Test]
    public void BindPanel_uses_injected_panel_bridge_when_set()
    {
        var result = BalticReplayHarness.Run(42, "baltic-patrol", ticks: 1, mvpEngagement: false);
        var prior = SensorC2Bridge.PanelBridge;
        try
        {
            SensorC2Bridge.PanelBridge = new StubPanelBridge();
            var panel = SensorC2Bridge.BindPanel(result.SensorC2);

            Assert.That(panel.EmconLabel, Is.EqualTo("EMCON: STUB"));
            Assert.That(panel.ContactCountLabel, Is.EqualTo("CONTACTS: 0"));
        }
        finally
        {
            SensorC2Bridge.PanelBridge = prior;
        }
    }

    private sealed class StubPanelBridge : ISensorC2PanelBridge
    {
        public SensorC2PanelState BindPanel(SensorC2Snapshot snapshot) =>
            new("EMCON: STUB", "TRACK: STUB", "CONTACTS: 0", Array.Empty<SensorC2ContactRow>());
    }
}