namespace ProjectAegis.Sim.Scenario;

/// <summary>Detection trial identity without basePd; resolved from platform catalog.</summary>
public sealed record ScenarioCatalogDetectionTarget(
    string ObserverId,
    string SensorId,
    string TargetId,
    string ContactId,
    double EnvMask = 1.0,
    double JamStrength = 0.0,
    bool RequiresActiveRadar = true);