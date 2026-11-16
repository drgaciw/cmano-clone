namespace ProjectAegis.Ai.Host.Aar;

/// <summary>Evidence-linked AAR assistant pipeline (DRG-739).</summary>
public interface IAarAssistantPipeline
{
    /// <summary>
    /// Runs prompt → provider → citation/number validation → optional envelope publish.
    /// Always returns the deterministic report.
    /// </summary>
    Task<AarAssistantResult> RunAsync(
        AarAssistantRequest request,
        CancellationToken cancellationToken = default);
}
