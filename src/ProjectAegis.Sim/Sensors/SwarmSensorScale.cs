namespace ProjectAegis.Sim.Sensors;

/// <summary>
/// SWARM-04 / DRG-89: scale sensor detection quality by living swarm integrity.
/// Curve is an explicit tuning knob (default: linear integrity fraction).
/// </summary>
public static class SwarmSensorScale
{
    /// <summary>
    /// Power on integrity fraction before multiplying Pd.
    /// 1.0 = linear; <1 rewards depleted swarms slightly; >1 punishes depletion harder.
    /// </summary>
    public const double IntegrityPower = 1.0;

    /// <summary>Floor scale when at least one drone remains (0 = allow true zero only at 0 drones).</summary>
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
    public static double ScaleFactor(int droneCount, int maxDrones)
    {
        var fraction = IntegrityFraction(droneCount, maxDrones);
        if (fraction <= 0)
        {
            return 0;
        }

        var scale = IntegrityPower == 1.0 ? fraction : Math.Pow(fraction, IntegrityPower);
        if (scale < MinLivingScale)
        {
            scale = MinLivingScale;
        }

        return Math.Clamp(scale, 0, 1);
    }

    /// <summary>Scale a base detection probability by swarm integrity.</summary>
    public static double ScalePd(double basePd, int droneCount, int maxDrones)
    {
        if (basePd <= 0)
        {
            return 0;
        }

        return Math.Clamp(basePd * ScaleFactor(droneCount, maxDrones), 0, 1);
    }
}
