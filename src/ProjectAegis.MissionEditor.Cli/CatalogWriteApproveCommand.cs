namespace ProjectAegis.MissionEditor.Cli;

using System.Text.Json;
using ProjectAegis.Data.Import;
using ProjectAegis.Data.Snapshots;
using ProjectAegis.Data.Telemetry;
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
        string? releaseVersion = null,
        bool enableBalanceDrift = false)
    {
        if (!File.Exists(databasePath))
        {
            output.WriteLine(JsonSerializer.Serialize(new { ok = false, error = "database_not_found" }, JsonOptions));
            return 1;
        }

        var clock = new FixedCatalogClock(2000);
        var balanceDriftSettings = enableBalanceDrift
            ? new CatalogBalanceDriftPipelineSettings(enableBalanceDrift: true)
            : CatalogBalanceDriftPipelineSettings.Disabled;
        BalanceDriftReport balanceDriftAdvisory = BalanceDriftReport.EmptyDisabled;

        using (var gate = new CatalogWriteGate(databasePath, clock))
        {
            if (enableBalanceDrift)
            {
                var entityIds = gate.ListStagingEntityIds(batchId);
                balanceDriftAdvisory = CatalogBalanceDriftPipelineEvaluator.EvaluateForDiff(
                    balanceDriftSettings,
                    entityIds);
            }

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

        var payload = new Dictionary<string, object?>
        {
            ["ok"] = true,
            ["batchId"] = batchId,
            ["releaseVersion"] = bind.ReleaseVersion,
            ["snapshotId"] = bind.SnapshotId,
            ["contentHashSha256"] = bind.ContentHashSha256,
            ["sensorRowCount"] = bind.SensorRowCount,
            ["tlTier"] = bind.TlTier,
        };

        if (enableBalanceDrift)
        {
            payload["balanceDriftAdvisory"] = NightlyApproveBalanceDriftSummary.ToDto(balanceDriftAdvisory);
        }

        output.WriteLine(JsonSerializer.Serialize(payload, JsonOptions));
        return 0;
    }
}