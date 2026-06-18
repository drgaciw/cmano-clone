namespace ProjectAegis.MissionEditor.Cli;

using System.Text.Json;
using ProjectAegis.Data.Excel;
using ProjectAegis.Data.Platform;
using ProjectAegis.Data.WriteGate;

/// <summary>
/// S22-02 / S23-01: platform_import_xlsx verb (pattern from CatalogImportMarkdownCommand).
/// Wires through IWriteGate (Propose*Batch for supported sheets) per PLE-6.3 / DBI-8.3; no auto-commit.
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
        string? ioFlag,
        TextWriter output)
    {
        if (string.IsNullOrWhiteSpace(db))
        {
            var err = new { ok = false, verb = "platform_import_xlsx", error = "--db <catalog.db> required (existing file)" };
            output.WriteLine(JsonSerializer.Serialize(err, JsonOptions));
            return 1;
        }

        if (string.IsNullOrWhiteSpace(inPath))
        {
            var err = new { ok = false, verb = "platform_import_xlsx", error = "--in <workbook.xlsx> required" };
            output.WriteLine(JsonSerializer.Serialize(err, JsonOptions));
            return 1;
        }

        if (!File.Exists(inPath))
        {
            var err = new { ok = false, verb = "platform_import_xlsx", error = $"workbook not found: {inPath}" };
            output.WriteLine(JsonSerializer.Serialize(err, JsonOptions));
            return 1;
        }

        try
        {
            var clock = new FixedCatalogClock(0);
            var io = PlatformWorkbookIoSelection.Resolve(
                inPath,
                ioFlag,
                PlatformWorkbookIoFactories.ClosedXml);
            var writeService = new PlatformWorkbookWriteService();
            var writeResult = writeService.ProposeFromFile(
                db,
                inPath,
                io,
                clock,
                actorType ?? "cli",
                actorId ?? "user");
            var result = writeResult.Import;

            var payload = new
            {
                ok = true,
                verb = "platform_import_xlsx",
                db,
                inPath,
                actor = new { type = actorType ?? "cli", id = actorId ?? "user" },
                io = io.GetType().Name,
                snapshotId = result.Plan.SourceSnapshotId,
                snapshotResolved = result.Plan.SnapshotResolved,
                changeCount = result.Plan.Changes.Count,
                staged = result.Staged,
                proposed = writeResult.Proposed,
                batchIds = writeResult.BatchIds,
                sensorBatchId = result.SensorBatchId,
                mountBatchId = result.MountBatchId,
                loadoutBatchId = result.LoadoutBatchId,
                magazineBatchId = result.MagazineBatchId,
                commsBatchId = result.CommsBatchId,
                mobilityBatchId = result.MobilityBatchId,
                signatureBatchId = result.SignatureBatchId,
                emconBatchId = result.EmconBatchId,
                damageBatchId = result.DamageBatchId,
                requiresHumanApproval = result.Plan.RequiresHumanApproval,
                notes = result.Notes,
                nextStep = "catalog_write_approve --db <path> --batch <batchId>",
            };

            output.WriteLine(JsonSerializer.Serialize(payload, JsonOptions));
            return 0;
        }
        catch (Exception ex)
        {
            var err = new { ok = false, verb = "platform_import_xlsx", error = ex.Message, note = "gate/import failed (db must exist and have schema 007+)" };
            output.WriteLine(JsonSerializer.Serialize(err, JsonOptions));
            return 1;
        }
    }

}