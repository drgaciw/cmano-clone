namespace ProjectAegis.Ai.Host.Evidence;

/// <summary>
/// Host-computed quantitative counts from the source order log (never from a model).
/// Counts are taken from the full chronological log before side filtering.
/// </summary>
public sealed record EvidenceQuantitativeSummary(
    int DecisionCount,
    int PolicyDenialCount,
    int EngagementCount,
    int ControllerChangeCount,
    double? ObjectivesRatio);
