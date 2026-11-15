using ProjectAegis.Delegation.Decision;

namespace ProjectAegis.Ai.Host.Evidence;

/// <summary>
/// Pure projection from <see cref="IReadOnlyOrderLog"/> into the frozen evidence DTO (DRG-736).
/// Does not mutate the log; does not call <c>OnScenarioFinalized</c>.
/// </summary>
public interface IEvidenceProjector
{
    /// <summary>
    /// Projects chronological entries into a side-filtered <see cref="EvidenceProjectionResult"/>.
    /// </summary>
    /// <param name="log">Read-only order log (ADR-019).</param>
    /// <param name="request">Header identity, viewing side, and optional AAR/facts.</param>
    /// <param name="annotations">
    /// Optional per-<c>SequenceId</c> side / hidden-from metadata (fixtures and tagged exports).
    /// </param>
    EvidenceProjectionResult Project(
        IReadOnlyOrderLog log,
        EvidenceProjectionRequest request,
        IReadOnlyDictionary<ulong, EvidenceSourceAnnotation>? annotations = null);
}
