namespace ProjectAegis.Data.Catalog;

/// <summary>
/// SWARM-02: aggregate integrity state for a spawned swarm unit (Data-side sim unit model).
/// <see cref="DroneCount"/> is living drones; platform is destroyed when count reaches 0 (Sim wave).
/// Produced by <see cref="SwarmUnitFactory"/> from catalog <see cref="CatalogSwarmPlatform"/>.
/// </summary>
public sealed record SwarmUnitIntegrity(
    string UnitId,
    string PlatformId,
    int DroneCount,
    int MaxDrones)
{
    public bool IsDestroyed => DroneCount <= 0;

    public double IntegrityFraction =>
        MaxDrones <= 0 ? 0 : Math.Clamp(DroneCount / (double)MaxDrones, 0, 1);
}
