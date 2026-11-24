using NUnit.Framework;
using ProjectAegis.Delegation.Comms;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Sim.Comms;
using ProjectAegis.Sim.Scenario;
using ProjectAegis.Sim.Sensors;

namespace ProjectAegis.Delegation.Tests.Comms;

[TestFixture]
public sealed class CommsGridRejoinReconcilerTests
{
    [Test]
    public void Rejoin_with_new_active_contact_merges_into_side_picture_and_emits_summary()
    {
        var reconciler = new CommsGridRejoinReconciler();
        var log = new DecisionLog();

        reconciler.NotifyOffGrid("u1", sinceTick: 10);
        reconciler.RecordUnitObservation("u1", "target-1", ContactLifecycleState.Detected);

        var rejoin = new CommsGridChange(20, "u1", CommsGridMembership.OffGrid, CommsGridMembership.OnGrid, "periscope");
        var result = reconciler.ReconcileChanges(new[] { rejoin }, log, simTime: 20.0);

        Assert.That(result.InformativeMergeCount, Is.EqualTo(1));
        Assert.That(result.SuppressedCount, Is.EqualTo(0));
        Assert.That(log.ContactChanges, Has.Count.EqualTo(1));
        Assert.That(log.ContactChanges[0].ObserverId, Is.EqualTo("u1"));
        Assert.That(log.ContactChanges[0].ContactId, Is.EqualTo("dl-target-1"));
        Assert.That(log.ContactChanges[0].TargetId, Is.EqualTo("target-1"));
        Assert.That(log.ContactChanges[0].PreviousState, Is.EqualTo("Unknown"));
        Assert.That(log.ContactChanges[0].NewState, Is.EqualTo("Detected"));
        Assert.That(log.ContactChanges[0].SimTick, Is.EqualTo(20UL));

        Assert.That(log.CommsGridRejoins, Has.Count.EqualTo(1));
        var summary = log.CommsGridRejoins[0];
        Assert.That(summary.SequenceId, Is.GreaterThan(0UL));
        Assert.That(summary.UnitId, Is.EqualTo("u1"));
        Assert.That(summary.OffGridSinceTick, Is.EqualTo(10UL));
        Assert.That(summary.RejoinTick, Is.EqualTo(20UL));
        Assert.That(summary.SuppressedCount, Is.EqualTo(0));
        Assert.That(summary.InformativeMerges, Has.Count.EqualTo(1));
        Assert.That(summary.InformativeMerges[0], Is.EqualTo(
            new CommsGridRejoinMergeDelta("target-1", "dl-target-1", "Unknown", "Detected")));
        Assert.That(reconciler.GetSideContactState("target-1"), Is.EqualTo(ContactLifecycleState.Detected));
    }

    [Test]
    public void Rejoin_with_strictly_higher_rank_upgrades_side_picture_and_emits_summary()
    {
        var reconciler = new CommsGridRejoinReconciler();
        var log = new DecisionLog();
        log.AppendContactChange(new ContactChangeRecord(
            0, 5.0, 5, "u2", "dl-target-1", "target-1", "Unknown", "Detected"));

        reconciler.NotifyOffGrid("u1", sinceTick: 10);
        reconciler.RecordUnitObservation("u1", "target-1", ContactLifecycleState.Identified);

        var rejoin = new CommsGridChange(20, "u1", CommsGridMembership.OffGrid, CommsGridMembership.OnGrid, "surfaced");
        reconciler.ReconcileChanges(new[] { rejoin }, log, simTime: 20.0);

        Assert.That(log.ContactChanges, Has.Count.EqualTo(2));
        Assert.That(log.ContactChanges[1].ContactId, Is.EqualTo("dl-target-1"));
        Assert.That(log.ContactChanges[1].PreviousState, Is.EqualTo("Detected"));
        Assert.That(log.ContactChanges[1].NewState, Is.EqualTo("Identified"));
        Assert.That(log.CommsGridRejoins, Has.Count.EqualTo(1));
        Assert.That(log.CommsGridRejoins[0].InformativeMerges[0].PreviousState, Is.EqualTo("Detected"));
        Assert.That(log.CommsGridRejoins[0].InformativeMerges[0].NewState, Is.EqualTo("Identified"));
        Assert.That(reconciler.GetSideContactState("target-1"), Is.EqualTo(ContactLifecycleState.Identified));
    }

    [Test]
    public void Rejoin_with_lost_side_picture_treats_unit_active_track_as_informative_merge()
    {
        var reconciler = new CommsGridRejoinReconciler();
        var log = new DecisionLog();
        log.AppendContactChange(new ContactChangeRecord(
            0, 5.0, 5, "u2", "dl-target-1", "target-1", "Detected", "Lost"));

        reconciler.NotifyOffGrid("u1", sinceTick: 10);
        reconciler.RecordUnitObservation("u1", "target-1", ContactLifecycleState.Classified);

        var rejoin = new CommsGridChange(20, "u1", CommsGridMembership.OffGrid, CommsGridMembership.OnGrid, "rejoin");
        reconciler.ReconcileChanges(new[] { rejoin }, log, simTime: 20.0);

        Assert.That(log.ContactChanges, Has.Count.EqualTo(2));
        Assert.That(log.ContactChanges[1].PreviousState, Is.EqualTo("Lost"));
        Assert.That(log.ContactChanges[1].NewState, Is.EqualTo("Classified"));
        Assert.That(log.CommsGridRejoins, Has.Count.EqualTo(1));
    }

    [Test]
    public void Rejoin_with_redundant_lower_rank_or_lost_observations_emits_nothing()
    {
        var reconciler = new CommsGridRejoinReconciler();
        var log = new DecisionLog();
        log.AppendContactChange(new ContactChangeRecord(0, 1.0, 1, "u2", "dl-t1", "t1", "Unknown", "Identified"));
        log.AppendContactChange(new ContactChangeRecord(0, 2.0, 2, "u2", "dl-t2", "t2", "Unknown", "Classified"));
        log.AppendContactChange(new ContactChangeRecord(0, 3.0, 3, "u2", "dl-t3", "t3", "Unknown", "Detected"));

        var beforeFingerprint = log.ComputeFingerprint();
        var beforeEntries = log.ChronologicalEntries().Count;

        reconciler.NotifyOffGrid("u1", sinceTick: 10);
        reconciler.RecordUnitObservation("u1", "t1", ContactLifecycleState.Identified); // same rank
        reconciler.RecordUnitObservation("u1", "t2", ContactLifecycleState.Detected);   // lower rank
        reconciler.RecordUnitObservation("u1", "t3", ContactLifecycleState.Lost);       // Lost never overwrites active
        reconciler.RecordUnitObservation("u1", "t4", ContactLifecycleState.Lost);       // Lost on unknown side target

        var rejoin = new CommsGridChange(20, "u1", CommsGridMembership.OffGrid, CommsGridMembership.OnGrid, "rejoin");
        var result = reconciler.ReconcileChanges(new[] { rejoin }, log, simTime: 20.0);

        Assert.That(result.InformativeMergeCount, Is.EqualTo(0));
        Assert.That(result.SuppressedCount, Is.EqualTo(4));
        Assert.That(log.ChronologicalEntries(), Has.Count.EqualTo(beforeEntries));
        Assert.That(log.ComputeFingerprint(), Is.EqualTo(beforeFingerprint));
        Assert.That(log.CommsGridRejoins, Is.Empty);
        Assert.That(reconciler.GetSideContactState("t3"), Is.EqualTo(ContactLifecycleState.Detected));
    }

    [Test]
    public void Rejoin_with_mixed_observations_emits_informative_in_ordinal_target_order_and_counts_suppressed()
    {
        var reconciler = new CommsGridRejoinReconciler();
        var log = new DecisionLog();
        log.AppendContactChange(new ContactChangeRecord(0, 1.0, 1, "u2", "dl-t-b", "t-b", "Unknown", "Detected"));
        log.AppendContactChange(new ContactChangeRecord(0, 2.0, 2, "u2", "dl-t-c", "t-c", "Unknown", "Identified"));
        log.AppendContactChange(new ContactChangeRecord(0, 3.0, 3, "u2", "dl-t-d", "t-d", "Unknown", "Detected"));

        reconciler.NotifyOffGrid("u1", sinceTick: 10);
        reconciler.RecordUnitObservation("u1", "t-b", ContactLifecycleState.Classified); // upgrade Detected -> Classified
        reconciler.RecordUnitObservation("u1", "t-a", ContactLifecycleState.Identified); // new Unknown -> Identified
        reconciler.RecordUnitObservation("u1", "t-c", ContactLifecycleState.Detected);   // suppressed (lower)
        reconciler.RecordUnitObservation("u1", "t-d", ContactLifecycleState.Lost);       // suppressed (Lost)

        var rejoin = new CommsGridChange(20, "u1", CommsGridMembership.OffGrid, CommsGridMembership.OnGrid, "rejoin");
        reconciler.ReconcileChanges(new[] { rejoin }, log, simTime: 20.0);

        var addedContacts = log.ContactChanges.Skip(3).ToArray();
        Assert.That(addedContacts.Select(c => c.TargetId), Is.EqualTo(new[] { "t-a", "t-b" }));
        Assert.That(log.CommsGridRejoins, Has.Count.EqualTo(1));

        var summary = log.CommsGridRejoins[0];
        Assert.That(summary.SuppressedCount, Is.EqualTo(2));
        Assert.That(summary.InformativeMerges.Select(m => m.TargetId), Is.EqualTo(new[] { "t-a", "t-b" }));
    }

    [Test]
    public void Rejoin_across_multiple_units_on_same_tick_is_deterministic_and_deduplicates_side_upgrades()
    {
        var reconciler = new CommsGridRejoinReconciler();
        var log = new DecisionLog();
        var grid = new CommsGridRegistry(new[]
        {
            new ScenarioCommsGridTransition(10, "u2", "OffGrid"),
            new ScenarioCommsGridTransition(10, "u1", "OffGrid"),
            new ScenarioCommsGridTransition(20, "u2", "OnGrid"),
            new ScenarioCommsGridTransition(20, "u1", "OnGrid"),
        });

        var offChanges = grid.Advance(10);
        reconciler.ReconcileChanges(offChanges, log, simTime: 10.0);
        Assert.That(log.ChronologicalEntries(), Is.Empty);

        reconciler.RecordUnitObservation("u1", "target-1", ContactLifecycleState.Identified);
        reconciler.RecordUnitObservation("u2", "target-1", ContactLifecycleState.Classified);
        reconciler.RecordUnitObservation("u2", "target-2", ContactLifecycleState.Detected);

        var onChanges = grid.Advance(20);
        reconciler.ReconcileChanges(onChanges, log, simTime: 20.0);

        // u1 precedes u2 in ordinal order; u1 upgrades target-1 to Identified, so u2's Classified on target-1 is suppressed.
        Assert.That(log.CommsGridRejoins.Select(r => r.UnitId), Is.EqualTo(new[] { "u1", "u2" }));
        Assert.That(log.CommsGridRejoins[0].InformativeMerges.Select(m => m.TargetId), Is.EqualTo(new[] { "target-1" }));
        Assert.That(log.CommsGridRejoins[1].InformativeMerges.Select(m => m.TargetId), Is.EqualTo(new[] { "target-2" }));
        Assert.That(log.CommsGridRejoins[1].SuppressedCount, Is.EqualTo(1));
    }

    [Test]
    public void Rejoin_clears_episode_unit_observations_so_stale_tracks_do_not_resurrect_lost_targets()
    {
        var reconciler = new CommsGridRejoinReconciler();
        var log = new DecisionLog();

        reconciler.NotifyOffGrid("u1", sinceTick: 10);
        reconciler.RecordUnitObservation("u1", "target-1", ContactLifecycleState.Identified);
        reconciler.ReconcileChanges(
            new[] { new CommsGridChange(15, "u1", CommsGridMembership.OffGrid, CommsGridMembership.OnGrid, "up") },
            log,
            simTime: 15.0);
        Assert.That(log.CommsGridRejoins, Has.Count.EqualTo(1));

        log.AppendContactChange(new ContactChangeRecord(0, 18.0, 18, "u2", "dl-target-1", "target-1", "Identified", "Lost"));

        reconciler.NotifyOffGrid("u1", sinceTick: 20);
        reconciler.ReconcileChanges(
            new[] { new CommsGridChange(30, "u1", CommsGridMembership.OffGrid, CommsGridMembership.OnGrid, "up2") },
            log,
            simTime: 30.0);

        Assert.That(log.CommsGridRejoins, Has.Count.EqualTo(1));
        Assert.That(reconciler.GetSideContactState("target-1"), Is.EqualTo(ContactLifecycleState.Lost));
    }
}
