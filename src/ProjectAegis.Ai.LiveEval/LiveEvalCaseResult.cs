using ProjectAegis.Ai.Contracts;

namespace ProjectAegis.Ai.LiveEval;

/// <summary>One fixture/case measurement row (fake or future paid).</summary>
public sealed record LiveEvalCaseResult(
    string CaseId,
    string ProviderId,
    string ModelId,
    string PromptVersion,
    GenerationStatus Status,
    bool CitationsResolved,
    bool NumbersConsistent,
    long LatencyMilliseconds,
    int? PromptTokens,
    int? CompletionTokens,
    decimal? EstimatedCostUsd,
    string? SkipReason);
