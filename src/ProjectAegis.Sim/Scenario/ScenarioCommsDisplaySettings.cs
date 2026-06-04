namespace ProjectAegis.Sim.Scenario;

/// <summary>C2 map comms presentation (cyber-comms GDD — degraded lag / ghost symbology).</summary>
public sealed class ScenarioCommsDisplaySettings
{
    public static ScenarioCommsDisplaySettings Default { get; } = new(2, 0.06f, 0.04f);

    public ScenarioCommsDisplaySettings(int degradedLagTicks, float ghostOffsetX, float ghostOffsetY)
        : this(degradedLagTicks, ghostOffsetX, ghostOffsetY, degradedOrderDelayTicks: 0, degradedStaleThresholdDivisor: 1)
    {
    }

    public ScenarioCommsDisplaySettings(
        int degradedLagTicks,
        float ghostOffsetX,
        float ghostOffsetY,
        int degradedOrderDelayTicks,
        int degradedStaleThresholdDivisor)
    {
        if (degradedLagTicks < 1 || degradedLagTicks > 10)
        {
            throw new ArgumentOutOfRangeException(nameof(degradedLagTicks), "degradedLagTicks must be in [1, 10].");
        }

        if (degradedOrderDelayTicks < 0 || degradedOrderDelayTicks > 10)
        {
            throw new ArgumentOutOfRangeException(nameof(degradedOrderDelayTicks), "degradedOrderDelayTicks must be in [0, 10].");
        }

        if (degradedStaleThresholdDivisor < 1 || degradedStaleThresholdDivisor > 8)
        {
            throw new ArgumentOutOfRangeException(nameof(degradedStaleThresholdDivisor), "degradedStaleThresholdDivisor must be in [1, 8].");
        }

        GhostOffsetX = Clamp01(ghostOffsetX);
        GhostOffsetY = Clamp01(ghostOffsetY);
        DegradedLagTicks = degradedLagTicks;
        DegradedOrderDelayTicks = degradedOrderDelayTicks;
        DegradedStaleThresholdDivisor = degradedStaleThresholdDivisor;
    }

    public int DegradedLagTicks { get; }

    /// <summary>Extra ticks before a player order executes when comms are degraded.</summary>
    public int DegradedOrderDelayTicks { get; }

    /// <summary>Divides contact stale threshold while comms are degraded or denied.</summary>
    public int DegradedStaleThresholdDivisor { get; }

    /// <summary>Normalized X offset for ghost track (stale position hint).</summary>
    public float GhostOffsetX { get; }

    /// <summary>Normalized Y offset for ghost track (stale position hint).</summary>
    public float GhostOffsetY { get; }

    private static float Clamp01(float v) => v < 0f ? 0f : v > 0.25f ? 0.25f : v;
}