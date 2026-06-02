namespace ProjectAegis.Sim.Sensors;

/// <summary>Contact FSM states (sensor GDD MVP).</summary>
public enum ContactLifecycleState
{
    Unknown = 0,
    Detected = 1,
    Classified = 2,
    Identified = 3,
    Lost = 4,
}