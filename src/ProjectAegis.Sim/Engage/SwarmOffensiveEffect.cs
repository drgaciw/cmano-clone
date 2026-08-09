namespace ProjectAegis.Sim.Engage;

/// <summary>
/// SWARM-04 / DRG-88: scale offensive effect by living swarm integrity.
/// Curve is a documented tuning knob (linear integrity fraction by default).
/// </summary>
public static class SwarmOffensiveEffect
{
    /// <summary>Minimum scale when at least one drone remains (avoids zero-shot lockouts at 1/max).</summary>
    public const double MinLivingScale = 0.0;

    /// <summary>
    /// Linear integrity scale: <c>droneCount / maxDrones</c>, clamped to [0, 1].
    /// TUNING: replace with power curve via <see cref="ScaleFactorPower"/> if balance needs it.
    /// </summary>
    public const double ScaleFactorPower = 1.0;

    public static double IntegrityFraction(int droneCount, int maxDrones)
    {
        if (maxDrones <= 0)
        {
            return 0;
        }

        if (droneCount <= 0)
        {
            return 0;
        }

        return Math.Clamp(droneCount / (double)maxDrones, 0, 1);
    }

    /// <summary>
    /// Scales a base offensive effect (damage, salvo weight, or Pk contribution) by living integrity.
    /// Monotonic non-decreasing in droneCount for fixed maxDrones.
    /// </summary>
    public static double Scale(double baseEffect, int droneCount, int maxDrones)
    {
        if (baseEffect <= 0 || maxDrones <= 0 || droneCount <= 0)
        {
            return 0;
        }

        var fraction = IntegrityFraction(droneCount, maxDrones);
        var scale = ScaleFactorPower == 1.0 ? fraction : Math.Pow(fraction, ScaleFactorPower);
        if (scale < MinLivingScale && droneCount > 0)
        {
            scale = MinLivingScale;
        }

        return baseEffect * scale;
    }
}
