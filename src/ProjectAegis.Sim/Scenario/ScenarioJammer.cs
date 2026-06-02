namespace ProjectAegis.Sim.Scenario;

/// <summary>Scenario noise jammer affecting detection Pd (TR-sensor-003 MVP).</summary>
public sealed record ScenarioJammer(
    string TargetId,
    double JamStrength,
    ulong ActiveFromTick = 0,
    string? ObserverId = null);