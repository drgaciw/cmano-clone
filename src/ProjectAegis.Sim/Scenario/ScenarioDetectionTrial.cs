namespace ProjectAegis.Sim.Scenario;

using ProjectAegis.Sim.Sensors;

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
    double SwarmIntegrityScale = 1.0,
    /// <summary>
    /// S111 / DRG-10: sensor modality. Default <see cref="SensorModality.Radar"/> preserves existing call sites.
    /// RF ScenarioJamResolver jammers apply only when Radar; IR/Visual use trial.JamStrength only.
    /// </summary>
    SensorModality Modality = SensorModality.Radar);
