namespace ProjectAegis.Sim.Swarm;

/// <summary>SWARM-10 Phase B operational modes (distinct from Move/Attack/Hold intents).</summary>
public enum SwarmOperationalMode
{
    Hold = 0,
    Assault = 1,
    Screen = 2,
    Scatter = 3,
    Rejoin = 4,
}
