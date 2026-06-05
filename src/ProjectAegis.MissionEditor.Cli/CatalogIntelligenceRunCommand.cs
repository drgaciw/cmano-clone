namespace ProjectAegis.MissionEditor.Cli;

using System.Text.Json;
using ProjectAegis.Data.Agents;
using ProjectAegis.Data.Catalog;

/// <summary>Headless req-06 database intelligence pipeline report (MCP/CI).</summary>
public static class CatalogIntelligenceRunCommand
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static int Run(string? databasePath, TextWriter output)
    {
        ICatalogReader catalog;
        IDisposable? disposable = null;
        if (!string.IsNullOrWhiteSpace(databasePath) && File.Exists(databasePath))
        {
            var sqlite = new SqliteCatalogReader(Path.GetFullPath(databasePath), "mcp-intelligence");
            catalog = sqlite;
            disposable = sqlite;
        }
        else
        {
            catalog = CatalogReaderFactory.TryCreateBalticPatrolReader()
                ?? InMemoryCatalogReader.BalticPatrolFixture();
            disposable = catalog as IDisposable;
            databasePath = CatalogReaderFactory.ResolveBalticPatrolDatabasePath();
        }

        try
        {
            var result = new DatabaseIntelligenceOrchestrator().Run(catalog, databasePath);
            var payload = new
            {
                ok = result.Passed,
                agents = result.Reports.Select(r => new
                {
                    agentId = r.AgentId,
                    passed = r.Passed,
                    findings = r.Findings.Select(f => new { f.Code, f.Message, f.Severity }),
                }),
                mcpTools = new[]
                {
                    "catalog_intelligence_run",
                    "catalog_entity_map",
                    "catalog_write_propose",
                    "catalog_write_approve",
                    "catalog_import_markdown",
                },
            };
            output.WriteLine(JsonSerializer.Serialize(payload, JsonOptions));
            return result.Passed ? 0 : 1;
        }
        finally
        {
            disposable?.Dispose();
        }
    }
}