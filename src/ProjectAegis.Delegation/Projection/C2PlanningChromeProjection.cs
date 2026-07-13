namespace ProjectAegis.Delegation.Projection;

using ProjectAegis.Delegation.Orchestration;

/// <summary>
/// Projects Planning-phase presentation chrome from session phase (ADR-010 read-only contract).
/// </summary>
public static class C2PlanningChromeProjection
{
    public static C2PlanningChromeState Project(SimulationPhase phase)
    {
        var isPlanning = phase == SimulationPhase.Planning;
        return new C2PlanningChromeState(
            IsMapDimmed: isPlanning,
            IsDrawerReadOnly: isPlanning,
            Phase: phase);
    }
}