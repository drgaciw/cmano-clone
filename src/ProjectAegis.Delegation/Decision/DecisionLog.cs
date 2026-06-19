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
    private readonly List<FuelBurnRecord> _fuelBurns = new();
    private readonly List<PlatformDamageChangeRecord> _platformDamageChanges = new();
    private readonly List<OrderLogEntry> _chronological = new();

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

    public IReadOnlyList<FuelBurnRecord> FuelBurns => _fuelBurns;

    public IReadOnlyList<PlatformDamageChangeRecord> PlatformDamageChanges => _platformDamageChanges;

    public void Append(OrderLogEntry entry)
    {
        var sequenceId = entry.SequenceId == 0 ? NextSequence() : entry.SequenceId;
        switch (entry.Kind)
        {
            case OrderLogEntryKind.AgentDecision when entry.Payload is AgentDecisionPayload payload:
                _agentDecisions.Add(payload);
                _decisionSequences.Add(sequenceId);
                AppendChronologicalEntry(sequenceId, OrderLogEntryKind.AgentDecision, payload.SimTime, payload);
                break;
            case OrderLogEntryKind.AgentDecision when entry.Payload is DecisionRecord legacy:
                var migrated = AgentDecisionPayload.FromDecisionRecord(legacy, legacy.SimTick);
                _agentDecisions.Add(migrated);
                _decisionSequences.Add(sequenceId);
                AppendChronologicalEntry(sequenceId, OrderLogEntryKind.AgentDecision, legacy.SimTime, migrated);
                break;
            case OrderLogEntryKind.PolicyDenial when entry.Payload is PolicyDenialRecord denial:
                var denialRecord = denial with { SequenceId = sequenceId };
                _policyDenials.Add(denialRecord);
                AppendChronologicalEntry(sequenceId, OrderLogEntryKind.PolicyDenial, denialRecord.SimTime, denialRecord);
                break;
            case OrderLogEntryKind.Engagement when entry.Payload is EngagementRecord engagement:
                var engagementRecord = engagement with { SequenceId = sequenceId };
                _engagements.Add(engagementRecord);
                AppendChronologicalEntry(sequenceId, OrderLogEntryKind.Engagement, engagementRecord.SimTime, engagementRecord);
                break;
            case OrderLogEntryKind.EngagementOutcome when entry.Payload is EngagementOutcomeRecord outcome:
                var outcomeRecord = outcome with { SequenceId = sequenceId };
                _engagementOutcomes.Add(outcomeRecord);
                AppendChronologicalEntry(sequenceId, OrderLogEntryKind.EngagementOutcome, outcomeRecord.SimTime, outcomeRecord);
                break;
            case OrderLogEntryKind.MagazineChange when entry.Payload is MagazineChangeRecord magazine:
                var magazineRecord = magazine with { SequenceId = sequenceId };
                _magazineChanges.Add(magazineRecord);
                AppendChronologicalEntry(sequenceId, OrderLogEntryKind.MagazineChange, magazineRecord.SimTime, magazineRecord);
                break;
            case OrderLogEntryKind.ContactChange when entry.Payload is ContactChangeRecord contact:
                var contactRecord = contact with { SequenceId = sequenceId };
                _contactChanges.Add(contactRecord);
                AppendChronologicalEntry(sequenceId, OrderLogEntryKind.ContactChange, contactRecord.SimTime, contactRecord);
                break;
            case OrderLogEntryKind.MissionTransition when entry.Payload is MissionTransitionRecord mission:
                var missionRecord = mission with { SequenceId = sequenceId };
                _missionTransitions.Add(missionRecord);
                AppendChronologicalEntry(sequenceId, OrderLogEntryKind.MissionTransition, missionRecord.SimTime, missionRecord);
                break;
            case OrderLogEntryKind.EventFired when entry.Payload is EventFiredRecord fired:
                var firedRecord = fired with { SequenceId = sequenceId };
                _eventFired.Add(firedRecord);
                AppendChronologicalEntry(sequenceId, OrderLogEntryKind.EventFired, firedRecord.SimTime, firedRecord);
                break;
            case OrderLogEntryKind.ControllerChange when entry.Payload is ControllerChangeRecord controller:
                var controllerRecord = controller with { SequenceId = sequenceId };
                _controllerChanges.Add(controllerRecord);
                AppendChronologicalEntry(sequenceId, OrderLogEntryKind.ControllerChange, controllerRecord.SimTime, controllerRecord);
                break;
            case OrderLogEntryKind.GroupMemberDetach when entry.Payload is GroupMemberDetachRecord detach:
                var detachRecord = detach with { SequenceId = sequenceId };
                _groupMemberDetaches.Add(detachRecord);
                AppendChronologicalEntry(sequenceId, OrderLogEntryKind.GroupMemberDetach, detachRecord.SimTime, detachRecord);
                break;
            case OrderLogEntryKind.GroupMemberRejoin when entry.Payload is GroupMemberRejoinRecord rejoin:
                var rejoinRecord = rejoin with { SequenceId = sequenceId };
                _groupMemberRejoins.Add(rejoinRecord);
                AppendChronologicalEntry(sequenceId, OrderLogEntryKind.GroupMemberRejoin, rejoinRecord.SimTime, rejoinRecord);
                break;
            case OrderLogEntryKind.PlayerOrder when entry.Payload is PlayerOrderRecord playerOrder:
                var playerOrderRecord = playerOrder with { SequenceId = sequenceId };
                _playerOrders.Add(playerOrderRecord);
                AppendChronologicalEntry(sequenceId, OrderLogEntryKind.PlayerOrder, playerOrderRecord.SimTime, playerOrderRecord);
                break;
            case OrderLogEntryKind.PolicyUpdate when entry.Payload is PolicyUpdateRecord policyUpdate:
                var policyUpdateRecord = policyUpdate with { SequenceId = sequenceId };
                _policyUpdates.Add(policyUpdateRecord);
                AppendChronologicalEntry(sequenceId, OrderLogEntryKind.PolicyUpdate, policyUpdateRecord.SimTime, policyUpdateRecord);
                break;
            case OrderLogEntryKind.ModeChange when entry.Payload is ModeChangeRecord modeChange:
                var modeChangeRecord = modeChange with { SequenceId = sequenceId };
                _modeChanges.Add(modeChangeRecord);
                AppendChronologicalEntry(sequenceId, OrderLogEntryKind.ModeChange, modeChangeRecord.SimTime, modeChangeRecord);
                break;
            case OrderLogEntryKind.CommsStateChange when entry.Payload is CommsStateChangeRecord commsChange:
                var commsChangeRecord = commsChange with { SequenceId = sequenceId };
                _commsStateChanges.Add(commsChangeRecord);
                AppendChronologicalEntry(sequenceId, OrderLogEntryKind.CommsStateChange, commsChangeRecord.SimTime, commsChangeRecord);
                break;
            case OrderLogEntryKind.FuelStateChange when entry.Payload is FuelStateChangeRecord fuelChange:
                var fuelChangeRecord = fuelChange with { SequenceId = sequenceId };
                _fuelStateChanges.Add(fuelChangeRecord);
                AppendChronologicalEntry(sequenceId, OrderLogEntryKind.FuelStateChange, fuelChangeRecord.SimTime, fuelChangeRecord);
                break;
            case OrderLogEntryKind.FuelBurn when entry.Payload is FuelBurnRecord fuelBurn:
                var fuelBurnRecord = fuelBurn with { SequenceId = sequenceId };
                _fuelBurns.Add(fuelBurnRecord);
                AppendChronologicalEntry(sequenceId, OrderLogEntryKind.FuelBurn, fuelBurnRecord.SimTime, fuelBurnRecord);
                break;
            case OrderLogEntryKind.PlatformDamageChange when entry.Payload is PlatformDamageChangeRecord damageChange:
                var damageChangeRecord = damageChange with { SequenceId = sequenceId };
                _platformDamageChanges.Add(damageChangeRecord);
                AppendChronologicalEntry(sequenceId, OrderLogEntryKind.PlatformDamageChange, damageChangeRecord.SimTime, damageChangeRecord);
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

    public void AppendFuelBurn(FuelBurnRecord burn) =>
        Append(OrderLogEntryFactories.FromFuelBurn(burn));

    public void AppendPlatformDamageChange(PlatformDamageChangeRecord change) =>
        Append(OrderLogEntryFactories.FromPlatformDamageChange(change));

    /// <summary>Unified timeline sorted by sequence (ADR-003 MVP).</summary>
    public IReadOnlyList<OrderLogEntry> ChronologicalEntries() => _chronological;

    /// <summary>Deterministic replay fingerprint including denials and engagements.</summary>
    public string ComputeFingerprint()
    {
        var sb = new StringBuilder();
        foreach (var entry in _chronological)
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
                $"{p.SimTick}|{p.ResolvedExecuteSimTick}|{p.UnitId.Value}|{p.Kind}|{p.Source}",
            OrderLogEntryKind.PolicyUpdate when entry.Payload is PolicyUpdateRecord u =>
                $"{u.SimTick}|{u.PolicySnapshotId}|{u.Field}|{u.PreviousValue}|{u.NewValue}",
            OrderLogEntryKind.ModeChange when entry.Payload is ModeChangeRecord m =>
                $"{m.SimTick}|{m.UnitId?.Value}|{m.PreviousMode}|{m.NewMode}",
            OrderLogEntryKind.CommsStateChange when entry.Payload is CommsStateChangeRecord c =>
                $"{c.SimTick}|{c.NodeId}|{c.PreviousState}|{c.NewState}|{c.Reason}",
            OrderLogEntryKind.FuelStateChange when entry.Payload is FuelStateChangeRecord f =>
                $"{f.SimTick}|{f.UnitId.Value}|{f.PreviousState}|{f.NewState}|{f.RemainingFuelKg:R}",
            OrderLogEntryKind.FuelBurn when entry.Payload is FuelBurnRecord b =>
                $"{b.SimTick}|{b.UnitId.Value}|{b.DeltaKg:R}|{b.RemainingFuelKg:R}",
            OrderLogEntryKind.PlatformDamageChange when entry.Payload is PlatformDamageChangeRecord d =>
                $"{d.SimTick}|{d.UnitId.Value}|{d.PreviousHpPct:R}|{d.NewHpPct:R}|{d.ReasonCode}|{d.DamageLevel}",
            _ => "?",
        };

    internal ulong NextSequence() => ++_sequenceId;

    private void AppendChronologicalEntry(
        ulong sequenceId,
        OrderLogEntryKind kind,
        double simTime,
        object payload)
    {
        var entry = new OrderLogEntry(sequenceId, kind, simTime, payload);
        if (_chronological.Count == 0 || sequenceId >= _chronological[^1].SequenceId)
        {
            _chronological.Add(entry);
            return;
        }

        var index = _chronological.BinarySearch(entry, ChronologicalSequenceComparer.Instance);
        _chronological.Insert(index < 0 ? ~index : index, entry);
    }

    private sealed class ChronologicalSequenceComparer : IComparer<OrderLogEntry>
    {
        public static readonly ChronologicalSequenceComparer Instance = new();

        public int Compare(OrderLogEntry? x, OrderLogEntry? y) =>
            (x?.SequenceId ?? 0).CompareTo(y?.SequenceId ?? 0);
    }
}