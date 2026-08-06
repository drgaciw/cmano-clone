namespace ProjectAegis.Delegation.UnityAdapter.Tests.Bridge;

using NUnit.Framework;
using ProjectAegis.Delegation.Projection;

/// <summary>
/// Headless tests for domain-tag derivation used by CombatDomainsHotTickHost
/// (logic lives on Unity host as internal static — mirrored here via public binder + tag mapping rules).
/// </summary>
[TestFixture]
public sealed class DelegationBridgeHostDomainTagsTests
{
    [Test]
    public void FromMessageLog_kill_categories_map_to_surface_then_binder_engaged()
    {
        var lines = new[]
        {
            new MessageLogLine(1, 1.0, "KILL_CONFIRMED", "Destroyed", "u1"),
            new MessageLogLine(2, 1.1, "HIT", "Impact", "u2"),
        };
        var tags = CombatDomainActivityTags.FromMessageLog(lines);
        Assert.That(tags, Does.Contain("Surface"));

        var state = CombatDomainsHotTickPanelBinder.BindFromActiveDomainTags(tags);
        Assert.That(state.Rows.Single(r => r.DomainKey == "Surface").StateLabel, Is.EqualTo("ENGAGED"));
    }

    [Test]
    public void MessageLog_selection_drives_sequenceId_for_host_API()
    {
        var panel = MessageLogPanelBinder.Bind(new[]
        {
            new MessageLogLine(1001, 3.0, "HIT", "Impact", "friendly-2"),
        });
        var sel = MessageLogPanelSelection.SelectRow(panel, 0);
        Assert.That(sel, Is.Not.Null);
        Assert.That(sel!.SequenceId, Is.EqualTo(1001uL));
        Assert.That(sel.UnitId, Is.EqualTo("friendly-2"));
    }

    [Test]
    public void Tracker_from_message_log_binds_degraded_land_and_engaged_surface()
    {
        var tracker = new CombatDomainsHotTickTracker();
        tracker.ObserveMessageLog(2, new[]
        {
            new MessageLogLine(1, 1.0, "KILL_CONFIRMED", "Destroyed", "u1"),
            new MessageLogLine(2, 1.1, "POLICY_DENIAL", "ROE", "u2"),
        });
        var state = CombatDomainsHotTickPanelBinder.BindFromTracker(tracker);
        Assert.That(state.Rows.Single(r => r.DomainKey == "Surface").StateLabel, Is.EqualTo("ENGAGED"));
        Assert.That(state.Rows.Single(r => r.DomainKey == "Land").StateLabel, Is.EqualTo("DEGRADED"));
    }

    [Test]
    public void SelectBySequenceId_and_category_css_roundtrip()
    {
        var panel = MessageLogPanelBinder.Bind(new[]
        {
            new MessageLogLine(55, 0.5, "POLICY_DENIAL", "Denied", "blue"),
        });
        Assert.That(panel.Rows[0].CategoryCssClass, Is.EqualTo("message-log-row--policy"));
        var sel = MessageLogPanelSelection.SelectBySequenceId(panel, 55);
        Assert.That(sel, Is.Not.Null);
        Assert.That(sel!.UnitId, Is.EqualTo("blue"));
    }
}
