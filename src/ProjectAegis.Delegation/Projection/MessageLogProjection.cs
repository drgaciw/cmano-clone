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
            OrderLogEntryKind.MagazineChange when entry.Payload is MagazineChangeRecord m =>
                new MessageLogLine(
                    entry.SequenceId,
                    entry.SimTime,
                    "MAGAZINE",
                    $"Magazine {m.ShooterTargetId.Value} mount {m.MountId}: {m.Delta} ({m.ReasonCode})",
                    m.ShooterTargetId.Value),
            OrderLogEntryKind.ModeChange when entry.Payload is ModeChangeRecord mc =>
                new MessageLogLine(
                    entry.SequenceId,
                    entry.SimTime,
                    "MODE",
                    $"Mode {mc.PreviousMode} → {mc.NewMode}",
                    mc.UnitId?.Value),
            OrderLogEntryKind.PlayerOrder when entry.Payload is PlayerOrderRecord po =>
                new MessageLogLine(
                    entry.SequenceId,
                    entry.SimTime,
                    "PLAYER_ORDER",
                    $"Player ordered {po.Kind} for {po.UnitId.Value}",
                    po.UnitId.Value),
            OrderLogEntryKind.CommsStateChange when entry.Payload is CommsStateChangeRecord c =>
                new MessageLogLine(
                    entry.SequenceId,
                    entry.SimTime,
                    "COMMS",
                    $"Comms {c.NodeId}: {c.PreviousState} → {c.NewState} ({c.Reason})",
                    c.NodeId),
            OrderLogEntryKind.FuelStateChange when entry.Payload is FuelStateChangeRecord f =>
                new MessageLogLine(
                    entry.SequenceId,
                    entry.SimTime,
                    "FUEL",
                    $"Fuel {f.UnitId.Value}: {f.PreviousState} → {f.NewState} ({f.RemainingFuelKg:F0} kg)",
                    f.UnitId.Value),
            OrderLogEntryKind.OrdnanceStateChange when entry.Payload is OrdnanceStateChangeRecord o =>
                new MessageLogLine(
                    entry.SequenceId,
                    entry.SimTime,
                    "ORDNANCE",
                    $"Ordnance {o.UnitId.Value}: {o.PreviousState} → {o.NewState} (rem {o.RoundsRemaining})",
                    o.UnitId.Value),
            OrderLogEntryKind.FuelBurn when entry.Payload is FuelBurnRecord b =>
                new MessageLogLine(
                    entry.SequenceId,
                    entry.SimTime,
                    "FUEL",
                    $"Fuel burn {b.UnitId.Value}: {b.DeltaKg:F0} kg (rem {b.RemainingFuelKg:F0} kg)",
                    b.UnitId.Value),
            OrderLogEntryKind.PolicyUpdate when entry.Payload is PolicyUpdateRecord u =>
                new MessageLogLine(
                    entry.SequenceId,
                    entry.SimTime,
                    "POLICY_UPDATE",
                    $"Policy {u.Field}: {u.PreviousValue} → {u.NewValue}"),
            OrderLogEntryKind.AgentDecision when entry.Payload is AgentDecisionPayload d =>
                new MessageLogLine(
                    entry.SequenceId,
                    entry.SimTime,
                    "AGENT_DECISION",
                    $"Agent {d.AgentId.Value} chose {d.ChosenOrderKind} for {d.TargetId.Value}: {d.Rationale}",
                    d.TargetId.Value),
            OrderLogEntryKind.AgentDecision when entry.Payload is DecisionRecord legacy =>
                new MessageLogLine(
                    entry.SequenceId,
                    entry.SimTime,
                    "AGENT_DECISION",
                    $"Agent {legacy.AgentId.Value} chose {legacy.ChosenKind} for {legacy.TargetId.Value}: {legacy.Rationale}",
                    legacy.TargetId.Value),
            OrderLogEntryKind.MissionTransition when entry.Payload is MissionTransitionRecord mt =>
                new MessageLogLine(
                    entry.SequenceId,
                    entry.SimTime,
                    "MISSION",
                    $"Mission {mt.EventId} → {mt.PhaseCode}"),
            OrderLogEntryKind.EventFired when entry.Payload is EventFiredRecord ev =>
                new MessageLogLine(
                    entry.SequenceId,
                    entry.SimTime,
                    "EVENT",
                    $"Event {ev.EventId}: {ev.EventCode}"),
            OrderLogEntryKind.PlatformDamageChange when entry.Payload is PlatformDamageChangeRecord dmg =>
                new MessageLogLine(
                    entry.SequenceId,
                    entry.SimTime,
                    "DAMAGE",
                    $"Damage {dmg.UnitId.Value}: {dmg.PreviousHpPct:F0}% → {dmg.NewHpPct:F0}% ({dmg.ReasonCode})",
                    dmg.UnitId.Value),
            OrderLogEntryKind.ControllerChange when entry.Payload is ControllerChangeRecord cc =>
                new MessageLogLine(
                    entry.SequenceId,
                    entry.SimTime,
                    "CONTROLLER",
                    $"Controller {cc.TargetId.Value}: {cc.PreviousKind} → {cc.NewKind}",
                    cc.TargetId.Value),
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