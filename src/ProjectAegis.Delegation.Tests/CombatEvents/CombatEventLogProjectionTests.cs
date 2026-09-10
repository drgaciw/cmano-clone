using ProjectAegis.Delegation.CombatEvents;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Sim.Engage;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.CombatEvents;

public sealed class CombatEventLogProjectionTests
{
    [Test]
    public void Build_uses_enriched_log_facts_for_complete_terminal_leg()
    {
        var log = new DecisionLog();
        log.AppendEngagement(new EngagementRecord(
            0, 2, 2, new TargetId("u1"), 41, true, "Launched",
            new TargetId("hostile-1"), "baltic-rim-66", 2));
        log.AppendEngagementOutcome(new EngagementOutcomeRecord(
            0, 2, 2, new TargetId("u1"), new TargetId("hostile-1"), 41,
            EngagementOutcomeCodes.Kill, 0.1));

        var result = CombatEventLogProjection.Build(log, 2);

        Assert.That(result.Events.Select(e => e.Phase), Is.EqualTo(new[]
        {
            CombatEventPhase.IntentAccepted,
            CombatEventPhase.Authorized,
            CombatEventPhase.Firing,
            CombatEventPhase.TerminalOutcome,
        }));
        Assert.That(result.Events.All(e => e.TargetId == "hostile-1"), Is.True);
        Assert.That(result.Events.All(e => e.WeaponFamilyId == "baltic-rim-66"), Is.True);
        Assert.That(result.Events.All(e => e.CorrelationId == log.Engagements[0].SequenceId), Is.True);
    }

    [Test]
    public void Build_refusal_uses_row_sequence_as_zero_engagement_correlation()
    {
        var log = new DecisionLog();
        log.AppendEngagement(new EngagementRecord(
            0, 3, 3, new TargetId("u1"), 0, false, "ROE_WEAPONS_TIGHT",
            new TargetId("hostile-1"), "baltic-oto-76"));

        var result = CombatEventLogProjection.Build(log, 3);

        Assert.That(result.Events.Select(e => e.Phase), Is.EqualTo(new[]
        {
            CombatEventPhase.IntentAccepted,
            CombatEventPhase.AuthorizationRefused,
        }));
        Assert.That(result.Events[0].CorrelationId, Is.Not.Zero);
        Assert.That(result.Events[1].Outcome, Is.EqualTo("ROE_WEAPONS_TIGHT"));
    }

    [Test]
    public void Build_projects_explicit_unknowns_for_legacy_rows()
    {
        var log = new DecisionLog();
        log.AppendEngagement(new EngagementRecord(
            0, 1, 1, new TargetId("u1"), 1, true, "Launched"));

        var events = CombatEventLogProjection.Build(log, 1).Events;
        Assert.That(events, Is.Not.Empty);
        Assert.That(events.All(e => e.TargetId == CombatEventLogProjection.UnknownTargetId), Is.True);
        Assert.That(events.All(e => e.WeaponFamilyId == CombatEventLogProjection.UnknownWeaponFamilyId), Is.True);
    }

    [Test]
    public void Build_does_not_attach_future_or_prior_outcome_and_only_missile_infers_in_flight()
    {
        var log = new DecisionLog();
        log.AppendEngagementOutcome(new EngagementOutcomeRecord(
            0, 1, 1, new TargetId("u1"), new TargetId("t1"), 8,
            EngagementOutcomeCodes.Kill, 0.1));
        log.AppendEngagement(new EngagementRecord(
            0, 2, 2, new TargetId("u1"), 8, true, "Launched",
            new TargetId("t1"), "Gun"));
        log.AppendEngagementOutcome(new EngagementOutcomeRecord(
            0, 9, 9, new TargetId("u1"), new TargetId("t1"), 8,
            EngagementOutcomeCodes.Hit, 0.2));

        var result = CombatEventLogProjection.Build(log, 3);

        Assert.That(result.Events.Any(e => e.Phase == CombatEventPhase.TerminalOutcome), Is.False);
        Assert.That(result.Events.Any(e => e.Phase == CombatEventPhase.InFlight), Is.False);
    }

    [Test]
    public void Build_surfaces_unsynthesized_policy_denial_with_unknown_leg_facts()
    {
        var log = new DecisionLog();
        log.AppendPolicyDenial(new PolicyDenialRecord(
            0, 4, 4, new AgentId("a1"), new TargetId("u1"), 0,
            ProjectAegis.Sim.Policy.FireAbortReason.CommsDenied, OrderKind.Engage));

        var result = CombatEventLogProjection.Build(log, 4);

        Assert.That(result.Events.Select(e => e.Phase), Is.EqualTo(new[]
        {
            CombatEventPhase.IntentAccepted,
            CombatEventPhase.AuthorizationRefused,
        }));
        Assert.That(result.Events.All(e => e.TargetId == CombatEventLogProjection.UnknownTargetId), Is.True);
    }

    [Test]
    public void Build_keeps_unrelated_refusals_for_same_shooter_and_tick()
    {
        var log = new DecisionLog();
        log.AppendPolicyDenial(new PolicyDenialRecord(
            0, 4, 4, new AgentId("a1"), new TargetId("u1"), 0,
            ProjectAegis.Sim.Policy.FireAbortReason.CommsDenied, OrderKind.Engage));
        log.AppendPolicyDenial(new PolicyDenialRecord(
            0, 4, 4, new AgentId("a1"), new TargetId("u1"), 0,
            ProjectAegis.Sim.Policy.FireAbortReason.RoeHoldFire, OrderKind.Engage));
        log.AppendEngagement(new EngagementRecord(
            0, 4, 4, new TargetId("u1"), 0, false, "ROE_WEAPONS_TIGHT",
            new TargetId("t1"), "Missile"));

        var refusals = CombatEventLogProjection.Build(log, 4).Events
            .Where(e => e.Phase == CombatEventPhase.AuthorizationRefused)
            .ToArray();

        Assert.That(refusals.Select(e => e.Outcome), Is.EquivalentTo(new[]
        {
            nameof(ProjectAegis.Sim.Policy.FireAbortReason.CommsDenied),
            nameof(ProjectAegis.Sim.Policy.FireAbortReason.RoeHoldFire),
            "ROE_WEAPONS_TIGHT",
        }));
    }

    [Test]
    public void Build_does_not_deduplicate_against_future_fractional_time_engagement()
    {
        var log = new DecisionLog();
        log.AppendPolicyDenial(new PolicyDenialRecord(
            0, 4.1, 4, new AgentId("a1"), new TargetId("u1"), 0,
            ProjectAegis.Sim.Policy.FireAbortReason.WeaponsTight, OrderKind.Engage));
        log.AppendEngagement(new EngagementRecord(
            0, 4.9, 4, new TargetId("u1"), 0, false, "ROE_WEAPONS_TIGHT",
            new TargetId("t1"), "Missile"));

        var refusals = CombatEventLogProjection.Build(log, 4.5).Events
            .Where(e => e.Phase == CombatEventPhase.AuthorizationRefused)
            .ToArray();

        Assert.That(refusals, Has.Length.EqualTo(1));
        Assert.That(refusals[0].Outcome, Is.EqualTo(nameof(ProjectAegis.Sim.Policy.FireAbortReason.WeaponsTight)));
    }

    [Test]
    public void Build_deduplicates_exact_preceding_weapons_tight_surface_pair()
    {
        var log = new DecisionLog();
        log.AppendPolicyDenial(new PolicyDenialRecord(
            0, 4, 4, new AgentId("a1"), new TargetId("u1"), 0,
            ProjectAegis.Sim.Policy.FireAbortReason.WeaponsTight, OrderKind.Engage));
        log.AppendEngagement(new EngagementRecord(
            0, 4, 4, new TargetId("u1"), 0, false, "ROE_WEAPONS_TIGHT",
            new TargetId("t1"), "Missile"));

        var refusals = CombatEventLogProjection.Build(log, 4).Events
            .Where(e => e.Phase == CombatEventPhase.AuthorizationRefused)
            .ToArray();

        Assert.That(refusals, Has.Length.EqualTo(1));
        Assert.That(refusals[0].TargetId, Is.EqualTo("t1"));
        Assert.That(refusals[0].ExplanationRef, Is.EqualTo("policy:WeaponsTight"));
    }

    [Test]
    public void Build_reused_engagement_id_does_not_attach_later_attempt_outcome_to_earlier_leg()
    {
        var log = new DecisionLog();
        log.AppendEngagement(new EngagementRecord(
            0, 1, 1, new TargetId("u1"), 7, true, "Launched",
            new TargetId("t1"), "Missile"));
        log.AppendEngagement(new EngagementRecord(
            0, 2, 2, new TargetId("u1"), 7, true, "Launched",
            new TargetId("t1"), "Missile"));
        log.AppendEngagementOutcome(new EngagementOutcomeRecord(
            0, 2.1, 2, new TargetId("u1"), new TargetId("t1"), 7,
            EngagementOutcomeCodes.Kill, 0.1));

        var result = CombatEventLogProjection.Build(log, 3);
        var legs = result.Events.GroupBy(e => e.CorrelationId).OrderBy(g => g.Key).ToArray();

        Assert.That(legs[0].Any(e => e.Phase == CombatEventPhase.TerminalOutcome), Is.False);
        Assert.That(legs[0].Any(e => e.Phase == CombatEventPhase.InFlight), Is.True);
        Assert.That(legs[1].Any(e => e.Phase == CombatEventPhase.TerminalOutcome), Is.True);
    }

    [Test]
    public void Build_large_log_preserves_all_grouped_terminal_legs()
    {
        var log = new DecisionLog();
        const int count = 1200;
        for (var i = 0; i < count; i++)
        {
            var shooter = new TargetId($"u{i}");
            var victim = new TargetId($"t{i}");
            var engagementId = (ulong)(i + 1);
            log.AppendEngagement(new EngagementRecord(
                0, i, (ulong)i, shooter, engagementId, true, "Launched", victim, "Missile"));
            log.AppendEngagementOutcome(new EngagementOutcomeRecord(
                0, i + 0.1, (ulong)i, shooter, victim, engagementId,
                EngagementOutcomeCodes.Hit, 0.5));
        }

        var result = CombatEventLogProjection.Build(log, double.MaxValue);

        Assert.That(result.Events.Count(e => e.Phase == CombatEventPhase.TerminalOutcome), Is.EqualTo(count));
        Assert.That(result.Events.Select(e => e.CorrelationId).Distinct().Count(), Is.EqualTo(count));
    }
}
