namespace ProjectAegis.Sim.Scenario;

/// <summary>Scenario noise jammer. <paramref name="TechGeneration"/> 0 = unspecified (no ECCM matrix adjustment).</summary>
public sealed record ScenarioJammer(
    string TargetId,
    double JamStrength,
    ulong ActiveFromTick = 0,
    string? ObserverId = null,
    int TechGeneration = 0);
