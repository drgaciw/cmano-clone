namespace ProjectAegis.Sim.Scenario;

/// <summary>
/// Scenario-authored or catalog-resolved withdraw/readiness trial;
/// damage inputs may come from catalog via <see cref="WithdrawReadinessTrialResolver"/>.
/// </summary>
public sealed record ScenarioWithdrawReadinessTrial(
    string PlatformId,
    double ReadinessScore,
    bool WithdrawRecommended,
    bool CatalogResolved);