using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Sim.Catalog;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

[TestFixture]
public sealed class KillChainContactStateProjectionTests
{
    [Test]
    public void Empty_log_yields_empty_snapshot()
    {
        var snapshot = KillChainContactStateProjection.Project(new DecisionLog(), currentSimTick: 0);

        Assert.That(snapshot.Contacts, Is.Empty);
        Assert.That(snapshot.Transitions, Is.Empty);
    }

    [Test]
    public void Detected_publishes_Find_without_location_or_targetability()
    {
        var log = new DecisionLog();
        log.AppendContactChange(Change(1, "c1", "hostile-1", "Unknown", "Detected"));

        var snapshot = KillChainContactStateProjection.Project(log, currentSimTick: 1);

        Assert.That(snapshot.Contacts, Has.Count.EqualTo(1));
        var state = snapshot.Contacts[0];
        Assert.That(state.ContactId, Is.EqualTo("c1"));
        Assert.That(state.TargetId, Is.EqualTo("hostile-1"));
        Assert.That(state.ObserverId, Is.EqualTo("u1"));
        Assert.That(state.Phase, Is.EqualTo(KillChainPhase.Find));
        Assert.That(state.DetectionCaptured, Is.True);
        Assert.That(state.LocationSufficient, Is.False);
        Assert.That(state.TrackContinuous, Is.False);
        Assert.That(state.Targetable, Is.False);
        Assert.That(state.Loss, Is.EqualTo(KillChainLossKind.None));
        Assert.That(state.FirstSimTick, Is.EqualTo(1UL));
        Assert.That(state.LastSimTick, Is.EqualTo(1UL));
        Assert.That(state.FirstSimTime, Is.EqualTo(1.0));
        Assert.That(state.LastSimTime, Is.EqualTo(1.0));
        Assert.That(state.CorrelationSequenceId, Is.GreaterThan(0UL));
        Assert.That(state.SourceRefs, Does.Contain("observer:u1"));
        Assert.That(state.SourceRefs, Does.Contain("contact:c1"));
        Assert.That(state.SourceRefs, Does.Contain("target:hostile-1"));

        Assert.That(snapshot.Transitions.Select(t => t.Kind), Is.EqualTo(new[] { KillChainTransitionKind.Find }));
        Assert.That(snapshot.Transitions[0].PreviousPhase, Is.EqualTo(KillChainPhase.None));
        Assert.That(snapshot.Transitions[0].NewPhase, Is.EqualTo(KillChainPhase.Find));
        Assert.That(snapshot.Transitions[0].SimTick, Is.EqualTo(1UL));
        Assert.That(snapshot.Transitions[0].CorrelationSequenceId, Is.EqualTo(state.CorrelationSequenceId));
    }

    [Test]
    public void Classified_publishes_Fix_and_Track_from_location_and_custody()
    {
        var log = new DecisionLog();
        log.AppendContactChange(Change(1, "c1", "hostile-1", "Unknown", "Detected"));
        log.AppendContactChange(Change(5, "c1", "hostile-1", "Detected", "Classified"));

        var snapshot = KillChainContactStateProjection.Project(log, currentSimTick: 5);

        Assert.That(snapshot.Contacts[0].Phase, Is.EqualTo(KillChainPhase.Track));
        Assert.That(snapshot.Contacts[0].DetectionCaptured, Is.True);
        Assert.That(snapshot.Contacts[0].LocationSufficient, Is.True);
        Assert.That(snapshot.Contacts[0].TrackContinuous, Is.True);
        Assert.That(snapshot.Contacts[0].Targetable, Is.False);
        Assert.That(snapshot.Transitions.Select(t => t.Kind), Is.EqualTo(new[]
        {
            KillChainTransitionKind.Find,
            KillChainTransitionKind.Fix,
            KillChainTransitionKind.Track,
        }));
    }

    [Test]
    public void Identified_with_fire_control_publishes_Target()
    {
        var log = new DecisionLog();
        log.AppendContactChange(Change(1, "c1", "hostile-1", "Unknown", "Detected"));
        log.AppendContactChange(Change(5, "c1", "hostile-1", "Detected", "Classified"));
        log.AppendContactChange(Change(9, "c1", "hostile-1", "Classified", "Identified"));

        var snapshot = KillChainContactStateProjection.Project(
            log,
            currentSimTick: 9,
            fireControl: new StubFireControl("c1"));

        Assert.That(snapshot.Contacts[0].Phase, Is.EqualTo(KillChainPhase.Target));
        Assert.That(snapshot.Contacts[0].Targetable, Is.True);
        Assert.That(snapshot.Contacts[0].LocationSufficient, Is.True);
        Assert.That(snapshot.Contacts[0].TrackContinuous, Is.True);
        Assert.That(snapshot.Transitions.Select(t => t.Kind), Is.EqualTo(new[]
        {
            KillChainTransitionKind.Find,
            KillChainTransitionKind.Fix,
            KillChainTransitionKind.Track,
            KillChainTransitionKind.Target,
        }));
    }

    [Test]
    public void Fire_control_on_Detected_is_Fix_not_Target_until_Track()
    {
        var log = new DecisionLog();
        log.AppendContactChange(Change(1, "c1", "hostile-1", "Unknown", "Detected"));

        var atDetect = KillChainContactStateProjection.Project(
            log,
            currentSimTick: 1,
            fireControl: new StubFireControl("c1"));

        Assert.That(atDetect.Contacts[0].Phase, Is.EqualTo(KillChainPhase.Fix));
        Assert.That(atDetect.Contacts[0].LocationSufficient, Is.True);
        Assert.That(atDetect.Contacts[0].TrackContinuous, Is.False);
        Assert.That(atDetect.Contacts[0].Targetable, Is.False);
        Assert.That(atDetect.Transitions.Select(t => t.Kind), Is.EqualTo(new[]
        {
            KillChainTransitionKind.Find,
            KillChainTransitionKind.Fix,
        }));

        var held = KillChainContactStateProjection.Project(
            log,
            currentSimTick: 2,
            fireControl: new StubFireControl("c1"));

        Assert.That(held.Contacts[0].Phase, Is.EqualTo(KillChainPhase.Target));
        Assert.That(held.Contacts[0].TrackContinuous, Is.True);
        Assert.That(held.Contacts[0].Targetable, Is.True);
        Assert.That(held.Transitions.Select(t => t.Kind), Is.EqualTo(new[]
        {
            KillChainTransitionKind.Find,
            KillChainTransitionKind.Fix,
            KillChainTransitionKind.Track,
            KillChainTransitionKind.Target,
        }));
        Assert.That(held.Transitions[2].SimTick, Is.EqualTo(2UL));
        Assert.That(held.Transitions[2].CorrelationSequenceId, Is.EqualTo(atDetect.Contacts[0].CorrelationSequenceId));
    }

    [Test]
    public void Lost_captures_loss_and_clears_targetability()
    {
        var log = new DecisionLog();
        log.AppendContactChange(Change(1, "c1", "hostile-1", "Unknown", "Detected"));
        log.AppendContactChange(Change(5, "c1", "hostile-1", "Detected", "Classified"));
        log.AppendContactChange(Change(12, "c1", "hostile-1", "Classified", "Lost"));

        var snapshot = KillChainContactStateProjection.Project(
            log,
            currentSimTick: 12,
            fireControl: new StubFireControl("c1"));

        Assert.That(snapshot.Contacts[0].Loss, Is.EqualTo(KillChainLossKind.Lost));
        Assert.That(snapshot.Contacts[0].TrackContinuous, Is.False);
        Assert.That(snapshot.Contacts[0].Targetable, Is.False);
        Assert.That(snapshot.Contacts[0].LastSimTick, Is.EqualTo(12UL));
        Assert.That(snapshot.Transitions.Select(t => t.Kind), Does.Contain(KillChainTransitionKind.Lost));
        Assert.That(snapshot.Transitions.Last().Kind, Is.EqualTo(KillChainTransitionKind.Lost));
        Assert.That(snapshot.Transitions.Last().Loss, Is.EqualTo(KillChainLossKind.Lost));
    }

    [Test]
    public void Stale_current_tick_marks_degradation_without_UI_clock()
    {
        var log = new DecisionLog();
        log.AppendContactChange(Change(1, "c1", "hostile-1", "Unknown", "Classified"));

        var snapshot = KillChainContactStateProjection.Project(
            log,
            currentSimTick: 1 + (ulong)KillChainContactStateProjection.DefaultStaleThresholdTicks + 1);

        Assert.That(snapshot.Contacts[0].Loss, Is.EqualTo(KillChainLossKind.Stale));
        Assert.That(snapshot.Contacts[0].TrackContinuous, Is.False);
        Assert.That(snapshot.Contacts[0].Targetable, Is.False);
        Assert.That(snapshot.Contacts[0].LocationSufficient, Is.True);
        Assert.That(snapshot.Contacts[0].Phase, Is.EqualTo(KillChainPhase.Fix));
        Assert.That(snapshot.Transitions.Select(t => t.Kind), Does.Contain(KillChainTransitionKind.Degraded));
        var degraded = snapshot.Transitions.Single(t => t.Kind == KillChainTransitionKind.Degraded);
        Assert.That(degraded.Loss, Is.EqualTo(KillChainLossKind.Stale));
        Assert.That(degraded.SimTick, Is.EqualTo(1UL + (ulong)KillChainContactStateProjection.DefaultStaleThresholdTicks + 1));
    }

    [Test]
    public void Bda_hit_projects_degradation_from_order_log()
    {
        var log = new DecisionLog();
        log.AppendContactChange(Change(1, "c1", "hostile-1", "Unknown", "Identified"));
        log.AppendPlatformDamageChange(new PlatformDamageChangeRecord(
            0,
            2,
            2,
            new TargetId("hostile-1"),
            100,
            75,
            PlatformDamageChangeReasonCodes.Hit,
            1));

        var snapshot = KillChainContactStateProjection.Project(
            log,
            currentSimTick: 2,
            fireControl: new StubFireControl("c1"));

        Assert.That(snapshot.Contacts[0].Loss, Is.EqualTo(KillChainLossKind.DegradedL1));
        Assert.That(snapshot.Contacts[0].Targetable, Is.False);
        Assert.That(snapshot.Transitions.Select(t => t.Kind), Does.Contain(KillChainTransitionKind.Degraded));
    }

    [Test]
    public void Contacts_and_transitions_are_replay_stable()
    {
        var log = new DecisionLog();
        log.AppendContactChange(Change(1, "c2", "hostile-2", "Unknown", "Detected"));
        log.AppendContactChange(Change(1, "c1", "hostile-1", "Unknown", "Classified"));
        log.AppendContactChange(Change(4, "c1", "hostile-1", "Classified", "Identified"));

        var a = KillChainContactStateProjection.Project(log, currentSimTick: 4, fireControl: new StubFireControl("c1"));
        var b = KillChainContactStateProjection.Project(log, currentSimTick: 4, fireControl: new StubFireControl("c1"));

        Assert.That(Canonical(a), Is.EqualTo(Canonical(b)));
        Assert.That(a.Contacts.Select(c => c.ContactId), Is.EqualTo(new[] { "c1", "c2" }));
    }

    [Test]
    public void Source_sequence_ids_correlate_to_contact_change_rows()
    {
        var log = new DecisionLog();
        log.AppendContactChange(Change(1, "c1", "hostile-1", "Unknown", "Detected"));
        log.AppendContactChange(Change(3, "c1", "hostile-1", "Detected", "Classified"));
        var sequences = log.ContactChanges.Select(c => c.SequenceId).ToArray();

        var snapshot = KillChainContactStateProjection.Project(log, currentSimTick: 3);

        Assert.That(snapshot.Contacts[0].SourceSequenceIds, Is.EqualTo(sequences));
        Assert.That(snapshot.Transitions[0].CorrelationSequenceId, Is.EqualTo(sequences[0]));
        Assert.That(snapshot.Transitions[1].CorrelationSequenceId, Is.EqualTo(sequences[1]));
    }

    [Test]
    public void Live_detection_after_Lost_clears_loss_and_republishes_Find()
    {
        var log = new DecisionLog();
        log.AppendContactChange(Change(1, "c1", "hostile-1", "Unknown", "Detected"));
        log.AppendContactChange(Change(4, "c1", "hostile-1", "Detected", "Lost"));
        log.AppendContactChange(Change(8, "c1", "hostile-1", "Lost", "Detected"));

        var snapshot = KillChainContactStateProjection.Project(log, currentSimTick: 8);

        Assert.That(snapshot.Contacts[0].Loss, Is.EqualTo(KillChainLossKind.None));
        Assert.That(snapshot.Contacts[0].DetectionCaptured, Is.True);
        Assert.That(snapshot.Contacts[0].Phase, Is.EqualTo(KillChainPhase.Find));
        Assert.That(snapshot.Transitions.Select(t => t.Kind), Is.EqualTo(new[]
        {
            KillChainTransitionKind.Find,
            KillChainTransitionKind.Lost,
            KillChainTransitionKind.Find,
        }));
    }

    [Test]
    public void Fire_control_lookup_is_contact_keyed_not_ui_state()
    {
        var changes = new[]
        {
            new ContactChangeRecord(7, 1.0, 1, "u1", "c1", "hostile-1", "Unknown", "Identified"),
        };

        var withoutFc = KillChainContactStateProjection.Project(changes, currentSimTick: 2);
        var withFc = KillChainContactStateProjection.Project(
            changes,
            currentSimTick: 2,
            fireControl: new StubFireControl("c1"));

        Assert.That(withoutFc.Contacts[0].Targetable, Is.False);
        Assert.That(withoutFc.Contacts[0].Phase, Is.EqualTo(KillChainPhase.Track));
        Assert.That(withFc.Contacts[0].Targetable, Is.True);
        Assert.That(withFc.Contacts[0].Phase, Is.EqualTo(KillChainPhase.Target));
    }

    private static ContactChangeRecord Change(
        ulong tick,
        string contactId,
        string targetId,
        string previous,
        string next) =>
        new(0, tick, tick, "u1", contactId, targetId, previous, next);

    private static string Canonical(KillChainContactSnapshot snapshot)
    {
        var contacts = string.Join(
            ";",
            snapshot.Contacts.Select(c =>
                string.Join(
                    ",",
                    c.ContactId,
                    c.TargetId,
                    c.ObserverId,
                    c.Phase,
                    c.Loss,
                    c.DetectionCaptured,
                    c.LocationSufficient,
                    c.TrackContinuous,
                    c.Targetable,
                    c.FirstSimTick,
                    c.FirstSimTime.ToString("R"),
                    c.LastSimTick,
                    c.LastSimTime.ToString("R"),
                    c.CorrelationSequenceId,
                    string.Join("|", c.SourceSequenceIds),
                    string.Join("|", c.SourceRefs))));
        var transitions = string.Join(
            ";",
            snapshot.Transitions.Select(t =>
                string.Join(
                    ",",
                    t.Kind,
                    t.ContactId,
                    t.PreviousPhase,
                    t.NewPhase,
                    t.Loss,
                    t.SimTick,
                    t.SimTime.ToString("R"),
                    t.CorrelationSequenceId,
                    string.Join("|", t.SourceRefs))));
        return contacts + "#" + transitions;
    }

    private sealed class StubFireControl : IKillChainFireControlSource
    {
        private readonly HashSet<string> _contactIds;

        public StubFireControl(params string[] contactIds) =>
            _contactIds = new HashSet<string>(contactIds, StringComparer.Ordinal);

        public bool HasFireControlTrack(string contactId, string targetId) =>
            _contactIds.Contains(contactId);
    }
}
