namespace ProjectAegis.Sim.Swarm.Assault;

/// <summary>
/// SWARM-17 / DRG-106: result of multi-axis auto-split planning.
/// When <see cref="SplitApplied"/> is false the plan is single-axis (or empty) with no fan-out.
/// </summary>
public sealed record SwarmAssaultSplitPlan(
    bool SplitApplied,
    int RequestedAxisCount,
    int EffectiveAxisCount,
    IReadOnlyList<SwarmAssaultAxisAllocation> Axes)
{
    /// <summary>Sum of all axis drone shares (must equal the planned droneCount when non-empty).</summary>
    public int TotalDroneShare
    {
        get
        {
            var sum = 0;
            for (var i = 0; i < Axes.Count; i++)
            {
                sum += Axes[i].DroneShare;
            }

            return sum;
        }
    }
}
