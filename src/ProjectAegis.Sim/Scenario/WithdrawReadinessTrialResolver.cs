namespace ProjectAegis.Sim.Scenario;

using ProjectAegis.Data.Catalog;
using ProjectAegis.Sim.Catalog;

/// <summary>Builds sorted withdraw/readiness trials from scenario JSON and/or catalog platform damage.</summary>
public static class WithdrawReadinessTrialResolver
{
    public static IReadOnlyList<ScenarioWithdrawReadinessTrial> Resolve(
        ScenarioPolicyProfile profile,
        ICatalogReader catalog)
    {
        if (profile.WithdrawReadinessTrials.Count > 0)
        {
            return profile.WithdrawReadinessTrials;
        }

        if (profile.CatalogWithdrawTargets.Count == 0)
        {
            return Array.Empty<ScenarioWithdrawReadinessTrial>();
        }

        var trials = new List<ScenarioWithdrawReadinessTrial>(profile.CatalogWithdrawTargets.Count);
        foreach (var target in profile.CatalogWithdrawTargets
                     .OrderBy(t => t.PlatformId, StringComparer.Ordinal))
        {
            if (PhaseBCatalogDamageReadinessStub.TryResolveScenarioTrial(
                    target.PlatformId,
                    target.CurrentHpPct,
                    catalog) is { } resolvedTrial)
            {
                trials.Add(resolvedTrial);
                continue;
            }

            trials.Add(new ScenarioWithdrawReadinessTrial(
                target.PlatformId,
                PhaseBCatalogDamageReadinessStub.NeutralReadinessScore,
                WithdrawRecommended: false,
                CatalogResolved: false));
        }

        return trials;
    }
}