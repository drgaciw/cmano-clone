namespace ProjectAegis.Sim.Scenario;

public sealed record ScenarioCommsTransition(
    ulong AtTick,
    string NewState,
    string NodeId,
    string Reason);