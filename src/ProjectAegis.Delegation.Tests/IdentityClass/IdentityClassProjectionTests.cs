using ProjectAegis.Delegation.Comms;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.IdentityClass;
using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.IdentityClass;

[TestFixture]
public sealed class IdentityClassProjectionTests
{
    [Test]
    public void Empty_log_yields_empty_snapshot()
    {
        var snapshot = IdentityClassProjection.Project(new DecisionLog(), currentSimTick: 0);

        Assert.That(snapshot.Rows, Is.Empty);
        Assert.That(IdentityClassFingerprint.Compute(snapshot), Is.EqualTo("ic:empty"));
    }

    [Test]
    public void Unknown_never_silent_contact_names_lifecycle_unknown_reason()
    {
        var log = new DecisionLog();
        log.AppendContactChange(Change(3, "c1", "hostile-1", "Unknown", "Unknown"));

        var snapshot = IdentityClassProjection.Project(log, currentSimTick: 3);

        Assert.That(snapshot.Rows, Has.Count.EqualTo(1));
        var row = snapshot.Rows[0];
        Assert.That(row.ContactId, Is.EqualTo("c1"));
        Assert.That(row.Classification, Is.EqualTo(IdentityClassification.Unknown));
        Assert.That(row.ReasonCode, Is.EqualTo(IdentityClassReasonCodes.LifecycleUnknown));
        Assert.That(row.ReasonLabel, Is.EqualTo(IdentityClassReasonLabels.LifecycleUnknown));
        Assert.That(row.ReasonLabel, Is.Not.Empty);
        Assert.That(row.ConfidenceBand, Is.EqualTo(IdentityConfidenceBand.Unknown));
        Assert.That(row.SimTick, Is.EqualTo(3UL));
    }

    [Test]
    public void Unknown_never_silent_denied_comms_names_comms_gap()
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

        var snapshot = IdentityClassProjection.Project(log, currentSimTick: 3);

        var row = snapshot.Rows[0];
        Assert.That(row.Classification, Is.EqualTo(IdentityClassification.Unknown));
        Assert.That(row.ReasonCode, Is.EqualTo(IdentityClassReasonCodes.CommsGap));
        Assert.That(row.ReasonLabel, Is.EqualTo(IdentityClassReasonLabels.CommsGap));
        Assert.That(row.ReasonLabel, Is.Not.Empty);
    }

    [Test]
    public void Classified_contact_names_lifecycle_classified_reason_and_medium_confidence()
    {
        var log = new DecisionLog();
        log.AppendContactChange(Change(5, "c1", "hostile-1", "Unknown", "Classified"));

        var snapshot = IdentityClassProjection.Project(log, currentSimTick: 5);

        Assert.That(snapshot.Rows, Has.Count.EqualTo(1));
        var row = snapshot.Rows[0];
        Assert.That(row.Classification, Is.EqualTo(IdentityClassification.Classified));
        Assert.That(row.ReasonCode, Is.EqualTo(IdentityClassReasonCodes.LifecycleClassified));
        Assert.That(row.ReasonLabel, Is.EqualTo(IdentityClassReasonLabels.LifecycleClassified));
        Assert.That(row.ReasonLabel, Is.Not.Empty);
        Assert.That(row.ConfidenceBand, Is.EqualTo(IdentityConfidenceBand.Medium));
        Assert.That(row.SimTick, Is.EqualTo(5UL));
    }

    [Test]
    public void Classified_identified_contact_names_lifecycle_identified_reason_and_high_confidence()
    {
        var log = new DecisionLog();
        log.AppendContactChange(Change(7, "c1", "hostile-1", "Classified", "Identified"));

        var snapshot = IdentityClassProjection.Project(log, currentSimTick: 7);

        var row = snapshot.Rows[0];
        Assert.That(row.Classification, Is.EqualTo(IdentityClassification.Classified));
        Assert.That(row.ReasonCode, Is.EqualTo(IdentityClassReasonCodes.LifecycleIdentified));
        Assert.That(row.ReasonLabel, Is.EqualTo(IdentityClassReasonLabels.LifecycleIdentified));
        Assert.That(row.ConfidenceBand, Is.EqualTo(IdentityConfidenceBand.High));
    }

    [Test]
    public void Tentative_detected_contact_names_lifecycle_detected_reason_and_low_confidence()
    {
        var log = new DecisionLog();
        log.AppendContactChange(Change(4, "c1", "hostile-1", "Unknown", "Detected"));

        var snapshot = IdentityClassProjection.Project(log, currentSimTick: 4);

        Assert.That(snapshot.Rows, Has.Count.EqualTo(1));
        var row = snapshot.Rows[0];
        Assert.That(row.Classification, Is.EqualTo(IdentityClassification.Tentative));
        Assert.That(row.ReasonCode, Is.EqualTo(IdentityClassReasonCodes.LifecycleDetected));
        Assert.That(row.ReasonLabel, Is.EqualTo(IdentityClassReasonLabels.LifecycleDetected));
        Assert.That(row.ReasonLabel, Is.Not.Empty);
        Assert.That(row.ConfidenceBand, Is.EqualTo(IdentityConfidenceBand.Low));
        Assert.That(row.SimTick, Is.EqualTo(4UL));
    }

    [Test]
    public void Tentative_stale_detected_contact_names_stale_track_reason()
    {
        var log = new DecisionLog();
        log.AppendContactChange(Change(1, "c1", "hostile-1", "Unknown", "Detected"));

        var staleTick = 1UL + (ulong)ContactProvenanceProjection.DefaultStaleThresholdTicks + 1;
        var snapshot = IdentityClassProjection.Project(log, currentSimTick: staleTick);

        var row = snapshot.Rows[0];
        Assert.That(row.Classification, Is.EqualTo(IdentityClassification.Tentative));
        Assert.That(row.ReasonCode, Is.EqualTo(IdentityClassReasonCodes.StaleTrack));
        Assert.That(row.ReasonLabel, Is.EqualTo(IdentityClassReasonLabels.StaleTrack));
        Assert.That(row.ReasonLabel, Is.Not.Empty);
    }

    [Test]
    public void All_rows_publish_non_empty_reason_labels()
    {
        var log = new DecisionLog();
        log.AppendContactChange(Change(1, "c2", "hostile-2", "Unknown", "Detected"));
        log.AppendContactChange(Change(1, "c1", "hostile-1", "Unknown", "Classified"));
        log.AppendContactChange(Change(2, "c3", "hostile-3", "Unknown", "Unknown"));

        var snapshot = IdentityClassProjection.Project(log, currentSimTick: 2);

        foreach (var row in snapshot.Rows)
        {
            Assert.That(row.ReasonCode, Is.Not.Empty);
            Assert.That(row.ReasonLabel, Is.Not.Empty);
        }
    }

    [Test]
    public void Fingerprint_is_replay_stable_for_identical_inputs()
    {
        var log = new DecisionLog();
        log.AppendContactChange(Change(1, "c2", "hostile-2", "Unknown", "Detected"));
        log.AppendContactChange(Change(1, "c1", "hostile-1", "Unknown", "Classified"));

        var a = IdentityClassProjection.Project(log, currentSimTick: 1);
        var b = IdentityClassProjection.Project(log, currentSimTick: 1);

        Assert.That(IdentityClassFingerprint.Compute(a), Is.EqualTo(IdentityClassFingerprint.Compute(b)));
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
