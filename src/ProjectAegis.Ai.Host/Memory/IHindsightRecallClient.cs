namespace ProjectAegis.Ai.Host.Memory;

/// <summary>
/// Host-side recall contract against the Hindsight memory sidecar (DRG-746 / AI-S2-4).
/// Separate from <c>ProjectAegis.Delegation.Hindsight.IHindsightMemoryClient</c>, which stays untouched.
/// </summary>
public interface IHindsightRecallClient
{
    /// <summary>Queries a Hindsight memory bank and maps transport/parse failures into explicit statuses.</summary>
    Task<HindsightRecallResult> RecallAsync(
        HindsightRecallRequest request,
        CancellationToken cancellationToken = default);
}
