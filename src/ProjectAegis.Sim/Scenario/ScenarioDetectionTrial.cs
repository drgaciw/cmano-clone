namespace ProjectAegis.Sim.Scenario;

/// <summary>Scenario-authored detection trial; basePd may come from catalog via <see cref="DetectionTrialResolver"/>.</summary>
public sealed record ScenarioDetectionTrial(
    string ObserverId,
    string SensorId,
    string TargetId,
    string ContactId,
    double BasePd,
    double EnvMask = 1.0,
    double JamStrength = 0.0,
    double EccmFactor = 1.0,
    bool RequiresActiveRadar = true,
    /// <summary>
    /// SWARM-04: precomputed swarm integrity scale for the observer (1.0 = no swarm / full).
    /// Scenarios or spawners set this from <see cref="ProjectAegis.Sim.Sensors.SwarmSensorScale"/>.
    /// </summary>
    double SwarmIntegrityScale = 1.0);
