using ProjectAegis.Delegation.Comms;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.TrackCustody;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.TrackCustody;

[TestFixture]
public sealed class TrackCustodyProjectionTests
{
    [Test]
    public void Empty_log_yields_empty_snapshot()
    {
        var snapshot = TrackCustodyProjection.Project(new DecisionLog(), currentSimTick: 0);

        Assert.That(snapshot.Rows, Is.Empty);
        Assert.That(snapshot.Entries, Is.Empty);
        Assert.That(TrackCustodyFingerprint.Compute(snapshot), Is.EqualTo("tc:empty"));
    }

    [Test]
    public void Held_fresh_track_has_no_break_cause()
    {
        var log = new DecisionLog();
        log.AppendContactChange(Change(5, "c1", "hostile-1", "Unknown", "Classified"));

        var snapshot = TrackCustodyProjection.Project(log, currentSimTick: 5);

        Assert.That(snapshot.Rows, Has.Count.EqualTo(1));
        var row = snapshot.Rows[0];
        Assert.That(row.ContactId, Is.EqualTo("c1"));
        Assert.That(row.TargetId, Is.EqualTo("hostile-1"));
        Assert.That(row.ObserverId, Is.EqualTo("u1"));
        Assert.That(row.Custody, Is.EqualTo(TrackCustodyState.Held));
        Assert.That(row.Cause, Is.EqualTo(TrackCustodyCause.None));
        Assert.That(row.CauseLabel, Is.Empty);
        Assert.That(row.LastKnownTick, Is.EqualTo(5UL));
        Assert.That(row.LastKnownSimTime, Is.EqualTo(5.0));
    }

    [Test]
    public void Stale_track_names_stale_cause_and_publishes_ledger_entry()
    {
        var log = new DecisionLog();
        log.AppendContactChange(Change(1, "c1", "hostile-1", "Unknown", "Classified"));

        var staleTick = 1UL + (ulong)KillChainContactStateProjection.DefaultStaleThresholdTicks + 1;
        var snapshot = TrackCustodyProjection.Project(log, currentSimTick: staleTick);

        var row = snapshot.Rows[0];
        Assert.That(row.Custody, Is.EqualTo(TrackCustodyState.Held));
        Assert.That(row.Cause, Is.EqualTo(TrackCustodyCause.Stale));
        Assert.That(row.CauseLabel, Is.EqualTo(TrackCustodyCauseLabels.Stale));

        Assert.That(snapshot.Entries, Has.Count.EqualTo(1));
        Assert.That(snapshot.Entries[0].Custody, Is.EqualTo(TrackCustodyState.Held));
        Assert.That(snapshot.Entries[0].Cause, Is.EqualTo(TrackCustodyCause.Stale));
        Assert.That(snapshot.Entries[0].CauseLabel, Is.EqualTo(TrackCustodyCauseLabels.Stale));
        Assert.That(snapshot.Entries[0].SimTick, Is.EqualTo(staleTick));
    }

    [Test]
    public void Timeout_drop_names_lost_sensor_and_marks_dropped()
    {
        var log = new DecisionLog();
        log.AppendContactChange(Change(1, "c1", "hostile-1", "Unknown", "Classified"));

        var dropTick = 1UL + (ulong)KillChainContactStateProjection.DefaultDropThresholdTicks + 1;
        var snapshot = TrackCustodyProjection.Project(log, currentSimTick: dropTick);

        var row = snapshot.Rows[0];
        Assert.That(row.Custody, Is.EqualTo(TrackCustodyState.Dropped));
        Assert.That(row.Cause, Is.EqualTo(TrackCustodyCause.LostSensor));
        Assert.That(row.CauseLabel, Is.EqualTo(TrackCustodyCauseLabels.LostSensor));
        Assert.That(row.CauseLabel, Is.Not.Empty);

        var lostEntry = snapshot.Entries.Single(e => e.Custody == TrackCustodyState.Dropped);
        Assert.That(lostEntry.Cause, Is.EqualTo(TrackCustodyCause.LostSensor));
        Assert.That(lostEntry.CauseLabel, Is.EqualTo(TrackCustodyCauseLabels.LostSensor));
    }

    [Test]
    public void Explicit_lost_lifecycle_names_explicit_drop()
    {
        var log = new DecisionLog();
        log.AppendContactChange(Change(1, "c1", "hostile-1", "Unknown", "Detected"));
        log.AppendContactChange(Change(5, "c1", "hostile-1", "Detected", "Classified"));
        log.AppendContactChange(Change(12, "c1", "hostile-1", "Classified", "Lost"));

        var snapshot = TrackCustodyProjection.Project(log, currentSimTick: 12);

        var row = snapshot.Rows[0];
        Assert.That(row.Custody, Is.EqualTo(TrackCustodyState.Dropped));
        Assert.That(row.Cause, Is.EqualTo(TrackCustodyCause.ExplicitDrop));
        Assert.That(row.CauseLabel, Is.EqualTo(TrackCustodyCauseLabels.ExplicitDrop));

        var lostEntry = snapshot.Entries.Single(e => e.Custody == TrackCustodyState.Dropped);
        Assert.That(lostEntry.Cause, Is.EqualTo(TrackCustodyCause.ExplicitDrop));
        Assert.That(lostEntry.SimTick, Is.EqualTo(12UL));
    }

    [Test]
    public void Denied_comms_names_comms_denied_on_held_contact()
    {
        var log = new DecisionLog();
        log.AppendContactChange(Change(3, "c1", "hostile-1", "Unknown", "Detected"));
        log.AppendCommsStateChange(new CommsStateChangeRecord(
            0,
            3.0,
            3,
            "c2-net",
            CommsState.Nominal,
            CommsState.Denied,
            "jam"));

        var snapshot = TrackCustodyProjection.Project(log, currentSimTick: 3);

        var row = snapshot.Rows[0];
        Assert.That(row.Custody, Is.EqualTo(TrackCustodyState.Held));
        Assert.That(row.Cause, Is.EqualTo(TrackCustodyCause.CommsDenied));
        Assert.That(row.CauseLabel, Is.EqualTo(TrackCustodyCauseLabels.CommsDenied));
        Assert.That(row.CauseLabel, Is.Not.Empty);
    }

    [Test]
    public void Dropped_rows_never_have_empty_cause_label()
    {
        var log = new DecisionLog();
        log.AppendContactChange(Change(1, "c1", "hostile-1", "Unknown", "Classified"));
        log.AppendContactChange(Change(4, "c1", "hostile-1", "Classified", "Lost"));

        var snapshot = TrackCustodyProjection.Project(log, currentSimTick: 4);

        foreach (var row in snapshot.Rows.Where(r => r.Custody == TrackCustodyState.Dropped))
        {
            Assert.That(row.Cause, Is.Not.EqualTo(TrackCustodyCause.None));
            Assert.That(row.CauseLabel, Is.Not.Empty);
        }

        foreach (var entry in snapshot.Entries.Where(e => e.Custody == TrackCustodyState.Dropped))
        {
            Assert.That(entry.Cause, Is.Not.EqualTo(TrackCustodyCause.None));
            Assert.That(entry.CauseLabel, Is.Not.Empty);
        }
    }

    [Test]
    public void Fingerprint_is_replay_stable_for_identical_inputs()
    {
        var log = new DecisionLog();
        log.AppendContactChange(Change(1, "c2", "hostile-2", "Unknown", "Detected"));
        log.AppendContactChange(Change(1, "c1", "hostile-1", "Unknown", "Classified"));
        log.AppendContactChange(Change(4, "c1", "hostile-1", "Classified", "Lost"));

        var a = TrackCustodyProjection.Project(log, currentSimTick: 4);
        var b = TrackCustodyProjection.Project(log, currentSimTick: 4);

        Assert.That(TrackCustodyFingerprint.Compute(a), Is.EqualTo(TrackCustodyFingerprint.Compute(b)));
        Assert.That(a.Rows.Select(r => r.ContactId), Is.EqualTo(new[] { "c1", "c2" }));
    }

    private static ContactChangeRecord Change(
        ulong tick,
        string contactId,
        string targetId,
        string previous,
        string next) =>
        new(0, tick, tick, "u1", contactId, targetId, previous, next);
}
