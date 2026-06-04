namespace ProjectAegis.Delegation.Comms;

using ProjectAegis.Sim.Scenario;

/// <summary>Maps comms state to order execute tick offsets (req 19).</summary>
public static class CommsOrderDelay
{
    public static ulong ComputeExecuteSimTick(
        ulong queuedSimTick,
        CommsState state,
        ScenarioCommsDisplaySettings display)
    {
        var delayTicks = state switch
        {
            CommsState.Degraded => display.DegradedOrderDelayTicks,
            _ => 0,
        };

        return queuedSimTick + (ulong)Math.Max(0, delayTicks);
    }
}