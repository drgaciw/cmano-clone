namespace ProjectAegis.MissionEditor.Cli;

using System.Text.Json;
using ProjectAegis.Data.WriteGate;

/// <summary>
/// Sprint 19: Headless staging review proxy for OSINT proposals (lists pending, supports approve by batch).
/// Mirrors what a Unity staging UI would call. Uses existing gate ListPending/Approve.
/// </summary>
public static class OsintStagingReviewCommand
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static int Run(string databasePath, string? batchIdToApprove, TextWriter output)
    {
        if (!File.Exists(databasePath))
        {
            output.WriteLine(JsonSerializer.Serialize(new { ok = false, error = "database_not_found" }, JsonOptions));
            return 1;
        }

        using var gate = new CatalogWriteGate(databasePath);
        if (string.IsNullOrWhiteSpace(batchIdToApprove))
        {
            var pending = gate.ListPendingBatches();
            output.WriteLine(JsonSerializer.Serialize(new
            {
                ok = true,
                pending = pending.Select(p => new { p.BatchId, p.RecordCount, p.ActorType, p.ApprovalState }).ToArray()
            }, JsonOptions));
            return 0;
        }

        var decision = gate.ApproveBatch(batchIdToApprove, "human", "osint-ui-reviewer");
        output.WriteLine(JsonSerializer.Serialize(new
        {
            ok = decision.Committed,
            batchId = decision.BatchId,
            errors = decision.Errors
        }, JsonOptions));
        return decision.Committed ? 0 : 1;
    }
}
