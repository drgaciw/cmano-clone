namespace ProjectAegis.MissionEditor.Cli;

using System.Text.Json;
using ProjectAegis.Data.Platform;
using ProjectAegis.Data.WriteGate;

/// <summary>
/// S22-02: platform_export_xlsx verb (pattern from CatalogImportMarkdownCommand).
/// Exports via PlatformWorkbookExporter + IPlatformWorkbookIo (Canonical for headless; ClosedXML deferred per ADR-011).
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
        TextWriter output)
    {
        var clock = new FixedCatalogClock(0);
        var exporter = new PlatformWorkbookExporter();
        var data = PlatformCatalogExportData.Empty;
        var wb = exporter.Export(data, string.IsNullOrWhiteSpace(snapshotId) ? "cli-s22-export" : snapshotId, clock);

        string effectiveOut = string.IsNullOrWhiteSpace(outPath) ? "platform-export.platform.txt" : outPath;
        // Canonical produces deterministic roundtrip text (spec for future xlsx adapter); extension .xlsx is semantic.
        var io = new CanonicalTextWorkbookIo();
        io.Write(wb, effectiveOut);

        var payload = new
        {
            ok = true,
            verb = "platform_export_xlsx",
            snapshotId = string.IsNullOrWhiteSpace(snapshotId) ? "cli-s22-export" : snapshotId,
            outPath = effectiveOut,
            note = "exported via PlatformWorkbookExporter + CanonicalTextWorkbookIo (xlsx adapter deferred; see ADR-011)",
        };

        output.WriteLine(JsonSerializer.Serialize(payload, JsonOptions));
        return 0;
    }
}
