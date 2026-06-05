namespace ProjectAegis.Data.Catalog;

/// <summary>Scenario TL and swarm tier gates for near-future archetypes (req 09).</summary>
public static class CatalogArchetypeGate
{
    public static CatalogArchetypeBinding[] ApplyTechnologyLevelGate(
        IEnumerable<CatalogArchetypeBinding> archetypes,
        int scenarioMaxTechnologyLevel)
    {
        var maxTl = Math.Clamp(scenarioMaxTechnologyLevel, 0, 5);
        return archetypes
            .Where(a => a.TechnologyLevel <= maxTl)
            .OrderBy(a => a.ArchetypeId, StringComparer.Ordinal)
            .ToArray();
    }

    public static CatalogArchetypeBinding[] ApplySwarmTierCap(
        IEnumerable<CatalogArchetypeBinding> archetypes,
        SwarmTier maxTier)
    {
        var entityCap = SwarmTierLimits.MaxEntitiesFor(maxTier);
        return archetypes
            .Where(a => a.SwarmTier <= maxTier)
            .Where(a => a.MaxSwarmEntities <= entityCap)
            .OrderBy(a => a.ArchetypeId, StringComparer.Ordinal)
            .ToArray();
    }

    public static CatalogArchetypeBinding[] ApplyAllGates(
        IEnumerable<CatalogArchetypeBinding> archetypes,
        int scenarioMaxTechnologyLevel,
        SwarmTier maxSwarmTier) =>
        ApplySwarmTierCap(
            ApplyTechnologyLevelGate(archetypes, scenarioMaxTechnologyLevel),
            maxSwarmTier);
}