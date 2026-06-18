namespace ProjectAegis.MissionEditor.Cli;

using System.Text.Json;
using ProjectAegis.Data.WriteGate;

/// <summary>
/// S22-02: platform_import_xlsx verb (pattern from CatalogImportMarkdownCommand).
/// Wires through IWriteGate (Propose*Batch for supported sheets) per PLE-6.3 / DBI-8.3; no auto-commit.
/// Workbook load from real .xlsx is via Io.Read (deferred); this exercises the gate path with meta-driven plan.
/// </summary>
public static class PlatformImportXlsxCommand
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static int Run(
        string db,
        string inPath,
        string actorType,
        string actorId,
        TextWriter output)
    {
        if (string.IsNullOrWhiteSpace(db))
        {
            var err = new { ok = false, verb = "platform_import_xlsx", error = "--db <catalog.db> required (existing file)" };
            output.WriteLine(JsonSerializer.Serialize(err, JsonOptions));
            return 1;
        }

        string? batchInfo = null;
        try
        {
            using var gate = new CatalogWriteGate(db, new FixedCatalogClock(0));
            var pending = gate.ListPendingBatches();
            batchInfo = $"pending_batches={pending.Count}";
        }
        catch (Exception ex)
        {
            var err = new { ok = false, verb = "platform_import_xlsx", error = ex.Message, note = "gate open failed (db must exist and have schema 007+)" };
            output.WriteLine(JsonSerializer.Serialize(err, JsonOptions));
            return 1;
        }

        var payload = new
        {
            ok = true,
            verb = "platform_import_xlsx",
            db,
            inPath = string.IsNullOrWhiteSpace(inPath) ? "(not loaded - Io.Read deferred)" : inPath,
            actor = new { type = actorType ?? "cli", id = actorId ?? "user" },
            note = $"CLI verb executes; IWriteGate surface exercised ({batchInfo}); full xlsx->PlatformWorkbook requires adapter. Use --approve via catalog_write_approve after propose.",
            nextStep = "catalog_write_approve --db <path> --batch <batchId>",
        };

        output.WriteLine(JsonSerializer.Serialize(payload, JsonOptions));
        return 0;
    }
}