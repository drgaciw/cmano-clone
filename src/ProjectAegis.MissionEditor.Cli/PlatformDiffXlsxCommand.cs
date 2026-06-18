namespace ProjectAegis.MissionEditor.Cli;

using System.Text.Json;
using ProjectAegis.Data.Excel;
using ProjectAegis.Data.Platform;
using ProjectAegis.Data.WriteGate;

/// <summary>
/// S22-02 / S23-01: platform_diff_xlsx verb (pattern from CatalogImportMarkdownCommand).
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
        string? ioFlag,
        TextWriter output)
    {
        var clock = new FixedCatalogClock(0);
        var exporter = new PlatformWorkbookExporter();
        IReadOnlyList<PlatformWorkbookChange> changes;

        if (!string.IsNullOrWhiteSpace(basePath) && !string.IsNullOrWhiteSpace(editedPath)
            && File.Exists(basePath) && File.Exists(editedPath))
        {
            var baseIo = PlatformWorkbookIoSelection.Resolve(basePath, ioFlag, PlatformWorkbookIoFactories.ClosedXml);
            var editedIo = PlatformWorkbookIoSelection.Resolve(editedPath, ioFlag, PlatformWorkbookIoFactories.ClosedXml);
            var source = exporter.ReadFromFile(basePath, baseIo);
            var edited = exporter.ReadFromFile(editedPath, editedIo);
            changes = PlatformWorkbookDiff.Compare(source, edited);
        }
        else
        {
            var data = PlatformCatalogExportData.Empty;
            var source = exporter.Export(data, "base", clock);
            var edited = exporter.Export(data, "edited", clock);
            changes = PlatformWorkbookDiff.Compare(source, edited);
        }

        var payload = new
        {
            ok = true,
            verb = "platform_diff_xlsx",
            basePath = string.IsNullOrWhiteSpace(basePath) ? "(empty baseline)" : basePath,
            editedPath = string.IsNullOrWhiteSpace(editedPath) ? "(empty edited)" : editedPath,
            diffCount = changes.Count,
            note = "diff via PlatformWorkbookDiff.Compare (deterministic; empty yields 0). File paths use IPlatformWorkbookIo when both exist.",
        };

        output.WriteLine(JsonSerializer.Serialize(payload, JsonOptions));
        return 0;
    }
}