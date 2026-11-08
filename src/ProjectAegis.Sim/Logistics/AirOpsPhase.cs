namespace ProjectAegis.Sim.Logistics;

/// <summary>
/// Air-frame lifecycle phases (doc 16 LOG-08 / CMD-24 Phase N).
/// Deterministic pure-domain spine — no Unity, no wall-clock.
/// </summary>
public enum AirOpsPhase
{
    OnGround,
    Prepping,
    Taxiing,
    TakingOff,
    Airborne,
    Landing,
    Maintenance,
}
