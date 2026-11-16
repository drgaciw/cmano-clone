namespace ProjectAegis.Ai.Host.Authoring;

/// <summary>
/// Lightweight counters for comparing Propose outcomes against the keyword stub baseline
/// (<see cref="ProjectAegis.Data.Scenario.Authoring.AiAuthoringServices"/>).
/// </summary>
public sealed record AuthoringBaselineMetrics(
    int ValidDraftCount,
    int DiagnosticCount,
    int ConflictCount,
    int IdempotentNoOpCount);
