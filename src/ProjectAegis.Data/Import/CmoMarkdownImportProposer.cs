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

        var quarantineReport = BuildQuarantineReport(quarantined);

        return new CmoMarkdownImportResult(
            parsed.Count,
            approved.Length,
            quarantined.Length,
            batches,
            quarantineReport);
    }

    /// <summary>S22-04: parse platform + weapon + mount markdown and stage via write gate (no auto-commit).</summary>
    public static CmoMarkdownPlatformImportResult ProposePlatformWeaponMounts(
        string databasePath,
        string platformMarkdownPath,
        string? weaponMarkdownPath = null,
        bool mapBalticPlatformIds = false,
        ICatalogClock? clock = null)
    {
        if (string.IsNullOrWhiteSpace(databasePath))
        {
            throw new ArgumentException("Database path required.", nameof(databasePath));
        }

        var platforms = CmoMarkdownImporter.ReadPlatformBindings(platformMarkdownPath, mapBalticIds: mapBalticPlatformIds);
        var mounts = CmoMarkdownImporter.ReadPlatformMounts(platformMarkdownPath, mapBalticIds: mapBalticPlatformIds);
        var weapons = weaponMarkdownPath is null || !File.Exists(weaponMarkdownPath)
            ? Array.Empty<CatalogWeaponRecord>()
            : CmoMarkdownImporter.ReadWeaponBindings(weaponMarkdownPath);

        if (!File.Exists(databasePath))
        {
            CatalogSeedBootstrap.SeedBalticPatrol(databasePath, overwrite: false);
        }

        var catalogClock = clock ?? new FixedCatalogClock(0);
        using var gate = new CatalogWriteGate(databasePath, catalogClock);

        string? platformBatchId = null;
        if (platforms.Count > 0)
        {
            platformBatchId = gate.ProposePlatformBatch(
                platforms,
                "agent",
                "cmo-markdown-import",
                $"catalog_import_platform:{Path.GetFileName(platformMarkdownPath)}");
        }

        string? weaponBatchId = null;
        if (weapons.Count > 0)
        {
            weaponBatchId = gate.ProposeWeaponBatch(
                weapons,
                "agent",
                "cmo-markdown-import",
                $"catalog_import_weapon:{Path.GetFileName(weaponMarkdownPath!)}");
        }

        string? mountBatchId = null;
        if (mounts.Count > 0)
        {
            mountBatchId = gate.ProposeMountBatch(
                mounts,
                "agent",
                "cmo-markdown-import",
                $"catalog_import_mount:{Path.GetFileName(platformMarkdownPath)}");
        }

        return new CmoMarkdownPlatformImportResult(
            platforms.Count,
            weapons.Count,
            mounts.Count,
            platformBatchId,
            weaponBatchId,
            mountBatchId);
    }

    public static IReadOnlyList<CmoMarkdownQuarantineReportEntry> BuildQuarantineReport(
        IReadOnlyList<QuarantinedCatalogBinding> quarantined) =>
        quarantined
            .OrderBy(q => q.Binding.PlatformId, StringComparer.Ordinal)
            .ThenBy(q => q.Binding.SensorId, StringComparer.Ordinal)
            .Select(q => new CmoMarkdownQuarantineReportEntry(
                q.Binding.PlatformId,
                q.Binding.SensorId,
                q.RejectionReason,
                q.Binding.SourceFile))
            .ToArray();

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
    IReadOnlyList<CmoMarkdownImportBatch> Batches,
    IReadOnlyList<CmoMarkdownQuarantineReportEntry> QuarantineReport);

public sealed record CmoMarkdownPlatformImportResult(
    int PlatformCount,
    int WeaponCount,
    int MountCount,
    string? PlatformBatchId,
    string? WeaponBatchId,
    string? MountBatchId);