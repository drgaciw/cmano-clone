namespace ProjectAegis.Delegation.CombatEvents;

using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Sim.Engage;
using ProjectAegis.Sim.Policy;

/// <summary>
/// DRG-211: folds explicit engage-assess intent/authority/preview input with order-log engagement rows
/// into a deterministic combat-event snapshot. Presentation-only — does not enqueue orders or resolve combat.
/// </summary>
public static class CombatEventProjection
{
    public const string OutcomeIntentAccepted = "IntentAccepted";
    public const string OutcomeAuthorized = "Authorized";
    public const string OutcomeLaunch = "Launch";
    public const string OutcomeInFlight = "InFlight";
    public const string ExplanationIntentAccepted = "engage-assess:intent-accepted";
    public const string ExplanationLaunch = "engage-assess:launch";
    public const string ExplanationInFlight = "engage-assess:in-flight";

    /// <summary>
    /// Projects the combat-event lifecycle for one shooter/target leg. Never emits a silent authorization deny.
    /// </summary>
    public static CombatEventSnapshot Project(CombatEngageAssessInput input, DecisionLog? log = null)
    {
        if (!input.IntentAccepted)
        {
            return CombatEventSnapshot.Empty;
        }

        var events = new List<CombatEvent>(6);
        events.Add(CreateEvent(
            input,
            CombatEventPhase.IntentAccepted,
            OutcomeIntentAccepted,
            input.SimTick,
            input.SimTime,
            ExplanationIntentAccepted));

        var refusal = ResolveAuthorizationRefusal(input, log);
        if (refusal is not null)
        {
            events.Add(CreateEvent(
                input,
                CombatEventPhase.AuthorizationRefused,
                refusal.Outcome,
                refusal.SimTick,
                refusal.SimTime,
                refusal.ExplanationRef));
            return new CombatEventSnapshot(events);
        }

        events.Add(CreateEvent(
            input,
            CombatEventPhase.Authorized,
            OutcomeAuthorized,
            input.SimTick,
            input.SimTime,
            EngageExplainProjection.CanFireLabel));

        var engagement = FindEngagement(log, input);
        if (engagement is null)
        {
            return new CombatEventSnapshot(events);
        }

        if (!engagement.Launched)
        {
            var abortCode = engagement.AbortReasonCode ?? "ENGAGE_ABORT";
            events.Add(CreateEvent(
                input,
                CombatEventPhase.AuthorizationRefused,
                abortCode,
                engagement.SimTick,
                engagement.SimTime,
                BuildAbortExplanationRef(abortCode)));
            return new CombatEventSnapshot(events);
        }

        events.Add(CreateEvent(
            input,
            CombatEventPhase.Firing,
            OutcomeLaunch,
            engagement.SimTick,
            engagement.SimTime,
            ExplanationLaunch));

        var outcome = FindOutcome(log, input.ShooterId, engagement.EngagementId);
        if (outcome is null)
        {
            events.Add(CreateEvent(
                input,
                CombatEventPhase.InFlight,
                OutcomeInFlight,
                engagement.SimTick,
                engagement.SimTime,
                ExplanationInFlight));
            return new CombatEventSnapshot(events);
        }

        events.Add(CreateEvent(
            input,
            CombatEventPhase.TerminalOutcome,
            outcome.OutcomeCode,
            outcome.SimTick,
            outcome.SimTime,
            BuildOutcomeExplanationRef(outcome.OutcomeCode)));
        return new CombatEventSnapshot(events);
    }

    private static CombatEvent CreateEvent(
        CombatEngageAssessInput input,
        CombatEventPhase phase,
        string outcome,
        ulong simTick,
        double simTime,
        string explanationRef) =>
        new(
            phase,
            input.ShooterId,
            input.TargetId,
            input.WeaponFamilyId,
            outcome,
            ResolveCorrelationId(input, phase),
            simTime,
            simTick,
            explanationRef);

    private static ulong ResolveCorrelationId(CombatEngageAssessInput input, CombatEventPhase phase) =>
        phase switch
        {
            CombatEventPhase.IntentAccepted or CombatEventPhase.Authorized or CombatEventPhase.AuthorizationRefused
                => input.CorrelationId,
            _ => input.CorrelationId,
        };

    private sealed record AuthorizationRefusal(string Outcome, string ExplanationRef, ulong SimTick, double SimTime);

    private static AuthorizationRefusal? ResolveAuthorizationRefusal(
        CombatEngageAssessInput input,
        DecisionLog? log)
    {
        var policyDenial = FindPolicyDenial(log, input.TargetId);
        if (policyDenial is not null)
        {
            var reason = policyDenial.Reason.ToString();
            return new AuthorizationRefusal(
                reason,
                BuildPolicyExplanationRef(policyDenial.Reason),
                policyDenial.SimTick,
                policyDenial.SimTime);
        }

        if (input.Preview is { CanFire: false })
        {
            var code = input.Preview.AbortPreviewCode ?? "ENGAGE_BLOCKED";
            return new AuthorizationRefusal(
                code,
                BuildAbortExplanationRef(code),
                input.SimTick,
                input.SimTime);
        }

        return null;
    }

    private static PolicyDenialRecord? FindPolicyDenial(DecisionLog? log, string targetId)
    {
        if (log is null)
        {
            return null;
        }

        PolicyDenialRecord? latest = null;
        for (var i = 0; i < log.PolicyDenials.Count; i++)
        {
            var denial = log.PolicyDenials[i];
            if (denial.AttemptedKind != OrderKind.Engage)
            {
                continue;
            }

            if (!string.Equals(denial.TargetId.Value, targetId, StringComparison.Ordinal))
            {
                continue;
            }

            latest = denial;
        }

        return latest;
    }

    private static EngagementRecord? FindEngagement(DecisionLog? log, CombatEngageAssessInput input)
    {
        if (log is null)
        {
            return null;
        }

        EngagementRecord? latest = null;
        for (var i = 0; i < log.Engagements.Count; i++)
        {
            var engagement = log.Engagements[i];
            if (!string.Equals(engagement.ShooterTargetId.Value, input.ShooterId, StringComparison.Ordinal))
            {
                continue;
            }

            if (input.CorrelationId != 0 && engagement.EngagementId != input.CorrelationId)
            {
                continue;
            }

            latest = engagement;
        }

        return latest;
    }

    private static EngagementOutcomeRecord? FindOutcome(
        DecisionLog? log,
        string shooterId,
        ulong engagementId)
    {
        if (log is null)
        {
            return null;
        }

        EngagementOutcomeRecord? latest = null;
        for (var i = 0; i < log.EngagementOutcomes.Count; i++)
        {
            var outcome = log.EngagementOutcomes[i];
            if (outcome.EngagementId != engagementId)
            {
                continue;
            }

            if (!string.Equals(outcome.ShooterTargetId.Value, shooterId, StringComparison.Ordinal))
            {
                continue;
            }

            latest = outcome;
        }

        return latest;
    }

    private static string BuildAbortExplanationRef(string code) => $"abort:{code}";

    private static string BuildPolicyExplanationRef(FireAbortReason reason) => $"policy:{reason}";

    private static string BuildOutcomeExplanationRef(string outcomeCode) => $"outcome:{outcomeCode}";
}
