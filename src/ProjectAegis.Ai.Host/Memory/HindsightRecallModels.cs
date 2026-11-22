namespace ProjectAegis.Ai.Host.Memory;

using ProjectAegis.Ai.Contracts;

/// <summary>Request payload for host-side Hindsight memory recall (DRG-746 / AI-S2-4).</summary>
public sealed record HindsightRecallRequest(
    string BankId,
    string Query,
    string Budget = "mid",
    int MaxTokens = 4096);

/// <summary>One memory item returned by the Hindsight recall endpoint (DRG-746 / AI-S2-4).</summary>
public sealed record HindsightRecallItem(
    string Text,
    double Score,
    string? Id = null,
    string? BankId = null,
    string? Context = null,
    string? CreatedAt = null);

/// <summary>Explicit result envelope returned by <see cref="IHindsightRecallClient.RecallAsync"/> (DRG-746 / AI-S2-4).</summary>
public sealed record HindsightRecallResult(
    GenerationStatus Status,
    string BankId,
    string Query,
    IReadOnlyList<HindsightRecallItem> Items,
    string? Error = null)
{
    /// <summary>True when recall completed with <see cref="GenerationStatus.Succeeded"/>.</summary>
    public bool IsSuccess => Status == GenerationStatus.Succeeded;
}
