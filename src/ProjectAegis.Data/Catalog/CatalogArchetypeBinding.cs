namespace ProjectAegis.Data.Catalog;

/// <summary>Near-future platform archetype row (req 09 TL + swarm tier).</summary>
public sealed record CatalogArchetypeBinding(
    string ArchetypeId,
    int TechnologyLevel,
    SwarmTier SwarmTier,
    int MaxSwarmEntities,
    int TrlLevel = 9,
    string ReviewState = CatalogReviewStates.Approved);