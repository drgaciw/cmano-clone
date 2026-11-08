namespace ProjectAegis.Sim.Catalog;

using ProjectAegis.Data.Catalog;

/// <summary>
/// Req-21 Phase B smoke: applies committed platform signature to catalog-resolved detection trials.
/// Additive-only — when <see cref="ICatalogReader.TryGetSignature"/> misses, inputs pass through unchanged.
/// </summary>
public static class PhaseBCatalogDetectionModifier
{
    /// <summary>Gameplay-neutral RCS reference (-10 dBsm) used by Baltic Phase B fixtures.</summary>
    public const double NeutralRcsDbsm = -10.0;

    private const double EnvMaskScalePerDb = 0.01;

    public static (double BasePd, double EnvMask) Apply(
        double basePd,
        double envMask,
        ICatalogReader catalog,
        string observerId)
    {
        if (!catalog.TryGetSignature(observerId, out var signature))
        {
            return (basePd, envMask);
        }

        return (basePd, ApplyEnvMask(envMask, signature));
    }

    public static double ApplyEnvMask(double envMask, CatalogSignature signature)
    {
        var factor = 1.0 + (signature.RcsBandDbsm - NeutralRcsDbsm) * EnvMaskScalePerDb;
        factor = Math.Clamp(factor, 0.25, 4.0);
        return Math.Clamp(envMask * factor, 0.0, 1.0);
    }
}