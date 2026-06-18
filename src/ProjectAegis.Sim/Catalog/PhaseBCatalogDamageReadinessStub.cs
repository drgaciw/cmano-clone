namespace ProjectAegis.Sim.Catalog;

using ProjectAegis.Data.Catalog;
using ProjectAegis.Sim.Scenario;

/// <summary>
/// Req-21 Phase B: applies committed platform damage to catalog-resolved withdraw/readiness trials.
/// Additive-only — when <see cref="ICatalogReader.TryGetPlatformDamage"/> misses, inputs pass through unchanged.
/// </summary>
public static class PhaseBCatalogDamageReadinessStub
{
    /// <summary>Gameplay-neutral readiness when catalog damage is absent (Baltic legacy fixtures).</summary>
    public const double NeutralReadinessScore = 1.0;

    public sealed record WithdrawReadinessTrial(
        double ReadinessScore,
        bool WithdrawRecommended,
        bool CatalogResolved);

    public static WithdrawReadinessTrial EvaluateWithdrawReadiness(
        string platformId,
        double currentHpPct,
        ICatalogReader catalog)
    {
        if (!catalog.TryGetPlatformDamage(platformId, out var damage))
        {
            return new WithdrawReadinessTrial(
                NeutralReadinessScore,
                WithdrawRecommended: false,
                CatalogResolved: false);
        }

        var clampedHpPct = Math.Clamp(currentHpPct, 0.0, 100.0);
        var currentHp = clampedHpPct / 100.0 * damage.MaxHp;
        var withdrawRecommended = damage.WithdrawThresholdPct > 0
            && currentHp <= damage.WithdrawThresholdPct;

        return new WithdrawReadinessTrial(
            ComputeReadinessScore(clampedHpPct, damage),
            withdrawRecommended,
            CatalogResolved: true);
    }

    public static double ComputeReadinessScore(double currentHpPct, CatalogPlatformDamage damage)
    {
        var hpNorm = Math.Clamp(currentHpPct / 100.0, 0.0, 1.0);
        if (damage.CriticalFlags <= 0)
        {
            return hpNorm;
        }

        const double criticalPenaltyPerFlag = 0.1;
        var penalty = Math.Min(0.5, damage.CriticalFlags * criticalPenaltyPerFlag);
        return Math.Clamp(hpNorm - penalty, 0.0, 1.0);
    }

    /// <summary>Maps catalog damage columns to a scenario trial DTO when damage resolves.</summary>
    public static ScenarioWithdrawReadinessTrial? TryResolveScenarioTrial(
        string platformId,
        double currentHpPct,
        ICatalogReader catalog)
    {
        var trial = EvaluateWithdrawReadiness(platformId, currentHpPct, catalog);
        if (!trial.CatalogResolved)
        {
            return null;
        }

        return new ScenarioWithdrawReadinessTrial(
            platformId,
            trial.ReadinessScore,
            trial.WithdrawRecommended,
            CatalogResolved: true);
    }
}