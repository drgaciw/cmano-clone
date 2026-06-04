namespace ProjectAegis.MissionEditor.Cli;

using System.Text.Json;
using ProjectAegis.Data.WriteGate;

public static class CatalogWriteApproveCommand
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static int Run(string databasePath, string batchId, TextWriter output)
    {
        if (!File.Exists(databasePath))
        {
            output.WriteLine(JsonSerializer.Serialize(new { ok = false, error = "database_not_found" }, JsonOptions));
            return 1;
        }

        using var gate = new CatalogWriteGate(databasePath, new FixedCatalogClock(2000));
        var decision = gate.ApproveBatch(batchId, "human", "reviewer-mcp");
        output.WriteLine(JsonSerializer.Serialize(new
        {
            ok = decision.Committed,
            batchId = decision.BatchId,
            errors = decision.Errors,
        }, JsonOptions));
        return decision.Committed ? 0 : 1;
    }
}