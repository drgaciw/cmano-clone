namespace ProjectAegis.Sim.Sensors;

/// <summary>
/// SWARM-26 / DRG-96 (SWARM-B5): observer-facing classification of a hostile contact
/// as single airframe vs UAS swarm cloud when sensor quality allows.
/// </summary>
public enum SwarmContactClass
{
    /// <summary>Insufficient sensor quality to form a useful class.</summary>
    Unknown = 0,

    /// <summary>Contact resolved as a single air platform.</summary>
    SingleAirframe = 1,

    /// <summary>Contact resolved as a multi-vehicle UAS swarm / cloud.</summary>
    UasSwarmCloud = 2,

    /// <summary>Ambiguous mid-quality hint of multi-return / swarm-like signature.</summary>
    PossibleSwarm = 3,
}
