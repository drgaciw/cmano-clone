namespace ProjectAegis.Ai.Host.Advice;

/// <summary>Host pipeline that explains validated AdviceFrame facts offline or via a chat client.</summary>
public interface IAdviceExplanationPipeline
{
    /// <summary>Runs one explanation; never mutates sim/advice authority fields.</summary>
    Task<AdviceExplanationResult> RunAsync(
        AdviceExplanationRequest request,
        CancellationToken cancellationToken = default);
}
