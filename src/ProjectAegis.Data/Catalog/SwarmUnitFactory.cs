namespace ProjectAegis.Data.Catalog;

/// <summary>
/// Data-surface unit factory for swarm platforms (DRG-86 / SWARM-A1).
/// Instantiates <see cref="SwarmUnitIntegrity"/> from catalog swarm rows — NOT Sim engagement.
/// </summary>
public static class SwarmUnitFactory
{
    /// <summary>
    /// Spawns integrity fields for a scenario unit referencing a swarm catalog platform id.
    /// Initial count defaults to <see cref="CatalogSwarmPlatform.MaxDrones"/>; clamped to [0, max].
    /// </summary>
    public static bool TryCreate(
        string unitId,
        string platformId,
        ICatalogReader catalog,
        out SwarmUnitIntegrity integrity,
        int? initialDroneCount = null)
    {
        integrity = default!;
        if (string.IsNullOrWhiteSpace(unitId) ||
            string.IsNullOrWhiteSpace(platformId) ||
            catalog is null)
        {
            return false;
        }

        if (!catalog.TryGetSwarmPlatform(platformId.Trim(), out var swarm) || !swarm.IsSwarm)
        {
            return false;
        }

        if (swarm.MaxDrones <= 0)
        {
            return false;
        }

        var count = initialDroneCount ?? swarm.MaxDrones;
        if (count < 0)
        {
            count = 0;
        }
        else if (count > swarm.MaxDrones)
        {
            count = swarm.MaxDrones;
        }

        integrity = new SwarmUnitIntegrity(
            unitId.Trim(),
            swarm.PlatformId,
            count,
            swarm.MaxDrones);
        return true;
    }

    /// <summary>
    /// Same as <see cref="TryCreate"/> but throws when the platform is not a loadable swarm entry.
    /// </summary>
    public static SwarmUnitIntegrity Create(
        string unitId,
        string platformId,
        ICatalogReader catalog,
        int? initialDroneCount = null)
    {
        if (TryCreate(unitId, platformId, catalog, out var integrity, initialDroneCount))
        {
            return integrity;
        }

        throw new InvalidOperationException(
            $"Cannot spawn swarm unit '{unitId}': platform '{platformId}' is not a swarm catalog entry.");
    }
}
