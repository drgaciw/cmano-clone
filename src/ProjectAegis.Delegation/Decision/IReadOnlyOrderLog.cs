namespace ProjectAegis.Delegation.Decision;

/// <summary>
/// ADR-019: read-only view of the order log for AAR / analysis consumers.
/// <para>
/// Deliberately has <b>no</b> <c>Append</c>. Anything handed an
/// <see cref="IReadOnlyOrderLog"/> cannot mutate the log, and therefore cannot
/// desync <c>ComputeFingerprint()</c> — the replay/golden determinism invariant.
/// </para>
/// <para>
/// AAR-facing APIs must take this type, never <see cref="IOrderLog"/> or the
/// concrete <c>DecisionLog</c>. Extending this surface with further read-only
/// projections is expected and additive; adding any mutating member is not.
/// </para>
/// </summary>
public interface IReadOnlyOrderLog
{
    IReadOnlyList<OrderLogEntry> ChronologicalEntries();

    string ComputeFingerprint();

    IReadOnlyList<DecisionRecord> Records { get; }

    IReadOnlyList<PolicyDenialRecord> PolicyDenials { get; }

    IReadOnlyList<EngagementRecord> Engagements { get; }

    IReadOnlyList<ControllerChangeRecord> ControllerChanges { get; }
}
