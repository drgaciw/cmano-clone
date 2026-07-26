namespace ProjectAegis.MissionEditor.Cli;

using System.Text.Json;
using ProjectAegis.Data.Catalog;
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
        TextWriter output,
        string? tlTierFilter = null)
    {
        var clock = new FixedCatalogClock(0);
        var exporter = new PlatformWorkbookExporter();
        // Default to Baltic snapshot when --snapshot omitted (was cli-s22-export → silent empty).
        var effectiveSnapshot = string.IsNullOrWhiteSpace(snapshotId)
            ? CatalogValidationDefaults.BalticSnapshotId
            : snapshotId.Trim();
        var filterActive = !string.IsNullOrWhiteSpace(tlTierFilter) && CatalogTlTier.IsValid(tlTierFilter);
        var resolvedTlTier = filterActive ? CatalogTlTier.Normalize(tlTierFilter) : null;
        var snapshotResolved = PlatformCatalogExportResolver.TryResolve(
            dbPath,
            effectiveSnapshot,
            out var data,
            resolvedTlTier);
        if (!snapshotResolved)
        {
            data = PlatformCatalogExportData.Empty;
        }

        var manifest = !string.IsNullOrWhiteSpace(dbPath) && File.Exists(dbPath)
            ? CatalogExportManifest.Resolve(dbPath, effectiveSnapshot)
            : CatalogExportManifest.DefaultForSnapshot(effectiveSnapshot);
        if (filterActive)
        {
            manifest = manifest with { TlTier = resolvedTlTier! };
        }

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
            ok = snapshotResolved,
            verb = "platform_export_xlsx",
            snapshotId = effectiveSnapshot,
            snapshotResolved,
            tlTierFilter = filterActive ? resolvedTlTier : null,
            platformCount = data.Platforms?.Count ?? 0,
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
            note = snapshotResolved
                ? "exported via PlatformWorkbookExporter + IPlatformWorkbookIo (ClosedXML default for .xlsx; canonical fallback via --io canonical)"
                : "snapshot not resolved — empty workbook written; pass --snapshot baltic_patrol or a valid release id",
        };

        output.WriteLine(JsonSerializer.Serialize(payload, JsonOptions));
        // Non-zero when DB was given but snapshot did not resolve (silent-empty was UAT P0).
        if (!string.IsNullOrWhiteSpace(dbPath) && File.Exists(dbPath) && !snapshotResolved)
        {
            return 1;
        }

        return 0;
    }
}