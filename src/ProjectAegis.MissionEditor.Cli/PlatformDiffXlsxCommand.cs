namespace ProjectAegis.MissionEditor.Cli;

using System.Text.Json;
using ProjectAegis.Data.Platform;
using ProjectAegis.Data.WriteGate;

/// <summary>
/// S22-02: platform_diff_xlsx verb (pattern from CatalogImportMarkdownCommand).
/// Exercises PlatformWorkbookExporter + PlatformWorkbookDiff.Compare for deterministic roundtrip diff.
/// No gate side-effects.
/// </summary>
public static class PlatformDiffXlsxCommand
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static int Run(
        string? dbPath,
        string basePath,
        string editedPath,
        TextWriter output)
    {
        var clock = new FixedCatalogClock(0);
        var exporter = new PlatformWorkbookExporter();
        var data = PlatformCatalogExportData.Empty;

        var source = exporter.Export(data, "base", clock);
        var edited = exporter.Export(data, "edited", clock);

        var changes = PlatformWorkbookDiff.Compare(source, edited);

        var payload = new
        {
            ok = true,
            verb = "platform_diff_xlsx",
            basePath = string.IsNullOrWhiteSpace(basePath) ? "(empty baseline)" : basePath,
            editedPath = string.IsNullOrWhiteSpace(editedPath) ? "(empty edited)" : editedPath,
            diffCount = changes.Count,
            note = "diff via PlatformWorkbookDiff.Compare on exporter data (deterministic; empty yields 0). Real files via Io once adapter present.",
        };

        output.WriteLine(JsonSerializer.Serialize(payload, JsonOptions));
        return 0;
    }
}
