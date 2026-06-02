namespace ProjectAegis.Sim.Scenario;

/// <summary>Scenario-authored contact appearance (sorted iteration in simulator).</summary>
public sealed record ScenarioContactSeed(
    string ObserverId,
    string TargetId,
    string ContactId,
    ulong AppearAtTick,
    bool HasFireControlTrack = true);