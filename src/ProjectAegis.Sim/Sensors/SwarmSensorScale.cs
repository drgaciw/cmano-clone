namespace ProjectAegis.Sim.Sensors;

/// <summary>
/// SWARM-04 / DRG-89: scale sensor detection quality by living swarm integrity.
/// Default curve constants are compile-time defaults; call sites may override per scenario.
/// </summary>
public static class SwarmSensorScale
{
    /// <summary>
    /// Default power on integrity fraction before multiplying Pd.
    /// 1.0 = linear; <1 rewards depleted swarms slightly; >1 punishes depletion harder.
    /// </summary>
    public const double IntegrityPower = 1.0;

    /// <summary>Default floor scale when at least one drone remains.</summary>
    public const double MinLivingScale = 0.0;

    public static double IntegrityFraction(int droneCount, int maxDrones)
    {
        if (maxDrones <= 0 || droneCount <= 0)
        {
            return 0;
        }

        return Math.Clamp(droneCount / (double)maxDrones, 0, 1);
    }

    /// <summary>
    /// Multiplier applied to base Pd (or range factor). Monotonic non-decreasing in droneCount.
    /// </summary>
    public static double ScaleFactor(
        int droneCount,
        int maxDrones,
        double integrityPower = IntegrityPower,
        double minLivingScale = MinLivingScale)
    {
        var fraction = IntegrityFraction(droneCount, maxDrones);
        if (fraction <= 0)
        {
            return 0;
        }

        var scale = Math.Abs(integrityPower - 1.0) < 1e-12
            ? fraction
            : Math.Pow(fraction, integrityPower);
        if (scale < minLivingScale)
        {
            scale = minLivingScale;
        }

        return Math.Clamp(scale, 0, 1);
    }

    /// <summary>Scale a base detection probability by swarm integrity.</summary>
    public static double ScalePd(
        double basePd,
        int droneCount,
        int maxDrones,
        double integrityPower = IntegrityPower,
        double minLivingScale = MinLivingScale)
    {
        if (basePd <= 0)
        {
            return 0;
        }

        return Math.Clamp(
            basePd * ScaleFactor(droneCount, maxDrones, integrityPower, minLivingScale),
            0,
            1);
    }

    /// <summary>Build a trial-ready scale from living integrity (for scenario authoring / spawners).</summary>
    public static double ForTrial(
        int droneCount,
        int maxDrones,
        double integrityPower = IntegrityPower,
        double minLivingScale = MinLivingScale) =>
        ScaleFactor(droneCount, maxDrones, integrityPower, minLivingScale);
}
