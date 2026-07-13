namespace ProjectAegis.Delegation.Projection;

using ProjectAegis.Delegation.Orchestration;

/// <summary>
/// Read-only planning-phase chrome flags for C2 map and left drawer (S30-07).
/// </summary>
public sealed record C2PlanningChromeState(
    bool IsMapDimmed,
    bool IsDrawerReadOnly,
    SimulationPhase Phase);