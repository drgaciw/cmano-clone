namespace ProjectAegis.Delegation.Decision;

using System.Text;

public sealed class DecisionLog : IOrderLog
{
    private ulong _sequenceId;
    private readonly List<DecisionRecord> _records = new();
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

    public IReadOnlyList<DecisionRecord> Records => _records;

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

    public void Append(OrderLogEntry entry)
    {
        var sequenceId = entry.SequenceId == 0 ? NextSequence() : entry.SequenceId;
        switch (entry.Kind)
        {
            case OrderLogEntryKind.AgentDecision when entry.Payload is DecisionRecord record:
                _records.Add(record);
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
            case OrderLogEntryKind.ContactChange when entry.Payload is ContactChangeRecord contact:
                _contactChanges.Add(contact with { SequenceId = sequenceId });
                break;
            default:
                throw new ArgumentException($"Unsupported order log entry kind: {entry.Kind}", nameof(entry));
        }
    }

    public void Append(DecisionRecord record) =>
        Append(OrderLogEntry.FromDecisionRecord(record, (ulong)Math.Max(0, (long)record.SimTime)));

    public void AppendPolicyDenial(PolicyDenialRecord denial) =>
        _policyDenials.Add(denial with { SequenceId = NextSequence() });

    public void AppendEngagement(EngagementRecord engagement) =>
        _engagements.Add(engagement with { SequenceId = NextSequence() });

    public void AppendControllerChange(ControllerChangeRecord change) =>
        _controllerChanges.Add(change with { SequenceId = NextSequence() });

    public void AppendGroupMemberDetach(GroupMemberDetachRecord detach) =>
        _groupMemberDetaches.Add(detach with { SequenceId = NextSequence() });

    public void AppendGroupMemberRejoin(GroupMemberRejoinRecord rejoin) =>
        _groupMemberRejoins.Add(rejoin with { SequenceId = NextSequence() });

    public void AppendMagazineChange(MagazineChangeRecord change) =>
        _magazineChanges.Add(change with { SequenceId = NextSequence() });

    public void AppendContactChange(ContactChangeRecord change) =>
        _contactChanges.Add(change with { SequenceId = NextSequence() });

    public void AppendMissionTransition(MissionTransitionRecord transition) =>
        _missionTransitions.Add(transition with { SequenceId = NextSequence() });

    public void AppendEventFired(EventFiredRecord fired) =>
        _eventFired.Add(fired with { SequenceId = NextSequence() });

    public void AppendEngagementOutcome(EngagementOutcomeRecord outcome) =>
        _engagementOutcomes.Add(outcome with { SequenceId = NextSequence() });

    /// <summary>Unified timeline sorted by sequence (ADR-003 MVP).</summary>
    public IReadOnlyList<OrderLogEntry> ChronologicalEntries()
    {
        var entries = new List<OrderLogEntry>();
        for (var i = 0; i < _records.Count; i++)
        {
            entries.Add(new OrderLogEntry(
                _decisionSequences[i],
                OrderLogEntryKind.AgentDecision,
                _records[i].SimTime,
                _records[i]));
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
            OrderLogEntryKind.AgentDecision when entry.Payload is DecisionRecord r =>
                $"{r.AgentId.Value}|{r.ChosenKind}|{r.RngDraw:R}",
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
            _ => "?",
        };

    internal ulong NextSequence() => ++_sequenceId;
}
