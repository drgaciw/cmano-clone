namespace ProjectAegis.Delegation.Decision;

using System.Text;
using ProjectAegis.Delegation.Hindsight;

public sealed class DecisionLog : IOrderLog
{
    private ulong _sequenceId;

    /// <summary>Optional sidecar hook; does not affect append semantics or fingerprints.</summary>
    public IHindsightOrderLogHook? HindsightHook { get; set; }
    private readonly List<AgentDecisionPayload> _agentDecisions = new();
    private readonly List<ulong> _decisionSequences = new();
    private readonly List<PolicyDenialRecord> _policyDenials = new();
    private readonly List<EngagementRecord> _engagements = new();
    private readonly List<ControllerChangeRecord> _controllerChanges = new();
    private readonly List<GroupMemberDetachRecord> _groupMemberDetaches = new();
    private readonly List<GroupMemberRejoinRecord> _groupMemberRejoins = new();
    private readonly List<MagazineChangeRecord> _magazineChanges = new();
    private readonly List<ContactChangeRecord> _contactChanges = new();
    private readonly List<MissionTransitionRecord> _missionTransitions = new();
    private readonly List<EventFiredRecord> _eventFired = new();
    private readonly List<EngagementOutcomeRecord> _engagementOutcomes = new();
    private readonly List<PlayerOrderRecord> _playerOrders = new();
    private readonly List<PolicyUpdateRecord> _policyUpdates = new();
    private readonly List<ModeChangeRecord> _modeChanges = new();
    private readonly List<CommsStateChangeRecord> _commsStateChanges = new();
    private readonly List<FuelStateChangeRecord> _fuelStateChanges = new();

    public IReadOnlyList<DecisionRecord> Records =>
        _agentDecisions.Select(p => p.ToDecisionRecord()).ToArray();

    public IReadOnlyList<PolicyDenialRecord> PolicyDenials => _policyDenials;

    public IReadOnlyList<EngagementRecord> Engagements => _engagements;

    public IReadOnlyList<ControllerChangeRecord> ControllerChanges => _controllerChanges;

    public IReadOnlyList<GroupMemberDetachRecord> GroupMemberDetaches => _groupMemberDetaches;

    public IReadOnlyList<GroupMemberRejoinRecord> GroupMemberRejoins => _groupMemberRejoins;

    public IReadOnlyList<MagazineChangeRecord> MagazineChanges => _magazineChanges;

    public IReadOnlyList<ContactChangeRecord> ContactChanges => _contactChanges;

    public IReadOnlyList<MissionTransitionRecord> MissionTransitions => _missionTransitions;

    public IReadOnlyList<EventFiredRecord> EventFired => _eventFired;

    public IReadOnlyList<EngagementOutcomeRecord> EngagementOutcomes => _engagementOutcomes;

    public IReadOnlyList<PlayerOrderRecord> PlayerOrders => _playerOrders;

    public IReadOnlyList<PolicyUpdateRecord> PolicyUpdates => _policyUpdates;

    public IReadOnlyList<ModeChangeRecord> ModeChanges => _modeChanges;

    public IReadOnlyList<CommsStateChangeRecord> CommsStateChanges => _commsStateChanges;

    public IReadOnlyList<FuelStateChangeRecord> FuelStateChanges => _fuelStateChanges;

    public void Append(OrderLogEntry entry)
    {
        var sequenceId = entry.SequenceId == 0 ? NextSequence() : entry.SequenceId;
        switch (entry.Kind)
        {
            case OrderLogEntryKind.AgentDecision when entry.Payload is AgentDecisionPayload payload:
                _agentDecisions.Add(payload);
                _decisionSequences.Add(sequenceId);
                break;
            case OrderLogEntryKind.AgentDecision when entry.Payload is DecisionRecord legacy:
                _agentDecisions.Add(AgentDecisionPayload.FromDecisionRecord(legacy, legacy.SimTick));
                _decisionSequences.Add(sequenceId);
                break;
            case OrderLogEntryKind.PolicyDenial when entry.Payload is PolicyDenialRecord denial:
                _policyDenials.Add(denial with { SequenceId = sequenceId });
                break;
            case OrderLogEntryKind.Engagement when entry.Payload is EngagementRecord engagement:
                _engagements.Add(engagement with { SequenceId = sequenceId });
                break;
            case OrderLogEntryKind.EngagementOutcome when entry.Payload is EngagementOutcomeRecord outcome:
                _engagementOutcomes.Add(outcome with { SequenceId = sequenceId });
                break;
            case OrderLogEntryKind.MagazineChange when entry.Payload is MagazineChangeRecord magazine:
                _magazineChanges.Add(magazine with { SequenceId = sequenceId });
                break;
            case OrderLogEntryKind.ContactChange when entry.Payload is ContactChangeRecord contact:
                _contactChanges.Add(contact with { SequenceId = sequenceId });
                break;
            case OrderLogEntryKind.MissionTransition when entry.Payload is MissionTransitionRecord mission:
                _missionTransitions.Add(mission with { SequenceId = sequenceId });
                break;
            case OrderLogEntryKind.EventFired when entry.Payload is EventFiredRecord fired:
                _eventFired.Add(fired with { SequenceId = sequenceId });
                break;
            case OrderLogEntryKind.ControllerChange when entry.Payload is ControllerChangeRecord controller:
                _controllerChanges.Add(controller with { SequenceId = sequenceId });
                break;
            case OrderLogEntryKind.GroupMemberDetach when entry.Payload is GroupMemberDetachRecord detach:
                _groupMemberDetaches.Add(detach with { SequenceId = sequenceId });
                break;
            case OrderLogEntryKind.GroupMemberRejoin when entry.Payload is GroupMemberRejoinRecord rejoin:
                _groupMemberRejoins.Add(rejoin with { SequenceId = sequenceId });
                break;
            case OrderLogEntryKind.PlayerOrder when entry.Payload is PlayerOrderRecord playerOrder:
                _playerOrders.Add(playerOrder with { SequenceId = sequenceId });
                break;
            case OrderLogEntryKind.PolicyUpdate when entry.Payload is PolicyUpdateRecord policyUpdate:
                _policyUpdates.Add(policyUpdate with { SequenceId = sequenceId });
                break;
            case OrderLogEntryKind.ModeChange when entry.Payload is ModeChangeRecord modeChange:
                _modeChanges.Add(modeChange with { SequenceId = sequenceId });
                break;
            case OrderLogEntryKind.CommsStateChange when entry.Payload is CommsStateChangeRecord commsChange:
                _commsStateChanges.Add(commsChange with { SequenceId = sequenceId });
                break;
            case OrderLogEntryKind.FuelStateChange when entry.Payload is FuelStateChangeRecord fuelChange:
                _fuelStateChanges.Add(fuelChange with { SequenceId = sequenceId });
                break;
            default:
                throw new ArgumentException($"Unsupported order log entry kind: {entry.Kind}", nameof(entry));
        }

        NotifyHindsight(entry, sequenceId);
    }

    private void NotifyHindsight(OrderLogEntry entry, ulong sequenceId)
    {
        if (HindsightHook is null)
        {
            return;
        }

        var notified = entry.SequenceId == 0
            ? entry with { SequenceId = sequenceId }
            : entry;
        HindsightHook.OnAppended(notified);
    }

    public void Append(DecisionRecord record) =>
        Append(OrderLogEntry.FromDecisionRecord(record, (ulong)Math.Max(0, (long)record.SimTime)));

    public void AppendPolicyDenial(PolicyDenialRecord denial) =>
        Append(OrderLogEntryFactories.FromPolicyDenial(denial));

    public void AppendEngagement(EngagementRecord engagement) =>
        Append(OrderLogEntryFactories.FromEngagement(engagement));

    public void AppendControllerChange(ControllerChangeRecord change) =>
        Append(OrderLogEntryFactories.FromControllerChange(change));

    public void AppendGroupMemberDetach(GroupMemberDetachRecord detach) =>
        Append(OrderLogEntryFactories.FromGroupMemberDetach(detach));

    public void AppendGroupMemberRejoin(GroupMemberRejoinRecord rejoin) =>
        Append(OrderLogEntryFactories.FromGroupMemberRejoin(rejoin));

    public void AppendMagazineChange(MagazineChangeRecord change) =>
        Append(OrderLogEntryFactories.FromMagazineChange(change));

    public void AppendContactChange(ContactChangeRecord change) =>
        Append(OrderLogEntryFactories.FromContactChange(change));

    public void AppendMissionTransition(MissionTransitionRecord transition) =>
        Append(OrderLogEntryFactories.FromMissionTransition(transition));

    public void AppendEventFired(EventFiredRecord fired) =>
        Append(OrderLogEntryFactories.FromEventFired(fired));

    public void AppendEngagementOutcome(EngagementOutcomeRecord outcome) =>
        Append(OrderLogEntryFactories.FromEngagementOutcome(outcome));

    public void AppendPlayerOrder(PlayerOrderRecord order) =>
        Append(OrderLogEntryFactories.FromPlayerOrder(order));

    public void AppendPolicyUpdate(PolicyUpdateRecord update) =>
        Append(OrderLogEntryFactories.FromPolicyUpdate(update));

    public void AppendModeChange(ModeChangeRecord change) =>
        Append(OrderLogEntryFactories.FromModeChange(change));

    public void AppendCommsStateChange(CommsStateChangeRecord change) =>
        Append(OrderLogEntryFactories.FromCommsStateChange(change));

    public void AppendFuelStateChange(FuelStateChangeRecord change) =>
        Append(OrderLogEntryFactories.FromFuelStateChange(change));

    /// <summary>Unified timeline sorted by sequence (ADR-003 MVP).</summary>
    public IReadOnlyList<OrderLogEntry> ChronologicalEntries()
    {
        var entries = new List<OrderLogEntry>();
        for (var i = 0; i < _agentDecisions.Count; i++)
        {
            var payload = _agentDecisions[i];
            entries.Add(new OrderLogEntry(
                _decisionSequences[i],
                OrderLogEntryKind.AgentDecision,
                payload.SimTime,
                payload));
        }

        foreach (var d in _policyDenials)
        {
            entries.Add(new OrderLogEntry(d.SequenceId, OrderLogEntryKind.PolicyDenial, d.SimTime, d));
        }

        foreach (var e in _engagements)
        {
            entries.Add(new OrderLogEntry(e.SequenceId, OrderLogEntryKind.Engagement, e.SimTime, e));
        }

        foreach (var c in _controllerChanges)
        {
            entries.Add(new OrderLogEntry(c.SequenceId, OrderLogEntryKind.ControllerChange, c.SimTime, c));
        }

        foreach (var d in _groupMemberDetaches)
        {
            entries.Add(new OrderLogEntry(d.SequenceId, OrderLogEntryKind.GroupMemberDetach, d.SimTime, d));
        }

        foreach (var r in _groupMemberRejoins)
        {
            entries.Add(new OrderLogEntry(r.SequenceId, OrderLogEntryKind.GroupMemberRejoin, r.SimTime, r));
        }

        foreach (var m in _magazineChanges)
        {
            entries.Add(new OrderLogEntry(m.SequenceId, OrderLogEntryKind.MagazineChange, m.SimTime, m));
        }

        foreach (var c in _contactChanges)
        {
            entries.Add(new OrderLogEntry(c.SequenceId, OrderLogEntryKind.ContactChange, c.SimTime, c));
        }

        foreach (var m in _missionTransitions)
        {
            entries.Add(new OrderLogEntry(m.SequenceId, OrderLogEntryKind.MissionTransition, m.SimTime, m));
        }

        foreach (var e in _eventFired)
        {
            entries.Add(new OrderLogEntry(e.SequenceId, OrderLogEntryKind.EventFired, e.SimTime, e));
        }

        foreach (var o in _engagementOutcomes)
        {
            entries.Add(new OrderLogEntry(o.SequenceId, OrderLogEntryKind.EngagementOutcome, o.SimTime, o));
        }

        foreach (var p in _playerOrders)
        {
            entries.Add(new OrderLogEntry(p.SequenceId, OrderLogEntryKind.PlayerOrder, p.SimTime, p));
        }

        foreach (var u in _policyUpdates)
        {
            entries.Add(new OrderLogEntry(u.SequenceId, OrderLogEntryKind.PolicyUpdate, u.SimTime, u));
        }

        foreach (var m in _modeChanges)
        {
            entries.Add(new OrderLogEntry(m.SequenceId, OrderLogEntryKind.ModeChange, m.SimTime, m));
        }

        foreach (var c in _commsStateChanges)
        {
            entries.Add(new OrderLogEntry(c.SequenceId, OrderLogEntryKind.CommsStateChange, c.SimTime, c));
        }

        foreach (var f in _fuelStateChanges)
        {
            entries.Add(new OrderLogEntry(f.SequenceId, OrderLogEntryKind.FuelStateChange, f.SimTime, f));
        }

        return entries.OrderBy(e => e.SequenceId).ToArray();
    }

    /// <summary>Deterministic replay fingerprint including denials and engagements.</summary>
    public string ComputeFingerprint()
    {
        var sb = new StringBuilder();
        foreach (var entry in ChronologicalEntries())
        {
            sb.Append(entry.Kind);
            sb.Append('|');
            sb.Append(entry.SequenceId);
            sb.Append('|');
            sb.Append(entry.SimTime.ToString("R"));
            sb.Append('|');
            sb.Append(FormatPayload(entry));
            sb.Append('\n');
        }

        return sb.ToString();
    }

    private static string FormatPayload(OrderLogEntry entry) =>
        entry.Kind switch
        {
            OrderLogEntryKind.AgentDecision when entry.Payload is AgentDecisionPayload p =>
                $"{p.SimTick}|{p.AgentId.Value}|{p.ChosenOrderKind}|{ScoredIntentFingerprint.Format(p.ScoredIntents)}|{p.RngDraw:R}",
            OrderLogEntryKind.AgentDecision when entry.Payload is DecisionRecord r =>
                $"{r.SimTick}|{r.AgentId.Value}|{r.ChosenKind}|{ScoredIntentFingerprint.Format(r.Alternatives)}|{r.RngDraw:R}",
            OrderLogEntryKind.PolicyDenial when entry.Payload is PolicyDenialRecord d =>
                $"{d.TargetId.Value}|{d.Reason}|{d.AttemptedKind}",
            OrderLogEntryKind.Engagement when entry.Payload is EngagementRecord e =>
                $"{e.SimTick}|{e.ShooterTargetId.Value}|{e.EngagementId}|{e.Launched}|{e.AbortReasonCode}",
            OrderLogEntryKind.ControllerChange when entry.Payload is ControllerChangeRecord c =>
                $"{c.TargetId.Value}|{c.PreviousKind}|{c.NewKind}|{c.AgentId?.Value}",
            OrderLogEntryKind.GroupMemberDetach when entry.Payload is GroupMemberDetachRecord d =>
                $"{d.GroupId.Value}|{d.UnitId.Value}",
            OrderLogEntryKind.GroupMemberRejoin when entry.Payload is GroupMemberRejoinRecord r =>
                $"{r.GroupId.Value}|{r.UnitId.Value}",
            OrderLogEntryKind.MagazineChange when entry.Payload is MagazineChangeRecord m =>
                $"{m.SimTick}|{m.ShooterTargetId.Value}|{m.MountId}|{m.Delta}|{m.ReasonCode}",
            OrderLogEntryKind.ContactChange when entry.Payload is ContactChangeRecord c =>
                $"{c.SimTick}|{c.ObserverId}|{c.ContactId}|{c.TargetId}|{c.PreviousState}|{c.NewState}",
            OrderLogEntryKind.MissionTransition when entry.Payload is MissionTransitionRecord m =>
                $"{m.SimTick}|{m.EventId}|{m.PhaseCode}",
            OrderLogEntryKind.EventFired when entry.Payload is EventFiredRecord f =>
                $"{f.SimTick}|{f.EventId}|{f.EventCode}",
            OrderLogEntryKind.EngagementOutcome when entry.Payload is EngagementOutcomeRecord o =>
                $"{o.SimTick}|{o.EngagementId}|{o.VictimTargetId.Value}|{o.OutcomeCode}|{o.PkDraw:R}",
            OrderLogEntryKind.PlayerOrder when entry.Payload is PlayerOrderRecord p =>
                $"{p.SimTick}|{p.UnitId.Value}|{p.Kind}|{p.Source}",
            OrderLogEntryKind.PolicyUpdate when entry.Payload is PolicyUpdateRecord u =>
                $"{u.SimTick}|{u.PolicySnapshotId}|{u.Field}|{u.PreviousValue}|{u.NewValue}",
            OrderLogEntryKind.ModeChange when entry.Payload is ModeChangeRecord m =>
                $"{m.SimTick}|{m.UnitId?.Value}|{m.PreviousMode}|{m.NewMode}",
            OrderLogEntryKind.CommsStateChange when entry.Payload is CommsStateChangeRecord c =>
                $"{c.SimTick}|{c.NodeId}|{c.PreviousState}|{c.NewState}|{c.Reason}",
            OrderLogEntryKind.FuelStateChange when entry.Payload is FuelStateChangeRecord f =>
                $"{f.SimTick}|{f.UnitId.Value}|{f.PreviousState}|{f.NewState}|{f.RemainingFuelKg:R}",
            _ => "?",
        };

    internal ulong NextSequence() => ++_sequenceId;
}