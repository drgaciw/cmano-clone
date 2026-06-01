namespace ProjectAegis.Delegation.Decision;

using System.Text;

public sealed class DecisionLog
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

    public IReadOnlyList<DecisionRecord> Records => _records;

    public IReadOnlyList<PolicyDenialRecord> PolicyDenials => _policyDenials;

    public IReadOnlyList<EngagementRecord> Engagements => _engagements;

    public IReadOnlyList<ControllerChangeRecord> ControllerChanges => _controllerChanges;

    public IReadOnlyList<GroupMemberDetachRecord> GroupMemberDetaches => _groupMemberDetaches;

    public IReadOnlyList<GroupMemberRejoinRecord> GroupMemberRejoins => _groupMemberRejoins;

    public IReadOnlyList<MagazineChangeRecord> MagazineChanges => _magazineChanges;

    public void Append(DecisionRecord record)
    {
        _records.Add(record);
        _decisionSequences.Add(NextSequence());
    }

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
            _ => "?",
        };

    internal ulong NextSequence() => ++_sequenceId;
}
