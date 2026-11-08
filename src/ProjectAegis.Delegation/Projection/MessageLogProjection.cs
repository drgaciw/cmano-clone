namespace ProjectAegis.Delegation.Projection;

using System.Collections.Generic;
using CombatEvents;
using Decision;
using Orchestration;
using ProjectAegis.Sim.Engage;
using ProjectAegis.Sim.Scenario;

/// <summary>
/// Filters order log into CMANO-style message log lines (GDD order-log-replay §3).
/// Combat categories are taken from <see cref="CombatEventLogProjection"/> so a line exists
/// only when that snapshot recorded the launch, abort, or terminal outcome.
/// </summary>
public static class MessageLogProjection
{
    /// <summary>Project the full log (replay / AAR). Includes fogged agent decisions.</summary>
    public static IReadOnlyList<MessageLogLine> Project(DecisionLog log) =>
        Project(log.ChronologicalEntries());

    /// <summary>
    /// Project the live player view: agent decisions hidden by <paramref name="model"/> are omitted,
    /// matching <c>DelegationOrchestrator.GetLiveOrderLogView()</c>. The log itself is not modified.
    /// </summary>
    public static IReadOnlyList<MessageLogLine> ProjectLive(DecisionLog log, PlayerInfoModel model)
    {
        if (log is null)
        {
            throw new ArgumentNullException(nameof(log));
        }

        return Project(PlayerInfoFilter.FilterLiveEntries(log.ChronologicalEntries(), model));
    }

    public static IReadOnlyList<MessageLogLine> Project(IReadOnlyList<OrderLogEntry> entries)
    {
        var combatLog = new DecisionLog();
        foreach (var entry in entries)
        {
            if (entry.Kind is OrderLogEntryKind.Engagement or OrderLogEntryKind.EngagementOutcome)
            {
                combatLog.Append(entry);
            }
        }

        var snapshot = CombatEventLogProjection.Build(combatLog, double.MaxValue);
        var launches = new HashSet<ulong>();
        var aborts = new HashSet<ulong>();
        var terminals = new HashSet<ulong>();
        foreach (var combat in snapshot.Events)
        {
            switch (combat.Phase)
            {
                case CombatEventPhase.Firing when combat.Outcome == CombatEventProjection.OutcomeLaunch:
                    launches.Add(combat.CorrelationId);
                    break;
                case CombatEventPhase.AuthorizationRefused:
                    aborts.Add(combat.CorrelationId);
                    break;
                case CombatEventPhase.TerminalOutcome:
                    terminals.Add(combat.CorrelationId);
                    break;
            }
        }

        var outcomeCorrelation = IndexOutcomeCorrelations(entries);

        var lines = new List<MessageLogLine>();
        foreach (var entry in entries)
        {
            switch (entry.Kind)
            {
                case OrderLogEntryKind.Engagement when entry.Payload is EngagementRecord engagement:
                    if (engagement.Launched && launches.Contains(entry.SequenceId))
                    {
                        lines.Add(new MessageLogLine(
                            entry.SequenceId,
                            entry.SimTime,
                            "WEAPON_LAUNCH",
                            $"Unit {engagement.ShooterTargetId.Value} launched engagement {engagement.EngagementId}",
                            engagement.ShooterTargetId.Value));
                    }
                    else if (!engagement.Launched && aborts.Contains(entry.SequenceId))
                    {
                        lines.Add(new MessageLogLine(
                            entry.SequenceId,
                            entry.SimTime,
                            "ENGAGE_ABORT",
                            $"Engagement aborted: {engagement.AbortReasonCode}",
                            engagement.ShooterTargetId.Value));
                    }

                    break;
                case OrderLogEntryKind.EngagementOutcome
                    when entry.Payload is EngagementOutcomeRecord outcome
                        && TryTakeTerminal(terminals, outcomeCorrelation, entry.SequenceId):
                    lines.Add(ProjectCombatOutcome(outcome, entry.SequenceId, entry.SimTime));
                    break;
                default:
                    var line = TryProject(entry);
                    if (line != null)
                    {
                        lines.Add(line);
                    }

                    break;
            }
        }

        return lines;
    }

    /// <summary>
    /// Maps each outcome sequence to the launch sequence (<see cref="CombatEvent.CorrelationId"/>)
    /// of its own engagement. Display fields are not identity: an unmatched outcome with the
    /// same shooter, target, code, and sim time must not consume another engagement's terminal.
    /// </summary>
    private static Dictionary<ulong, ulong> IndexOutcomeCorrelations(IReadOnlyList<OrderLogEntry> entries)
    {
        var launches = new List<LaunchKey>();
        foreach (var entry in entries)
        {
            if (entry.Kind != OrderLogEntryKind.Engagement
                || entry.Payload is not EngagementRecord engagement
                || !engagement.Launched
                || entry.SequenceId == 0)
            {
                continue;
            }

            launches.Add(new LaunchKey(
                entry.SequenceId,
                engagement.ShooterTargetId.Value,
                engagement.EngagementId,
                engagement.VictimTargetId?.Value));
        }

        var byEngagement = new Dictionary<(string ShooterId, ulong EngagementId), List<int>>();
        for (var i = 0; i < launches.Count; i++)
        {
            var key = (launches[i].ShooterId, launches[i].EngagementId);
            if (!byEngagement.TryGetValue(key, out var indexes))
            {
                indexes = new List<int>();
                byEngagement[key] = indexes;
            }

            indexes.Add(i);
        }

        var nextSequence = new Dictionary<ulong, ulong>();
        foreach (var indexes in byEngagement.Values)
        {
            indexes.Sort((left, right) => launches[left].SequenceId.CompareTo(launches[right].SequenceId));
            for (var i = 0; i + 1 < indexes.Count; i++)
            {
                nextSequence[launches[indexes[i]].SequenceId] = launches[indexes[i + 1]].SequenceId;
            }
        }

        var correlationByOutcome = new Dictionary<ulong, ulong>();
        foreach (var entry in entries)
        {
            if (entry.Kind != OrderLogEntryKind.EngagementOutcome
                || entry.Payload is not EngagementOutcomeRecord outcome
                || entry.SequenceId == 0
                || !byEngagement.TryGetValue((outcome.ShooterTargetId.Value, outcome.EngagementId), out var indexes))
            {
                continue;
            }

            var owner = 0UL;
            var found = false;
            for (var i = 0; i < indexes.Count; i++)
            {
                var launch = launches[indexes[i]];
                if (entry.SequenceId <= launch.SequenceId)
                {
                    continue;
                }

                var next = nextSequence.TryGetValue(launch.SequenceId, out var following)
                    ? following
                    : ulong.MaxValue;
                if (entry.SequenceId >= next)
                {
                    continue;
                }

                if (launch.VictimId is not null
                    && !string.Equals(launch.VictimId, outcome.VictimTargetId.Value, StringComparison.Ordinal))
                {
                    continue;
                }

                if (!found || launch.SequenceId > owner)
                {
                    owner = launch.SequenceId;
                    found = true;
                }
            }

            if (found)
            {
                correlationByOutcome.TryAdd(entry.SequenceId, owner);
            }
        }

        return correlationByOutcome;
    }

    private static bool TryTakeTerminal(
        HashSet<ulong> terminals,
        Dictionary<ulong, ulong> correlationByOutcome,
        ulong outcomeSequence) =>
        correlationByOutcome.TryGetValue(outcomeSequence, out var correlation)
        && terminals.Remove(correlation);

    private readonly struct LaunchKey
    {
        public LaunchKey(ulong sequenceId, string shooterId, ulong engagementId, string? victimId)
        {
            SequenceId = sequenceId;
            ShooterId = shooterId;
            EngagementId = engagementId;
            VictimId = victimId;
        }

        public ulong SequenceId { get; }

        public string ShooterId { get; }

        public ulong EngagementId { get; }

        public string? VictimId { get; }
    }

    private static MessageLogLine? TryProject(OrderLogEntry entry) =>
        entry.Kind switch
        {
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