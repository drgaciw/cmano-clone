namespace ProjectAegis.Sim.Policy;

using ProjectAegis.Data.Catalog;
using ProjectAegis.Sim.Catalog;
using ProjectAegis.Sim.Scenario;

/// <summary>
/// Req-16/21 bounded readiness evaluation: merges scenario launch readiness with catalog withdraw trials.
/// Additive-only — catalog damage absent → scenario readiness unchanged.
/// </summary>
public static class ReadinessPolicyEvaluator
{
    public sealed record EffectiveReadiness(
        bool ReadyForLaunch,
        double ReadinessScore,
        bool WithdrawRecommended,
        bool CatalogResolved);

    public static IReadOnlyList<ScenarioWithdrawReadinessTrial> ResolveCatalogTrials(
        ScenarioPolicyProfile profile,
        ICatalogReader catalog) =>
        WithdrawReadinessTrialResolver.Resolve(profile, catalog);

    public static CatalogMagazineResolver.MagazineReadiness EvaluateMagazine(
        string platformId,
        ICatalogReader catalog) =>
        CatalogMagazineResolver.EvaluateInitialMagazine(platformId, catalog);

    public static PhaseBCatalogMobilityReadinessStub.MobilityReadiness EvaluateMobility(
        string platformId,
        ICatalogReader catalog) =>
        PhaseBCatalogMobilityReadinessStub.EvaluateLaunchReadiness(platformId, catalog);

    public static EmconState EvaluateRadarEmcon(
        string platformId,
        ScenarioPolicyProfile profile,
        ICatalogReader catalog) =>
        ScenarioEmconResolver.ResolveRadar(platformId, profile.UnitRadarEmcon, catalog);

    public static EffectiveReadiness EvaluateUnit(
        string platformId,
        ScenarioPolicyProfile profile,
        ICatalogReader catalog)
    {
        var scenarioReady = profile.UnitReadiness.TryGetValue(platformId, out var ready)
            ? ready
            : true;
        var mobility = EvaluateMobility(platformId, catalog);
        var readyForLaunch = scenarioReady && mobility.ReadyForLaunch;

        var catalogTrial = ResolveCatalogTrials(profile, catalog)
            .FirstOrDefault(t => string.Equals(t.PlatformId, platformId, StringComparison.Ordinal));

        if (catalogTrial == null)
        {
            return new EffectiveReadiness(
                readyForLaunch,
                mobility.CatalogResolved
                    ? mobility.MobilityScore
                    : PhaseBCatalogDamageReadinessStub.NeutralReadinessScore,
                WithdrawRecommended: false,
                CatalogResolved: mobility.CatalogResolved);
        }

        return new EffectiveReadiness(
            readyForLaunch,
            catalogTrial.ReadinessScore,
            catalogTrial.WithdrawRecommended,
            catalogTrial.CatalogResolved || mobility.CatalogResolved);
    }
}