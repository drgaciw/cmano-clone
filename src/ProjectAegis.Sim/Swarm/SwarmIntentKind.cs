namespace ProjectAegis.Sim.Swarm;

/// <summary>
/// SWARM-03 Phase A intents: aggregate swarm orders (no per-drone selection).
/// Distinct from Phase B operational modes (Screen/Scatter/Rejoin).
/// </summary>
public enum SwarmIntentKind
{
    Hold = 0,
    Move = 1,
    Attack = 2,
}
