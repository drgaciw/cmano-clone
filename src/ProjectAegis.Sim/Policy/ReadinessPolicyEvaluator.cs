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

    public static EffectiveReadiness EvaluateUnit(
        string platformId,
        ScenarioPolicyProfile profile,
        ICatalogReader catalog)
    {
        var scenarioReady = profile.UnitReadiness.TryGetValue(platformId, out var ready)
            ? ready
            : true;

        var catalogTrial = ResolveCatalogTrials(profile, catalog)
            .FirstOrDefault(t => string.Equals(t.PlatformId, platformId, StringComparison.Ordinal));

        if (catalogTrial == null)
        {
            return new EffectiveReadiness(
                scenarioReady,
                PhaseBCatalogDamageReadinessStub.NeutralReadinessScore,
                WithdrawRecommended: false,
                CatalogResolved: false);
        }

        return new EffectiveReadiness(
            scenarioReady,
            catalogTrial.ReadinessScore,
            catalogTrial.WithdrawRecommended,
            catalogTrial.CatalogResolved);
    }
}