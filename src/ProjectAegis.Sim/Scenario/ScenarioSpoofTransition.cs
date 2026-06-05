namespace ProjectAegis.Sim.Scenario;

/// <summary>Scenario-timed spoofed contact activation (req 19).</summary>
public sealed record ScenarioSpoofTransition(ulong AtTick, string ContactId, string Reason);