namespace ProjectAegis.MissionEditor.Cli;

using System.Text.Json;
using ProjectAegis.Data.Import;

/// <summary>Phase 2 CLI — propose CMO markdown (sensor / weapon / platform) through the catalog write gate.</summary>
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
        TextWriter output,
        string? reportOutPath = null,
        CmoMarkdownImportEntity entity = CmoMarkdownImportEntity.Sensor,
        bool mapBalticPlatformIds = false)
    {
        var result = entity switch
        {
            CmoMarkdownImportEntity.Weapon => CmoMarkdownImportProposer.ProposeWeaponsFromMarkdown(
                databasePath,
                markdownPath,
                maxRecords,
                chunkSize),
            CmoMarkdownImportEntity.Platform => CmoMarkdownImportProposer.ProposePlatformsFromMarkdown(
                databasePath,
                markdownPath,
                mapBalticPlatformIds,
                weaponMarkdownPath: null,
                maxRecords: maxRecords,
                chunkSize: chunkSize),
            _ => CmoMarkdownImportProposer.ProposeFromMarkdown(
                databasePath,
                markdownPath,
                maxRecords,
                chunkSize),
        };

        var payload = BuildPayload(result, entity);
        var json = JsonSerializer.Serialize(payload, JsonOptions);
        output.WriteLine(json);

        if (!string.IsNullOrWhiteSpace(reportOutPath))
        {
            File.WriteAllText(reportOutPath, json);
        }

        return 0;
    }

    internal static object BuildPayload(CmoMarkdownImportResult result, CmoMarkdownImportEntity entity = CmoMarkdownImportEntity.Sensor)
    {
        var payload = new Dictionary<string, object?>
        {
            ["ok"] = true,
            ["entity"] = entity.ToString().ToLowerInvariant(),
            ["parsedCount"] = result.ParsedCount,
            ["approvedCount"] = result.ApprovedCount,
            ["quarantinedCount"] = result.QuarantinedCount,
            ["batchCount"] = result.Batches.Count,
            ["batches"] = result.Batches.Select(b => new { b.BatchId, b.RecordCount }).ToArray(),
            ["nextStep"] = "catalog_write_approve --db <path> --batch <batchId>",
        };

        if (result.QuarantinedCount > 0)
        {
            payload["quarantineReport"] = result.QuarantineReport
                .Select(q => new
                {
                    platformId = q.PlatformId,
                    sensorId = q.SensorId,
                    reason = q.Reason,
                    sourceFile = q.SourceFile,
                })
                .ToArray();
        }

        return payload;
    }

    public static CmoMarkdownImportEntity ParseEntity(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return CmoMarkdownImportEntity.Sensor;
        }

        return raw.Trim().ToLowerInvariant() switch
        {
            "weapon" or "weapons" => CmoMarkdownImportEntity.Weapon,
            "platform" or "platforms" => CmoMarkdownImportEntity.Platform,
            "sensor" or "sensors" => CmoMarkdownImportEntity.Sensor,
            _ => throw new ArgumentException($"Unknown --entity '{raw}'. Use sensor, weapon, or platform."),
        };
    }
}