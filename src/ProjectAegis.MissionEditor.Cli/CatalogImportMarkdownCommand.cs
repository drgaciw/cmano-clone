namespace ProjectAegis.MissionEditor.Cli;

using System.Text.Json;
using ProjectAegis.Data.Import;

/// <summary>Phase 2 CLI — propose CMO sensor markdown through the catalog write gate.</summary>
public static class CatalogImportMarkdownCommand
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static int Run(
        string databasePath,
        string markdownPath,
        int? maxRecords,
        int chunkSize,
        TextWriter output)
    {
        var result = CmoMarkdownImportProposer.ProposeFromMarkdown(
            databasePath,
            markdownPath,
            maxRecords,
            chunkSize);

        var payload = new
        {
            ok = true,
            parsedCount = result.ParsedCount,
            approvedCount = result.ApprovedCount,
            quarantinedCount = result.QuarantinedCount,
            batchCount = result.Batches.Count,
            batches = result.Batches.Select(b => new { b.BatchId, b.RecordCount }),
            nextStep = "catalog_write_approve --db <path> --batch <batchId>",
        };

        output.WriteLine(JsonSerializer.Serialize(payload, JsonOptions));
        return 0;
    }
}