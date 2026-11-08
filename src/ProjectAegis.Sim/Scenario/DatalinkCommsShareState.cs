namespace ProjectAegis.Sim.Scenario;

/// <summary>
/// Datalink peer-share gate driven by contested C2 comms quality (TR-sensor-004, TR-cyber-001).
/// Sim-layer mirror of delegation CommsState — adapter maps at harness boundary.
/// </summary>
public enum DatalinkCommsShareState
{
    Nominal = 0,
    Degraded = 1,
    Denied = 2,
}