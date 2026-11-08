using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

/// <summary>S105 A1 — hot-tick tracker projection DTO (tick-stamped domain engagement).</summary>
public sealed class CombatDomainsHotTickTrackerTests
{
    [Test]
    public void ObserveTags_promotes_engaged_without_sim_coupling()
    {
        var tracker = new CombatDomainsHotTickTracker();
        tracker.ObserveActiveDomainTags(simTick: 12, activeDomainTags: new[] { "Air", "Mine" });

        var map = tracker.SnapshotEngagements();
        Assert.That(map["Air"], Is.EqualTo(CombatDomainHudEngagement.Engaged));
        Assert.That(map["Mine"], Is.EqualTo(CombatDomainHudEngagement.Engaged));
        Assert.That(map["Surface"], Is.EqualTo(CombatDomainHudEngagement.Idle));
        Assert.That(tracker.LastSimTick, Is.EqualTo(12));
    }

    [Test]
    public void ObserveMessageLog_maps_kill_comms_contact_and_degraded_policy()
    {
        var tracker = new CombatDomainsHotTickTracker();
        tracker.ObserveMessageLog(simTick: 5, lines: new[]
        {
            new MessageLogLine(1, 1.0, "KILL_CONFIRMED", "Destroyed", "u1"),
            new MessageLogLine(2, 1.1, "COMMS", "Jammed", null),
            new MessageLogLine(3, 1.2, "CONTACT", "Sub contact", "red"),
            new MessageLogLine(4, 1.3, "POLICY_DENIAL", "ROE hold", "blue"),
            new MessageLogLine(5, 1.4, "WEAPON_LAUNCH", "Launch", "blue"),
            new MessageLogLine(6, 1.5, "MAGAZINE", "Reload", "blue"),
        });

        var map = tracker.SnapshotEngagements();
        Assert.That(map["Surface"], Is.EqualTo(CombatDomainHudEngagement.Engaged)); // kill
        Assert.That(map["Air"], Is.EqualTo(CombatDomainHudEngagement.Engaged)); // comms / weapon
        Assert.That(map["Subsurface"], Is.EqualTo(CombatDomainHudEngagement.Engaged)); // contact
        Assert.That(map["Land"], Is.EqualTo(CombatDomainHudEngagement.Degraded)); // policy
        Assert.That(map["Facility"], Is.EqualTo(CombatDomainHudEngagement.Engaged)); // magazine
    }

    [Test]
    public void Binder_BindFromTracker_matches_snapshot()
    {
        var tracker = new CombatDomainsHotTickTracker();
        tracker.ObserveActiveDomainTags(3, new[] { "Subsurface" });
        var state = CombatDomainsHotTickPanelBinder.BindFromTracker(tracker);

        Assert.That(state.Rows.Single(r => r.DomainKey == "Subsurface").StateLabel, Is.EqualTo("ENGAGED"));
        Assert.That(state.Rows.Single(r => r.DomainKey == "Air").StateLabel, Is.EqualTo("IDLE"));
    }

    [Test]
    public void Engaged_wins_over_prior_degraded_for_same_domain()
    {
        var tracker = new CombatDomainsHotTickTracker();
        tracker.SetEngagement("Air", CombatDomainHudEngagement.Degraded);
        tracker.ObserveActiveDomainTags(1, new[] { "Air" });
        Assert.That(tracker.SnapshotEngagements()["Air"], Is.EqualTo(CombatDomainHudEngagement.Engaged));
    }

    [Test]
    public void Clear_resets_to_idle()
    {
        var tracker = new CombatDomainsHotTickTracker();
        tracker.ObserveActiveDomainTags(1, new[] { "Land" });
        tracker.Clear();
        Assert.That(tracker.SnapshotEngagements().Values.All(v => v == CombatDomainHudEngagement.Idle), Is.True);
        Assert.That(tracker.LastSimTick, Is.EqualTo(0));
    }
}
