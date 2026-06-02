namespace ProjectAegis.Sim.Scenario;

/// <summary>Scenario-authored detection trial (until Platform DB basePd).</summary>
public sealed record ScenarioDetectionTrial(
    string ObserverId,
    string SensorId,
    string TargetId,
    string ContactId,
    double BasePd,
    double EnvMask = 1.0,
    double JamStrength = 0.0,
    bool RequiresActiveRadar = true);