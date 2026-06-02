namespace ProjectAegis.Delegation.Projection;

using ProjectAegis.Delegation.Decision;
using ProjectAegis.Sim.Engage;

/// <summary>Filters order log into CMANO-style message log lines (GDD order-log-replay §3).</summary>
public static class MessageLogProjection
{
    public static IReadOnlyList<MessageLogLine> Project(DecisionLog log) =>
        Project(log.ChronologicalEntries());

    public static IReadOnlyList<MessageLogLine> Project(IReadOnlyList<OrderLogEntry> entries)
    {
        var lines = new List<MessageLogLine>();
        foreach (var entry in entries)
        {
            var line = TryProject(entry);
            if (line != null)
            {
                lines.Add(line);
            }
        }

        return lines;
    }

    private static MessageLogLine? TryProject(OrderLogEntry entry) =>
        entry.Kind switch
        {
            OrderLogEntryKind.EngagementOutcome when entry.Payload is EngagementOutcomeRecord o =>
                ProjectCombatOutcome(o, entry.SequenceId, entry.SimTime),
            OrderLogEntryKind.Engagement when entry.Payload is EngagementRecord e =>
                e.Launched
                    ? new MessageLogLine(
                        entry.SequenceId,
                        entry.SimTime,
                        "WEAPON_LAUNCH",
                        $"Unit {e.ShooterTargetId.Value} launched engagement {e.EngagementId}",
                        e.ShooterTargetId.Value)
                    : new MessageLogLine(
                        entry.SequenceId,
                        entry.SimTime,
                        "ENGAGE_ABORT",
                        $"Engagement aborted: {e.AbortReasonCode}",
                        e.ShooterTargetId.Value),
            OrderLogEntryKind.PolicyDenial when entry.Payload is PolicyDenialRecord d =>
                new MessageLogLine(
                    entry.SequenceId,
                    entry.SimTime,
                    "POLICY_DENIAL",
                    $"Fire denied for {d.TargetId.Value}: {d.Reason} ({d.AttemptedKind})",
                    d.TargetId.Value),
            OrderLogEntryKind.ContactChange when entry.Payload is ContactChangeRecord c =>
                new MessageLogLine(
                    entry.SequenceId,
                    entry.SimTime,
                    "CONTACT",
                    $"Contact {c.ContactId} {c.PreviousState} → {c.NewState} ({c.TargetId})",
                    c.ObserverId),
            _ => null,
        };

    private static MessageLogLine ProjectCombatOutcome(
        EngagementOutcomeRecord o,
        ulong sequenceId,
        double simTime)
    {
        var victim = o.VictimTargetId.Value;
        return o.OutcomeCode switch
        {
            EngagementOutcomeCodes.Kill => new MessageLogLine(
                sequenceId,
                simTime,
                "KILL_CONFIRMED",
                $"Hostile destroyed: {victim} (engagement {o.EngagementId})",
                o.ShooterTargetId.Value),
            EngagementOutcomeCodes.Intercept => new MessageLogLine(
                sequenceId,
                simTime,
                "INTERCEPT_SUCCESS",
                $"Threat neutralized (intercept): {victim} — target remains operational",
                o.ShooterTargetId.Value),
            EngagementOutcomeCodes.Hit => new MessageLogLine(
                sequenceId,
                simTime,
                "HIT",
                $"Weapon hit {victim} (engagement {o.EngagementId})",
                o.ShooterTargetId.Value),
            EngagementOutcomeCodes.Miss => new MessageLogLine(
                sequenceId,
                simTime,
                "MISS",
                $"Weapon missed {victim} (engagement {o.EngagementId})",
                o.ShooterTargetId.Value),
            _ => new MessageLogLine(
                sequenceId,
                simTime,
                "COMBAT",
                $"Outcome {o.OutcomeCode} vs {victim}",
                o.ShooterTargetId.Value),
        };
    }
}