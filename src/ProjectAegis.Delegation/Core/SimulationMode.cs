namespace ProjectAegis.Delegation.Core;

public enum SimulationModeKind
{
    Human,
    Mixed,
    AgentVsAgent,
}

public sealed record SimulationModeProfile(
    SimulationModeKind Kind,
    bool PlayerControlsFriendlySide);
