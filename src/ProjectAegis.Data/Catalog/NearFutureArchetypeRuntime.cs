namespace ProjectAegis.Data.Catalog;

/// <summary>Plans gated near-future spawns for headless/Unity runtime (req 09).</summary>
public static class NearFutureArchetypeRuntime
{
    public sealed record SpawnPlan(string ArchetypeId, string UnitId, SwarmTier SwarmTier, int TechnologyLevel);

    public static IReadOnlyList<SpawnPlan> PlanSpawns(
        IEnumerable<ScenarioNearFutureUnitRequest> requests,
        int scenarioMaxTechnologyLevel,
        SwarmTier maxSwarmTier,
        string catalogPath)
    {
        var rows = NearFutureArchetypeCatalog.LoadFromFile(catalogPath);
        var gated = CatalogArchetypeGate.ApplyAllGates(rows, scenarioMaxTechnologyLevel, maxSwarmTier);
        var allowed = gated.ToDictionary(a => a.ArchetypeId, StringComparer.Ordinal);

        var plans = new List<SpawnPlan>();
        foreach (var request in requests)
        {
            if (!allowed.TryGetValue(request.ArchetypeId, out var binding))
            {
                continue;
            }

            plans.Add(new SpawnPlan(
                binding.ArchetypeId,
                request.UnitId,
                binding.SwarmTier,
                binding.TechnologyLevel));
        }

        return plans.OrderBy(p => p.UnitId, StringComparer.Ordinal).ToArray();
    }
}

/// <summary>Spawn request independent of scenario JSON layer.</summary>
public sealed record ScenarioNearFutureUnitRequest(string ArchetypeId, string UnitId);