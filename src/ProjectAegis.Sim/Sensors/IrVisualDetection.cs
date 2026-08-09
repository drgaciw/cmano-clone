namespace ProjectAegis.Sim.Sensors;

/// <summary>
/// S111 / DRG-10: environment masks for infrared and visual (EO) detection trials.
/// Radar continues to use <see cref="ProjectAegis.Sim.Scenario.ScenarioDetectionTrial.EnvMask"/> as authored
/// (no day/night model here).
/// </summary>
public static class IrVisualDetection
{
    /// <summary>Default night floor so total blackout is not forced when dayFraction is 0.</summary>
    public const double DefaultVisualNightFloor = 0.05;

    /// <summary>
    /// Visual/EO env mask from day fraction (0 = night … 1 = full day) and optional weather attenuation.
    /// Result is clamp(dayFraction * weatherMask, 0, 1), then raised to at least <paramref name="nightFloor"/>.
    /// </summary>
    public static double ComputeVisualEnvMask(
        double dayFraction /* 0 night .. 1 full day */,
        double weatherMask = 1.0,
        double nightFloor = DefaultVisualNightFloor)
    {
        var day = Math.Clamp(dayFraction, 0, 1);
        var weather = Math.Clamp(weatherMask, 0, 1);
        var floor = Math.Clamp(nightFloor, 0, 1);
        var mask = day * weather;
        if (mask < floor)
        {
            mask = floor;
        }

        return Math.Clamp(mask, 0, 1);
    }

    /// <summary>
    /// Infrared env mask from thermal contrast (0..1) and optional weather attenuation.
    /// Result is clamp(thermalContrast * weatherMask, 0, 1). Independent of day/night.
    /// </summary>
    public static double ComputeInfraredEnvMask(
        double thermalContrast /* 0..1 */,
        double weatherMask = 1.0)
    {
        var contrast = Math.Clamp(thermalContrast, 0, 1);
        var weather = Math.Clamp(weatherMask, 0, 1);
        return Math.Clamp(contrast * weather, 0, 1);
    }
}
