namespace ProjectAegis.Delegation.CombatEvents;

using Core;
using Decision;
using Projection;
using ProjectAegis.Sim.Policy;
using TargetabilityAccept;

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
    /// <param name="input">Explicit intent / authority / preview facts.</param>
    /// <param name="log">Authoritative order log; null when no engagement evidence exists yet.</param>
    /// <param name="targetability">
    /// Authoritative Slice A acceptance snapshot (DRG-183/219). When supplied, a withheld row — or no row for
    /// the shooter/target leg — refuses authorization with the named Slice A cause. Null keeps the pre-Slice-A behaviour.
    /// </param>
    public static CombatEventSnapshot Project(
        CombatEngageAssessInput input,
        DecisionLog? log = null,
        TargetabilityAcceptSnapshot? targetability = null)
    {
        if (!input.IntentAccepted)
        {
            return CombatEventSnapshot.Empty;
        }

        var targetabilityRow = FindTargetabilityRow(targetability, input.ShooterId, input.TargetId);
        var facts = targetabilityRow is null
            ? Array.Empty<CombatTargetabilityFact>()
            : new[] { CombatTargetabilityFact.FromRow(targetabilityRow) };
        var engagement = FindEngagement(log, input);
        var execution = engagement is null
            ? Array.Empty<CombatExecutionFact>()
            : new[] { CreateExecutionFact(input, engagement) };
        return ProjectEvents(input, log, targetability, targetabilityRow, engagement) with
        {
            Targetability = facts,
            Execution = execution,
        };
    }

    private static CombatEventSnapshot ProjectEvents(
        CombatEngageAssessInput input,
        DecisionLog? log,
        TargetabilityAcceptSnapshot? targetability,
        TargetabilityAcceptContactRow? targetabilityRow,
        EngagementRecord? engagement)
    {
        var events = new List<CombatEvent>(6);
        events.Add(CreateEvent(
            input,
            CombatEventPhase.IntentAccepted,
            OutcomeIntentAccepted,
            input.SimTick,
            input.SimTime,
            ExplanationIntentAccepted));

        var refusal = ResolveAuthorizationRefusal(input, log, targetability, targetabilityRow);
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

        if (HasAffirmativeAuthorization(input, engagement))
        {
            events.Add(CreateEvent(
                input,
                CombatEventPhase.Authorized,
                OutcomeAuthorized,
                input.SimTick,
                input.SimTime,
                EngageExplainProjection.CanFireLabel));
        }

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
            input.CorrelationId,
            simTime,
            simTick,
            explanationRef);

    private sealed record AuthorizationRefusal(string Outcome, string ExplanationRef, ulong SimTick, double SimTime);

    /// <summary>
    /// Refusal precedence: shooter-scoped sim policy denial, then Slice A targetability, then preview abort.
    /// </summary>
    private static AuthorizationRefusal? ResolveAuthorizationRefusal(
        CombatEngageAssessInput input,
        DecisionLog? log,
        TargetabilityAcceptSnapshot? targetability,
        TargetabilityAcceptContactRow? targetabilityRow)
    {
        var policyDenial = FindPolicyDenial(log, input);
        if (policyDenial is not null)
        {
            var reason = policyDenial.Reason.ToString();
            return new AuthorizationRefusal(
                reason,
                BuildPolicyExplanationRef(policyDenial.Reason),
                policyDenial.SimTick,
                policyDenial.SimTime);
        }

        if (targetability is not null)
        {
            var cause = targetabilityRow is null
                ? TargetabilityAcceptCauseCodes.MissingProvenance
                : targetabilityRow.Disposition == TargetabilityAcceptDisposition.Withheld
                    ? targetabilityRow.WithheldCauseCode
                    : null;
            if (cause is not null)
            {
                return new AuthorizationRefusal(
                    cause,
                    BuildTargetabilityExplanationRef(cause),
                    input.SimTick,
                    input.SimTime);
            }
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

    /// <summary>
    /// Authorization requires affirmative preview or a matching launched engagement in the log.
    /// </summary>
    private static bool HasAffirmativeAuthorization(CombatEngageAssessInput input, EngagementRecord? engagement) =>
        input.Preview is { CanFire: true } || engagement is { Launched: true };

    /// <summary>
    /// First permitted Slice A row for the exact shooter/target leg; otherwise its first row.
    /// An unscoped contact-wide track refusal is the final fallback, never permission or actor authority.
    /// </summary>
    private static TargetabilityAcceptContactRow? FindTargetabilityRow(
        TargetabilityAcceptSnapshot? targetability,
        string shooterId,
        string targetId)
    {
        if (targetability is null)
        {
            return null;
        }

        TargetabilityAcceptContactRow? first = null;
        TargetabilityAcceptContactRow? contactRefusal = null;
        for (var i = 0; i < targetability.Contacts.Count; i++)
        {
            var row = targetability.Contacts[i];
            if (!string.Equals(row.TargetId, targetId, StringComparison.Ordinal))
            {
                continue;
            }

            if (!string.Equals(CombatTargetabilityFact.ShooterIdFromRow(row), shooterId, StringComparison.Ordinal))
            {
                if (CombatTargetabilityFact.IsContactWideRefusalRow(row)) contactRefusal ??= row;
                continue;
            }

            if (row.Disposition == TargetabilityAcceptDisposition.Permitted)
            {
                return row;
            }

            first ??= row;
        }

        return first ?? contactRefusal;
    }

    private static CombatExecutionFact CreateExecutionFact(
        CombatEngageAssessInput input,
        EngagementRecord engagement) =>
        new(
            input.CorrelationId,
            input.ShooterId,
            input.TargetId,
            input.WeaponFamilyId,
            engagement.HasFireControlTrack,
            engagement.SalvoSize,
            engagement.SimTime,
            engagement.SimTick);

    /// <summary>
    /// Policy denials record the commanded unit on <see cref="PolicyDenialRecord.TargetId"/> (see
    /// <c>AgentController</c> / <c>SimulationSession</c>), not the hostile victim id.
    /// </summary>
    private static PolicyDenialRecord? FindPolicyDenial(DecisionLog? log, CombatEngageAssessInput input)
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

            if (!string.Equals(denial.TargetId.Value, input.ShooterId, StringComparison.Ordinal))
            {
                continue;
            }

            if (denial.SimTick < input.SimTick)
            {
                continue;
            }

            latest = denial;
        }

        return latest;
    }

    private static EngagementRecord? FindEngagement(DecisionLog? log, CombatEngageAssessInput input)
    {
        if (log is null || input.CorrelationId == 0)
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

            if (engagement.EngagementId != input.CorrelationId)
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

    internal static string BuildTargetabilityExplanationRef(string cause) => $"targetability:{cause}";

    private static string BuildOutcomeExplanationRef(string outcomeCode) => $"outcome:{outcomeCode}";
}
