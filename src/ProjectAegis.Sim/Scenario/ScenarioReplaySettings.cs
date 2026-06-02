namespace ProjectAegis.Sim.Scenario;

/// <summary>Replay/checkpoint knobs from scenario policy (order-log-replay GDD).</summary>
public sealed record ScenarioReplaySettings(int CheckpointIntervalTicks = 300)
{
    public static ScenarioReplaySettings Default { get; } = new();
}