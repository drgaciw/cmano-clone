namespace ProjectAegis.MissionEditor.Cli;

using System.Text.Json;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.WriteGate;

public static class CatalogWriteProposeCommand
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static int Run(string databasePath, string platformId, string sensorId, double basePd, TextWriter output)
    {
        if (!File.Exists(databasePath))
        {
            CatalogSeedBootstrap.SeedBalticPatrol(databasePath, overwrite: false);
        }

        using var gate = new CatalogWriteGate(databasePath, new FixedCatalogClock(1000));
        var proposal = new CatalogSensorBinding(
            platformId,
            sensorId,
            basePd,
            SourceFactId: "agent-proposal",
            Confidence: 0.9,
            ImportBatchId: "mcp-propose",
            SourceFile: "catalog_write_propose",
            ReviewState: CatalogReviewStates.Approved,
            TrlLevel: 9,
            ValueTier: CatalogProvenanceTier.InterpretedValue,
            ReviewerId: "agent:database-intelligence",
            RevisedUtcTicks: 1000,
            CitationRef: "req-06-staging");

        var batchId = gate.ProposeSensorBatch([proposal], "agent", "database-intelligence", "MCP catalog_write_propose");
        output.WriteLine(JsonSerializer.Serialize(new { ok = true, batchId, recordCount = 1 }, JsonOptions));
        return 0;
    }
}