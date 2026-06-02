namespace ProjectAegis.Sim.Sensors;

using ProjectAegis.Sim.Scenario;

/// <summary>One sorted detection trial outcome for a tick.</summary>
public readonly record struct DetectionRollResult(
    ScenarioDetectionTrial Trial,
    double Pd,
    double Draw,
    bool Detected);