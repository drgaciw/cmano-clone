namespace ProjectAegis.Data.Import;

using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.WriteGate;

/// <summary>Phase 2 — parse CMO markdown and stage rows through the write gate (no direct SQLite writes).</summary>
public static class CmoMarkdownImportProposer
{
    public const int DefaultChunkSize = 500;

    public static CmoMarkdownImportResult ProposeFromMarkdown(
        string databasePath,
        string markdownPath,
        int? maxRecords = null,
        int chunkSize = DefaultChunkSize,
        ICatalogClock? clock = null)
    {
        if (string.IsNullOrWhiteSpace(databasePath))
        {
            throw new ArgumentException("Database path required.", nameof(databasePath));
        }

        if (!File.Exists(markdownPath))
        {
            throw new FileNotFoundException($"CMO markdown not found: {markdownPath}");
        }

        var parsed = CmoMarkdownImporter.ReadSensorBindings(markdownPath, maxRecords);
        var (approved, quarantined) = CatalogImportGate.PartitionForImport(parsed);

        if (!File.Exists(databasePath))
        {
            CatalogSeedBootstrap.SeedBalticPatrol(databasePath, overwrite: false);
        }

        var batches = new List<CmoMarkdownImportBatch>();
        var catalogClock = clock ?? new FixedCatalogClock(0);
        using var gate = new CatalogWriteGate(databasePath, catalogClock);

        var chunks = ChunkBindings(approved, Math.Max(1, chunkSize));
        foreach (var chunk in chunks)
        {
            var batchId = gate.ProposeSensorBatch(
                chunk,
                "agent",
                "cmo-markdown-import",
                $"catalog_import_markdown:{Path.GetFileName(markdownPath)}");
            batches.Add(new CmoMarkdownImportBatch(batchId, chunk.Length));
        }

        if (quarantined.Length > 0)
        {
            CatalogJsonImporter.WriteQuarantineRows(databasePath, quarantined);
        }

        return new CmoMarkdownImportResult(
            parsed.Count,
            approved.Length,
            quarantined.Length,
            batches);
    }

    public static CatalogSensorBinding[][] ChunkBindings(
        IReadOnlyList<CatalogSensorBinding> approved,
        int chunkSize)
    {
        if (approved.Count == 0)
        {
            return [];
        }

        var sorted = approved
            .OrderBy(b => b.PlatformId, StringComparer.Ordinal)
            .ThenBy(b => b.SensorId, StringComparer.Ordinal)
            .ToArray();

        var chunkCount = (sorted.Length + chunkSize - 1) / chunkSize;
        var chunks = new CatalogSensorBinding[chunkCount][];
        for (var i = 0; i < chunkCount; i++)
        {
            chunks[i] = sorted.Skip(i * chunkSize).Take(chunkSize).ToArray();
        }

        return chunks;
    }
}

public sealed record CmoMarkdownImportBatch(string BatchId, int RecordCount);

public sealed record CmoMarkdownImportResult(
    int ParsedCount,
    int ApprovedCount,
    int QuarantinedCount,
    IReadOnlyList<CmoMarkdownImportBatch> Batches);