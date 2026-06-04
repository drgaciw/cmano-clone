namespace ProjectAegis.MissionEditor.Cli;

using System.Text.Json;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Scenario.Authoring;

/// <summary>Lists gated near-future spawn plan from scenario metadata (req 09).</summary>
public static class ScenarioNearFutureSpawnCommand
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static int Run(string scenarioPath, TextWriter output)
    {
        if (!File.Exists(scenarioPath))
        {
            output.WriteLine(JsonSerializer.Serialize(new { ok = false, error = "file_not_found", scenarioPath }, JsonOptions));
            return 2;
        }

        var scenario = ScenarioDocumentJsonLoader.LoadFromFile(scenarioPath);
        var units = scenario.Metadata.NearFutureUnits ?? [];
        var requests = units
            .Select(u => new ScenarioNearFutureUnitRequest(u.ArchetypeId, u.UnitId))
            .ToArray();
        var catalogPath = ResolveCatalogPath();
        var plans = NearFutureArchetypeRuntime.PlanSpawns(
            requests,
            scenario.Metadata.MaxTechnologyLevel,
            SwarmTier.Medium,
            catalogPath);

        var payload = new
        {
            ok = true,
            scenarioPath,
            maxTechnologyLevel = scenario.Metadata.MaxTechnologyLevel,
            requested = requests.Length,
            accepted = plans.Count,
            spawns = plans.Select(p => new
            {
                p.ArchetypeId,
                p.UnitId,
                swarmTier = p.SwarmTier.ToString(),
                p.TechnologyLevel,
            }),
        };
        output.WriteLine(JsonSerializer.Serialize(payload, JsonOptions));
        return 0;
    }

    private static string ResolveCatalogPath()
    {
        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 8; i++)
        {
            var candidate = Path.Combine(dir, "data", "catalog", "near_future_archetypes.json");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = Path.GetDirectoryName(dir) ?? dir;
        }

        dir = Directory.GetCurrentDirectory();
        for (var i = 0; i < 8; i++)
        {
            var candidate = Path.Combine(dir, "data", "catalog", "near_future_archetypes.json");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = Path.GetDirectoryName(dir) ?? dir;
        }

        throw new FileNotFoundException("near_future_archetypes.json");
    }
}