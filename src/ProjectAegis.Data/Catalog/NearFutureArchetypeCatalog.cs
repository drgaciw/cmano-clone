namespace ProjectAegis.Data.Catalog;

using System.Text.Json;

/// <summary>Loads req-09 near_future_archetypes.json for TL/swarm gates.</summary>
public static class NearFutureArchetypeCatalog
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public static CatalogArchetypeBinding[] LoadFromFile(string path)
    {
        var json = File.ReadAllText(path);
        return Parse(json);
    }

    public static CatalogArchetypeBinding[] Parse(string json)
    {
        var dto = JsonSerializer.Deserialize<NearFutureArchetypeCatalogDto>(json, Options)
            ?? throw new InvalidDataException("Invalid near_future_archetypes.json");

        var rows = new List<CatalogArchetypeBinding>();
        foreach (var row in dto.Archetypes ?? [])
        {
            if (!SwarmTierLimits.TryParse(row.SwarmTier ?? "Micro", out var tier))
            {
                throw new InvalidDataException($"Unknown swarmTier for {row.ArchetypeId}");
            }

            rows.Add(new CatalogArchetypeBinding(
                row.ArchetypeId ?? "",
                row.TechnologyLevel,
                tier,
                row.MaxSwarmEntities,
                row.TrlLevel));
        }

        return rows
            .OrderBy(r => r.ArchetypeId, StringComparer.Ordinal)
            .ToArray();
    }

    private sealed class NearFutureArchetypeCatalogDto
    {
        public int Version { get; set; }

        public List<NearFutureArchetypeRowDto>? Archetypes { get; set; }
    }

    private sealed class NearFutureArchetypeRowDto
    {
        public string? ArchetypeId { get; set; }

        public int TechnologyLevel { get; set; }

        public string? SwarmTier { get; set; }

        public int MaxSwarmEntities { get; set; }

        public int TrlLevel { get; set; } = 9;
    }
}