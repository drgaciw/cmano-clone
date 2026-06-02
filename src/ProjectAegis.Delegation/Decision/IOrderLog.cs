namespace ProjectAegis.Delegation.Decision;

/// <summary>ADR-003 / order-log-replay: unified append-only order log surface (C1 MVP).</summary>
public interface IOrderLog
{
    void Append(OrderLogEntry entry);

    IReadOnlyList<OrderLogEntry> ChronologicalEntries();

    string ComputeFingerprint();
}