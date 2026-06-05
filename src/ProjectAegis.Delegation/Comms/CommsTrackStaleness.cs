namespace ProjectAegis.Delegation.Comms;

using ProjectAegis.Sim.Scenario;

/// <summary>Contact FSM staleness multiplier when datalink is degraded (req 19).</summary>
public static class CommsTrackStaleness
{
    public static int StaleThresholdDivisor(CommsState state, ScenarioCommsDisplaySettings display) =>
        state switch
        {
            CommsState.Degraded => Math.Max(1, display.DegradedStaleThresholdDivisor),
            CommsState.Denied => Math.Max(1, display.DegradedStaleThresholdDivisor),
            _ => 1,
        };
}