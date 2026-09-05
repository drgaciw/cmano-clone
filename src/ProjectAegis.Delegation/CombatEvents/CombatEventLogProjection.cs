namespace ProjectAegis.Delegation.CombatEvents;

using ProjectAegis.Delegation.Decision;
using ProjectAegis.Sim.Engage;
using ProjectAegis.Sim.Policy;

/// <summary>
/// Builds presentation combat events strictly from authoritative, enriched order-log rows.
/// Legacy rows remain visible with explicit unknown target or weapon-family facts.
/// </summary>
public static class CombatEventLogProjection
{
    public const string UnknownTargetId = "unknown-target";
    public const string UnknownWeaponFamilyId = "Unknown";

    /// <summary>Builds a replay-stable combat-event snapshot through the supplied simulation time.</summary>
    /// <remarks>Correlation ids are order-log sequence ids, not resolver engagement ids.</remarks>
    public static CombatEventSnapshot Build(DecisionLog? log, double simTime)
    {
        if (log is null)
        {
            return CombatEventSnapshot.Empty;
        }

        var events = new List<(CombatEvent Event, ulong Sequence)>();
        var engagements = log.Engagements
                     .Where(e => e.SimTime <= simTime)
                     .OrderBy(e => e.SimTick)
                     .ThenBy(e => e.SequenceId)
                     .ToArray();
        var outcomesByEngagement = log.EngagementOutcomes
            .Where(o => o.SimTime <= simTime)
            .GroupBy(o => (o.ShooterTargetId.Value, o.EngagementId))
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(o => o.SequenceId).ToArray());
        var nextAttemptBySequence = new Dictionary<ulong, ulong>();
        foreach (var group in engagements.GroupBy(e => (e.ShooterTargetId.Value, e.EngagementId)))
        {
            var attempts = group.OrderBy(e => e.SequenceId).ToArray();
            for (var i = 0; i + 1 < attempts.Length; i++)
            {
                nextAttemptBySequence[attempts[i].SequenceId] = attempts[i + 1].SequenceId;
            }
        }

        var surfacedWeaponsTight = engagements
            .Where(e => !e.Launched
                && e.AbortReasonCode == EngagementAbortReasonCodes.ToLogCode(
                    EngagementAbortReason.WeaponsTight))
            .GroupBy(e => (e.ShooterTargetId.Value, e.SimTime, e.AbortReasonCode))
            .ToDictionary(g => g.Key, g => g.Min(e => e.SequenceId));

        foreach (var engagement in engagements)
        {
            var victim = engagement.VictimTargetId?.Value ?? UnknownTargetId;
            var correlationId = engagement.SequenceId;
            Add(events, engagement, victim, correlationId,
                CombatEventPhase.IntentAccepted,
                CombatEventProjection.OutcomeIntentAccepted,
                CombatEventProjection.ExplanationIntentAccepted);

            if (!engagement.Launched)
            {
                var abort = engagement.AbortReasonCode ?? "ENGAGE_ABORT";
                Add(events, engagement, victim, correlationId,
                    CombatEventPhase.AuthorizationRefused, abort, ResolveAbortExplanation(abort));
                continue;
            }

            Add(events, engagement, victim, correlationId,
                CombatEventPhase.Authorized,
                CombatEventProjection.OutcomeAuthorized,
                ProjectAegis.Delegation.Projection.EngageExplainProjection.CanFireLabel);
            Add(events, engagement, victim, correlationId,
                CombatEventPhase.Firing,
                CombatEventProjection.OutcomeLaunch,
                CombatEventProjection.ExplanationLaunch);

            var nextMatchingEngagementSequence = nextAttemptBySequence.TryGetValue(
                engagement.SequenceId, out var nextSequence)
                ? nextSequence
                : ulong.MaxValue;
            outcomesByEngagement.TryGetValue(
                (engagement.ShooterTargetId.Value, engagement.EngagementId), out var candidateOutcomes);
            var outcome = (candidateOutcomes ?? Array.Empty<EngagementOutcomeRecord>())
                .Where(o => o.SimTime <= simTime
                    && o.SimTime >= engagement.SimTime
                    && o.SequenceId > engagement.SequenceId
                    && o.SequenceId < nextMatchingEngagementSequence
                    && o.EngagementId == engagement.EngagementId
                    && o.ShooterTargetId == engagement.ShooterTargetId
                    && (engagement.VictimTargetId is null || o.VictimTargetId == engagement.VictimTargetId.Value))
                .OrderBy(o => o.SequenceId)
                .FirstOrDefault();
            if (outcome is null)
            {
                if (string.Equals(
                        ResolveWeaponFamily(engagement), "Missile", StringComparison.OrdinalIgnoreCase))
                {
                    Add(events, engagement, victim, correlationId,
                        CombatEventPhase.InFlight,
                        CombatEventProjection.OutcomeInFlight,
                        CombatEventProjection.ExplanationInFlight);
                }
            }
            else
            {
                events.Add((new CombatEvent(
                    CombatEventPhase.TerminalOutcome,
                    engagement.ShooterTargetId.Value,
                    victim,
                    ResolveWeaponFamily(engagement),
                    outcome.OutcomeCode,
                    correlationId,
                    outcome.SimTime,
                    outcome.SimTick,
                    $"outcome:{outcome.OutcomeCode}"), outcome.SequenceId));
            }
        }

        foreach (var denial in log.PolicyDenials
                     .Where(d => d.SimTime <= simTime && d.AttemptedKind == ProjectAegis.Delegation.Core.OrderKind.Engage)
                     .OrderBy(d => d.SimTick)
                     .ThenBy(d => d.SequenceId))
        {
            var surfacedReason = EngagementAbortReasonCodes.ToLogCode(EngagementAbortReason.WeaponsTight);
            if (denial.Reason == FireAbortReason.WeaponsTight
                && surfacedWeaponsTight.TryGetValue(
                    (denial.TargetId.Value, denial.SimTime, surfacedReason), out var surfacedSequence)
                && denial.SequenceId < surfacedSequence)
            {
                continue;
            }

            var reason = denial.Reason.ToString();
            events.Add((new CombatEvent(
                CombatEventPhase.IntentAccepted,
                denial.TargetId.Value,
                UnknownTargetId,
                UnknownWeaponFamilyId,
                CombatEventProjection.OutcomeIntentAccepted,
                denial.SequenceId,
                denial.SimTime,
                denial.SimTick,
                CombatEventProjection.ExplanationIntentAccepted), denial.SequenceId));
            events.Add((new CombatEvent(
                CombatEventPhase.AuthorizationRefused,
                denial.TargetId.Value,
                UnknownTargetId,
                UnknownWeaponFamilyId,
                reason,
                denial.SequenceId,
                denial.SimTime,
                denial.SimTick,
                $"policy:{reason}"), denial.SequenceId));
        }

        events.Sort(static (a, b) =>
        {
            var time = a.Event.SimTime.CompareTo(b.Event.SimTime);
            if (time != 0) return time;
            var tick = a.Event.SimTick.CompareTo(b.Event.SimTick);
            if (tick != 0) return tick;
            var sequence = a.Sequence.CompareTo(b.Sequence);
            return sequence != 0 ? sequence : a.Event.Phase.CompareTo(b.Event.Phase);
        });

        return events.Count == 0
            ? CombatEventSnapshot.Empty
            : new CombatEventSnapshot(events.Select(e => e.Event).ToArray());
    }

    private static void Add(
        List<(CombatEvent Event, ulong Sequence)> events,
        EngagementRecord engagement,
        string victimId,
        ulong correlationId,
        CombatEventPhase phase,
        string outcome,
        string explanationRef) =>
        events.Add((new CombatEvent(
            phase,
            engagement.ShooterTargetId.Value,
            victimId,
            ResolveWeaponFamily(engagement),
            outcome,
            correlationId,
            engagement.SimTime,
            engagement.SimTick,
            explanationRef), engagement.SequenceId));

    private static string ResolveWeaponFamily(EngagementRecord engagement) =>
        string.IsNullOrWhiteSpace(engagement.WeaponFamilyId)
            ? UnknownWeaponFamilyId
            : engagement.WeaponFamilyId;

    private static string ResolveAbortExplanation(string abort) =>
        abort == EngagementAbortReasonCodes.ToLogCode(EngagementAbortReason.WeaponsTight)
            ? $"policy:{FireAbortReason.WeaponsTight}"
            : $"abort:{abort}";
}
