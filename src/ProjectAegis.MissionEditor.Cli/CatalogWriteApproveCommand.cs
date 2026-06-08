namespace ProjectAegis.MissionEditor.Cli;

using System.Text.Json;
using ProjectAegis.Data.Snapshots;
using ProjectAegis.Data.WriteGate;

public static class CatalogWriteApproveCommand
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static int Run(
        string databasePath,
        string batchId,
        TextWriter output,
        string? snapshotId = null,
        string? releaseVersion = null)
    {
        if (!File.Exists(databasePath))
        {
            output.WriteLine(JsonSerializer.Serialize(new { ok = false, error = "database_not_found" }, JsonOptions));
            return 1;
        }

        var clock = new FixedCatalogClock(2000);
        using (var gate = new CatalogWriteGate(databasePath, clock))
        {
            var decision = gate.ApproveBatch(batchId, "human", "reviewer-mcp");
            if (!decision.Committed)
            {
                output.WriteLine(JsonSerializer.Serialize(new
                {
                    ok = false,
                    batchId = decision.BatchId,
                    errors = decision.Errors,
                }, JsonOptions));
                return 1;
            }
        }

        var bind = CatalogSnapshotBinder.BindAfterApprove(
            databasePath,
            batchId,
            clock,
            snapshotId,
            releaseVersion);

        output.WriteLine(JsonSerializer.Serialize(new
        {
            ok = true,
            batchId,
            releaseVersion = bind.ReleaseVersion,
            snapshotId = bind.SnapshotId,
            contentHashSha256 = bind.ContentHashSha256,
            sensorRowCount = bind.SensorRowCount,
        }, JsonOptions));
        return 0;
    }
}