namespace ProjectAegis.Sim.Scenario;

/// <summary>C2 map comms presentation (cyber-comms GDD — degraded lag / ghost symbology).</summary>
public sealed class ScenarioCommsDisplaySettings
{
    public static ScenarioCommsDisplaySettings Default { get; } = new(2, 0.06f, 0.04f);

    public ScenarioCommsDisplaySettings(int degradedLagTicks, float ghostOffsetX, float ghostOffsetY)
    {
        if (degradedLagTicks < 1 || degradedLagTicks > 10)
        {
            throw new ArgumentOutOfRangeException(nameof(degradedLagTicks), "degradedLagTicks must be in [1, 10].");
        }

        GhostOffsetX = Clamp01(ghostOffsetX);
        GhostOffsetY = Clamp01(ghostOffsetY);
        DegradedLagTicks = degradedLagTicks;
    }

    public int DegradedLagTicks { get; }

    /// <summary>Normalized X offset for ghost track (stale position hint).</summary>
    public float GhostOffsetX { get; }

    /// <summary>Normalized Y offset for ghost track (stale position hint).</summary>
    public float GhostOffsetY { get; }

    private static float Clamp01(float v) => v < 0f ? 0f : v > 0.25f ? 0.25f : v;
}