namespace ProjectAegis.Delegation.Decision;

/// <summary>
/// ADR-003 / order-log-replay: unified append-only order log surface (C1 MVP).
/// <para>
/// ADR-019: the read surface lives on <see cref="IReadOnlyOrderLog"/>. This
/// interface adds only the mutating member. Hand AAR / analysis consumers the
/// read-only base type, never this one.
/// </para>
/// </summary>
public interface IOrderLog : IReadOnlyOrderLog
{
    void Append(OrderLogEntry entry);
}