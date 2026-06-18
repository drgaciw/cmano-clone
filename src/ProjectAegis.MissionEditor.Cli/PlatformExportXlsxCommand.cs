namespace ProjectAegis.MissionEditor.Cli;

using System.Text.Json;
using ProjectAegis.Data.Excel;
using ProjectAegis.Data.Platform;
using ProjectAegis.Data.Snapshots;
using ProjectAegis.Data.WriteGate;

/// <summary>
/// S22-02 / S23-01: platform_export_xlsx verb (pattern from CatalogImportMarkdownCommand).
/// Exports via PlatformWorkbookExporter + IPlatformWorkbookIo (ClosedXML for .xlsx; canonical text fallback).
/// Always executes, returns McpToolResult-style JSON. No auto-commit.
/// </summary>
public static class PlatformExportXlsxCommand
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static int Run(
        string? dbPath,
        string outPath,
        string snapshotId,
        string? ioFlag,
        TextWriter output)
    {
        var clock = new FixedCatalogClock(0);
        var exporter = new PlatformWorkbookExporter();
        var effectiveSnapshot = string.IsNullOrWhiteSpace(snapshotId) ? "cli-s22-export" : snapshotId;
        var snapshotResolved = PlatformCatalogExportResolver.TryResolve(dbPath, effectiveSnapshot, out var data);
        if (!snapshotResolved)
        {
            data = PlatformCatalogExportData.Empty;
        }

        var manifest = !string.IsNullOrWhiteSpace(dbPath) && File.Exists(dbPath)
            ? CatalogExportManifest.Resolve(dbPath, effectiveSnapshot)
            : CatalogExportManifest.DefaultForSnapshot(effectiveSnapshot);
        var wb = exporter.Export(data, effectiveSnapshot, clock, manifest);

        var effectiveOut = string.IsNullOrWhiteSpace(outPath)
            ? "platform-export.xlsx"
            : outPath;
        var io = PlatformWorkbookIoSelection.Resolve(
            effectiveOut,
            ioFlag,
            PlatformWorkbookIoFactories.ClosedXml);
        exporter.WriteToFile(wb, effectiveOut, io);

        var payload = new
        {
            ok = true,
            verb = "platform_export_xlsx",
            snapshotId = effectiveSnapshot,
            snapshotResolved,
            manifest = new
            {
                dbVersion = manifest.DbVersion,
                tlTier = manifest.TlTier,
                schemaVersion = manifest.SchemaVersion,
                contentHash = manifest.ContentHash,
                exportSchemaVersion = manifest.ExportSchemaVersion,
            },
            phaseBMobilityRows = data.Mobility?.Count ?? 0,
            phaseBSignatureRows = data.Signatures?.Count ?? 0,
            phaseBEmconRows = data.Emcon?.Count ?? 0,
            outPath = effectiveOut,
            io = io.GetType().Name,
            note = "exported via PlatformWorkbookExporter + IPlatformWorkbookIo (ClosedXML default for .xlsx; canonical fallback via --io canonical)",
        };

        output.WriteLine(JsonSerializer.Serialize(payload, JsonOptions));
        return 0;
    }
}