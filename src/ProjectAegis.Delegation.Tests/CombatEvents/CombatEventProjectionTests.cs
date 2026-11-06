using ProjectAegis.Data.Catalog;
using ProjectAegis.Delegation.CombatEvents;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Sim.Engage;
using ProjectAegis.Sim.Glossary;
using ProjectAegis.Sim.Policy;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.CombatEvents;

public sealed class CombatEventProjectionTests
{
    [Test]
    public void Permitted_path_emits_intent_authorized_firing_and_terminal_outcome()
    {
        var input = new CombatEngageAssessInput(
            ShooterId: "u1",
            TargetId: "hostile-1",
            WeaponFamilyId: CatalogWeaponIds.MvpDefault,
            IntentAccepted: true,
            SimTick: 1,
            SimTime: 1.0,
            CorrelationId: 42,
            Preview: new EngagePreview("DLZ: In", CanFire: true, AbortPreviewCode: null));

        var log = new DecisionLog();
        log.AppendEngagement(new EngagementRecord(
            1, 1.5, 2, new TargetId("u1"), 42, Launched: true));
        log.AppendEngagementOutcome(new EngagementOutcomeRecord(
            2, 3.0, 5, new TargetId("u1"), new TargetId("hostile-1"), 42,
            EngagementOutcomeCodes.Kill, 0.1));

        var snapshot = CombatEventProjection.Project(input, log);

        Assert.That(snapshot.Events.Select(e => e.Phase), Is.EqualTo(new[]
        {
            CombatEventPhase.IntentAccepted,
            CombatEventPhase.Authorized,
            CombatEventPhase.Firing,
            CombatEventPhase.TerminalOutcome,
        }));

        var terminal = snapshot.Events[^1];
        Assert.That(terminal.ShooterId, Is.EqualTo("u1"));
        Assert.That(terminal.TargetId, Is.EqualTo("hostile-1"));
        Assert.That(terminal.WeaponFamilyId, Is.EqualTo(CatalogWeaponIds.MvpDefault));
        Assert.That(terminal.Outcome, Is.EqualTo(EngagementOutcomeCodes.Kill));
        Assert.That(terminal.CorrelationId, Is.EqualTo(42UL));
        Assert.That(terminal.SimTime, Is.EqualTo(3.0));
        Assert.That(terminal.ExplanationRef, Is.EqualTo("outcome:Kill"));
    }

    [Test]
    public void Refused_path_emits_explicit_authorization_refusal_from_preview_abort()
    {
        var input = new CombatEngageAssessInput(
            ShooterId: "u1",
            TargetId: "hostile-1",
            WeaponFamilyId: CatalogWeaponIds.MvpDefault,
            IntentAccepted: true,
            SimTick: 1,
            SimTime: 1.0,
            CorrelationId: 7,
            Preview: new EngagePreview(
                "DLZ: Out",
                CanFire: false,
                AbortPreviewCode: AbortReasonCatalog.Engage.DLZ_OUT));

        var snapshot = CombatEventProjection.Project(input, log: null);

        Assert.That(snapshot.Events.Select(e => e.Phase), Is.EqualTo(new[]
        {
            CombatEventPhase.IntentAccepted,
            CombatEventPhase.AuthorizationRefused,
        }));

        var refused = snapshot.Events[^1];
        Assert.That(refused.Outcome, Is.EqualTo(AbortReasonCatalog.Engage.DLZ_OUT));
        Assert.That(refused.ExplanationRef, Is.EqualTo($"abort:{AbortReasonCatalog.Engage.DLZ_OUT}"));
        Assert.That(refused.ShooterId, Is.EqualTo("u1"));
        Assert.That(refused.TargetId, Is.EqualTo("hostile-1"));
    }

    [Test]
    public void Refused_path_emits_explicit_policy_denial_reason_for_shooter_scoped_log_row()
    {
        var input = new CombatEngageAssessInput(
            ShooterId: "u1",
            TargetId: "hostile-1",
            WeaponFamilyId: CatalogWeaponIds.MvpDefault,
            IntentAccepted: true,
            SimTick: 1,
            SimTime: 1.0,
            CorrelationId: 9,
            Preview: new EngagePreview("DLZ: In", CanFire: true, AbortPreviewCode: null));

        var log = new DecisionLog();
        log.AppendPolicyDenial(new PolicyDenialRecord(
            1, 1.2, 2,
            new AgentId("a1"),
            new TargetId("u1"),
            0,
            FireAbortReason.RoeHoldFire,
            OrderKind.Engage));

        var snapshot = CombatEventProjection.Project(input, log);

        Assert.That(snapshot.Events.Select(e => e.Phase), Is.EqualTo(new[]
        {
            CombatEventPhase.IntentAccepted,
            CombatEventPhase.AuthorizationRefused,
        }));

        var refused = snapshot.Events[^1];
        Assert.That(refused.Outcome, Is.EqualTo(nameof(FireAbortReason.RoeHoldFire)));
        Assert.That(refused.ExplanationRef, Is.EqualTo($"policy:{FireAbortReason.RoeHoldFire}"));
    }

    [Test]
    public void Victim_scoped_policy_denial_does_not_suppress_authorization()
    {
        var input = new CombatEngageAssessInput(
            ShooterId: "u1",
            TargetId: "hostile-2",
            WeaponFamilyId: CatalogWeaponIds.MvpDefault,
            IntentAccepted: true,
            SimTick: 10,
            SimTime: 10.0,
            CorrelationId: 20,
            Preview: new EngagePreview("DLZ: In", CanFire: true, AbortPreviewCode: null));

        var log = new DecisionLog();
        log.AppendPolicyDenial(new PolicyDenialRecord(
            1, 9.0, 9,
            new AgentId("a1"),
            new TargetId("hostile-1"),
            0,
            FireAbortReason.RoeHoldFire,
            OrderKind.Engage));

        var snapshot = CombatEventProjection.Project(input, log);

        Assert.That(snapshot.Events.Select(e => e.Phase), Is.EqualTo(new[]
        {
            CombatEventPhase.IntentAccepted,
            CombatEventPhase.Authorized,
        }));
    }

    [Test]
    public void Stale_shooter_scoped_policy_denial_does_not_apply_to_later_attempt()
    {
        var log = new DecisionLog();
        log.AppendPolicyDenial(new PolicyDenialRecord(
            1, 1.0, 1,
            new AgentId("a1"),
            new TargetId("u1"),
            0,
            FireAbortReason.RoeHoldFire,
            OrderKind.Engage));

        var input = new CombatEngageAssessInput(
            ShooterId: "u1",
            TargetId: "hostile-1",
            WeaponFamilyId: CatalogWeaponIds.MvpDefault,
            IntentAccepted: true,
            SimTick: 5,
            SimTime: 5.0,
            CorrelationId: 30,
            Preview: new EngagePreview("DLZ: In", CanFire: true, AbortPreviewCode: null));

        var snapshot = CombatEventProjection.Project(input, log);

        Assert.That(snapshot.Events.Select(e => e.Phase), Is.EqualTo(new[]
        {
            CombatEventPhase.IntentAccepted,
            CombatEventPhase.Authorized,
        }));
    }

    [Test]
    public void Launch_without_outcome_emits_in_flight()
    {
        var input = new CombatEngageAssessInput(
            "u1",
            "hostile-1",
            CatalogWeaponIds.MvpDefault,
            IntentAccepted: true,
            SimTick: 1,
            SimTime: 1.0,
            CorrelationId: 55,
            Preview: new EngagePreview("DLZ: In", true, null));

        var log = new DecisionLog();
        log.AppendEngagement(new EngagementRecord(
            1, 2.0, 3, new TargetId("u1"), 55, Launched: true));

        var snapshot = CombatEventProjection.Project(input, log);

        Assert.That(snapshot.Events.Select(e => e.Phase), Is.EqualTo(new[]
        {
            CombatEventPhase.IntentAccepted,
            CombatEventPhase.Authorized,
            CombatEventPhase.Firing,
            CombatEventPhase.InFlight,
        }));
    }

    [Test]
    public void Zero_correlation_does_not_attach_prior_same_shooter_engagement()
    {
        var log = new DecisionLog();
        log.AppendEngagement(new EngagementRecord(
            1, 2.0, 3, new TargetId("u1"), 99, Launched: true));
        log.AppendEngagementOutcome(new EngagementOutcomeRecord(
            2, 4.0, 5, new TargetId("u1"), new TargetId("hostile-old"), 99,
            EngagementOutcomeCodes.Kill, 0.1));

        var input = new CombatEngageAssessInput(
            "u1",
            "hostile-new",
            CatalogWeaponIds.MvpDefault,
            IntentAccepted: true,
            SimTick: 10,
            SimTime: 10.0,
            CorrelationId: 0,
            Preview: new EngagePreview("DLZ: In", true, null));

        var snapshot = CombatEventProjection.Project(input, log);

        Assert.That(snapshot.Events.Select(e => e.Phase), Is.EqualTo(new[]
        {
            CombatEventPhase.IntentAccepted,
            CombatEventPhase.Authorized,
        }));
    }

    [Test]
    public void Null_preview_without_log_evidence_omits_authorized()
    {
        var input = new CombatEngageAssessInput(
            "u1",
            "hostile-1",
            CatalogWeaponIds.MvpDefault,
            IntentAccepted: true,
            SimTick: 1,
            SimTime: 1.0,
            CorrelationId: 12,
            Preview: null);

        var snapshot = CombatEventProjection.Project(input, log: null);

        Assert.That(snapshot.Events.Select(e => e.Phase), Is.EqualTo(new[]
        {
            CombatEventPhase.IntentAccepted,
        }));
    }

    [Test]
    public void Snapshot_event_list_is_immutable_after_construction()
    {
        var events = new List<CombatEvent>
        {
            new(
                CombatEventPhase.IntentAccepted,
                "u1",
                "hostile-1",
                CatalogWeaponIds.MvpDefault,
                CombatEventProjection.OutcomeIntentAccepted,
                1,
                1.0,
                1,
                CombatEventProjection.ExplanationIntentAccepted),
        };

        var snapshot = new CombatEventSnapshot(events);
        events.Add(new CombatEvent(
            CombatEventPhase.Authorized,
            "u1",
            "hostile-1",
            CatalogWeaponIds.MvpDefault,
            CombatEventProjection.OutcomeAuthorized,
            1,
            1.0,
            1,
            EngageExplainProjection.CanFireLabel));

        Assert.That(snapshot.Events, Has.Count.EqualTo(1));
        Assert.That(snapshot.Events[0].Phase, Is.EqualTo(CombatEventPhase.IntentAccepted));
    }

    [Test]
    public void Fingerprint_is_identical_for_identical_inputs()
    {
        var input = new CombatEngageAssessInput(
            "u1",
            "hostile-1",
            CatalogWeaponIds.MvpDefault,
            IntentAccepted: true,
            SimTick: 4,
            SimTime: 4.5,
            CorrelationId: 100,
            Preview: new EngagePreview("DLZ: In", true, null));

        var log = new DecisionLog();
        log.AppendEngagement(new EngagementRecord(
            1, 5.0, 6, new TargetId("u1"), 100, Launched: true));
        log.AppendEngagementOutcome(new EngagementOutcomeRecord(
            2, 7.0, 8, new TargetId("u1"), new TargetId("hostile-1"), 100,
            EngagementOutcomeCodes.Hit, 0.2));

        var first = CombatEventProjection.Project(input, log);
        var second = CombatEventProjection.Project(input, log);

        Assert.That(
            CombatEventFingerprint.Compute(first),
            Is.EqualTo(CombatEventFingerprint.Compute(second)));
    }

    [Test]
    public void Dto_surface_omits_ui_derived_truth_fields()
    {
        var uiDerivedNames = new[]
        {
            "Selection",
            "Hover",
            "Camera",
            "Panel",
            "Visible",
            "Chrome",
            "IsSelected",
        };

        foreach (var type in new[]
                 {
                     typeof(CombatEvent),
                     typeof(CombatEngageAssessInput),
                     typeof(CombatEventSnapshot),
                 })
        {
            foreach (var prop in type.GetProperties())
            {
                foreach (var forbidden in uiDerivedNames)
                {
                    Assert.That(
                        prop.Name.Contains(forbidden, StringComparison.OrdinalIgnoreCase),
                        Is.False,
                        $"{type.Name}.{prop.Name} must not encode UI-derived truth");
                }
            }
        }
    }
}
