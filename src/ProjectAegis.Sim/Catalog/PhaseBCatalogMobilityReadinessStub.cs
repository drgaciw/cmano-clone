namespace ProjectAegis.Sim.Catalog;

using ProjectAegis.Data.Catalog;

/// <summary>
/// Req-21 Phase B: applies committed platform mobility to bounded launch-readiness evaluation.
/// Additive-only — when <see cref="ICatalogReader.TryGetMobility"/> misses, inputs pass through unchanged.
/// </summary>
public static class PhaseBCatalogMobilityReadinessStub
{
    /// <summary>Gameplay-neutral mobility score when catalog mobility is absent (Baltic legacy fixtures).</summary>
    public const double NeutralMobilityScore = 1.0;

    /// <summary>Baltic fixture reference speed (knots) for normalized readiness scoring.</summary>
    public const double BalticReferenceMaxSpeedKnots = 30.0;

    /// <summary>Baltic fixture reference range (nm) for normalized readiness scoring.</summary>
    public const double BalticReferenceRangeNm = 4000.0;

    public sealed record MobilityReadiness(
        bool ReadyForLaunch,
        double MobilityScore,
        bool CatalogResolved);

    public static MobilityReadiness EvaluateLaunchReadiness(string platformId, ICatalogReader catalog)
    {
        if (!catalog.TryGetMobility(platformId, out var mobility))
        {
            return new MobilityReadiness(
                ReadyForLaunch: true,
                NeutralMobilityScore,
                CatalogResolved: false);
        }

        var ready = mobility.MaxSpeedKnots > 0 && mobility.RangeNm > 0;
        return new MobilityReadiness(
            ready,
            ready ? ComputeMobilityScore(mobility) : 0.0,
            CatalogResolved: true);
    }

    public static double ComputeMobilityScore(CatalogMobility mobility)
    {
        var speedNorm = Math.Clamp(mobility.MaxSpeedKnots / BalticReferenceMaxSpeedKnots, 0.0, 1.0);
        var rangeNorm = Math.Clamp(mobility.RangeNm / BalticReferenceRangeNm, 0.0, 1.0);
        return Math.Clamp((speedNorm + rangeNorm) * 0.5, 0.0, 1.0);
    }
}