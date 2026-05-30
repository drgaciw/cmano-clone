namespace ProjectAegis.Delegation.Decision;

using System.Text;

public sealed class DecisionLog
{
    private ulong _sequenceId;
    private readonly List<DecisionRecord> _records = new();
    private readonly List<ulong> _decisionSequences = new();
    private readonly List<PolicyDenialRecord> _policyDenials = new();
    private readonly List<EngagementRecord> _engagements = new();

    public IReadOnlyList<DecisionRecord> Records => _records;

    public IReadOnlyList<PolicyDenialRecord> PolicyDenials => _policyDenials;

    public IReadOnlyList<EngagementRecord> Engagements => _engagements;

    public void Append(DecisionRecord record)
    {
        _records.Add(record);
        _decisionSequences.Add(NextSequence());
    }

    public void AppendPolicyDenial(PolicyDenialRecord denial) =>
        _policyDenials.Add(denial with { SequenceId = NextSequence() });

    public void AppendEngagement(EngagementRecord engagement) =>
        _engagements.Add(engagement with { SequenceId = NextSequence() });

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
                $"{e.ShooterTargetId.Value}|{e.EngagementId}|{e.Launched}|{e.AbortReason}",
            _ => "?",
        };

    internal ulong NextSequence() => ++_sequenceId;
}
