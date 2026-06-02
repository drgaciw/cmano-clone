namespace ProjectAegis.Sim.Scenario;

/// <summary>Contact FSM tuning from scenario JSON (sensor GDD stale/drop).</summary>
public sealed record ScenarioContactLifecycle(int StaleThresholdTicks = 30)
{
    public static ScenarioContactLifecycle Default { get; } = new();
}