namespace ProjectAegis.Sim.Logistics;

/// <summary>
/// Boat / small-craft lifecycle phases (doc 16 LOG-09…11 / CMD-25).
/// Deterministic pure-domain spine — no Unity, no wall-clock.
/// Distinct from air-ops: recovery is stricter than launch; Stranded is a first-class liability.
/// </summary>
public enum BoatOpsPhase
{
    Stowed,
    Prepping,
    Launching,
    Waterborne,
    Returning,
    Alongside,
    Recovering,
    Maintenance,
    Stranded,
}
