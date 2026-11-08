namespace ProjectAegis.Sim.Scenario;

using Sensors;

/// <summary>Scenario-authored detection trial; basePd may come from catalog via <see cref="DetectionTrialResolver"/>.</summary>
/// <param name="ObserverId">Observer unit identifier.</param>
/// <param name="SensorId">Sensor identifier.</param>
/// <param name="TargetId">Target unit identifier.</param>
/// <param name="ContactId">Projected contact identifier.</param>
/// <param name="BasePd">Base detection probability.</param>
/// <param name="EnvMask">Environmental detection multiplier.</param>
/// <param name="JamStrength">Authored jamming strength.</param>
/// <param name="EccmFactor">Electronic counter-countermeasure factor.</param>
/// <param name="RequiresActiveRadar">Whether the trial requires active radar.</param>
/// <param name="SwarmIntegrityScale">Precomputed swarm integrity scale for the observer; 1.0 means a non-swarm or full-integrity observer. Scenarios or spawners obtain it from <see cref="SwarmSensorScale"/>.</param>
/// <param name="Modality">Sensor modality. <see cref="SensorModality.Radar"/> preserves existing call sites; RF jammers apply only to radar while IR and visual trials use their authored jam strength.</param>
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
    double SwarmIntegrityScale = 1.0,
    SensorModality Modality = SensorModality.Radar);
