namespace ProjectAegis.Sim.Scenario;

/// <summary>Contact FSM tuning from scenario JSON (sensor GDD stale/drop/classify).</summary>
public sealed record ScenarioContactLifecycle(
    int StaleThresholdTicks = 30,
    int ClassifyAfterTicks = 0,
    int IdentifyAfterTicks = 0)
{
    public static ScenarioContactLifecycle Default { get; } = new();
}