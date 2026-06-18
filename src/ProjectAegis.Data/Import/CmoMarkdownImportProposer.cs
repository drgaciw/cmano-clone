namespace ProjectAegis.Data.Import;

using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Platform;
using ProjectAegis.Data.Telemetry;
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
        ICatalogClock? clock = null,
        CatalogBalanceDriftPipelineSettings? balanceDrift = null)
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
        var diffEntityIds = CatalogPipelineDiffEntityResolver.ResolveFromPlatformIds(
            approved.Select(binding => binding.PlatformId));

        return BuildImportResult(
            parsed.Count,
            approved.Length,
            quarantined.Length,
            batches,
            quarantineReport,
            [],
            balanceDrift,
            diffEntityIds);
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

        var weaponLookup = CmoMarkdownImporter.BuildWeaponNameLookup(weapons);
        var loadouts = CmoMarkdownImporter.ReadPlatformLoadouts(platformMarkdownPath, mapBalticIds: mapBalticPlatformIds);
        var (magazines, fittingQuarantine) = CmoMarkdownImporter.PartitionPlatformMagazines(
            platformMarkdownPath,
            mapBalticIds: mapBalticPlatformIds,
            weaponLookup,
            Path.GetFileName(platformMarkdownPath));

        string? loadoutBatchId = null;
        if (loadouts.Count > 0)
        {
            loadoutBatchId = gate.ProposeLoadoutBatch(
                loadouts,
                "agent",
                "cmo-markdown-import",
                $"catalog_import_loadout:{Path.GetFileName(platformMarkdownPath)}");
        }

        string? magazineBatchId = null;
        if (magazines.Count > 0)
        {
            magazineBatchId = gate.ProposeMagazineBatch(
                magazines,
                "agent",
                "cmo-markdown-import",
                $"catalog_import_magazine:{Path.GetFileName(platformMarkdownPath)}");
        }

        return new CmoMarkdownPlatformImportResult(
            platforms.Count,
            weapons.Count,
            mounts.Count,
            loadouts.Count,
            magazines.Count,
            fittingQuarantine.Count,
            platformBatchId,
            weaponBatchId,
            mountBatchId,
            loadoutBatchId,
            magazineBatchId,
            fittingQuarantine);
    }

    /// <summary>S26-02: parse weapon markdown, chunk, and stage via <see cref="IWriteGate.ProposeWeaponBatch"/>.</summary>
    public static CmoMarkdownImportResult ProposeWeaponsFromMarkdown(
        string databasePath,
        string markdownPath,
        int? maxRecords = null,
        int chunkSize = DefaultChunkSize,
        ICatalogClock? clock = null,
        CatalogBalanceDriftPipelineSettings? balanceDrift = null)
    {
        if (string.IsNullOrWhiteSpace(databasePath))
        {
            throw new ArgumentException("Database path required.", nameof(databasePath));
        }

        if (!File.Exists(markdownPath))
        {
            throw new FileNotFoundException($"CMO markdown not found: {markdownPath}");
        }

        var weapons = CmoMarkdownImporter.ReadWeaponBindings(markdownPath, maxRecords);
        EnsureDatabase(databasePath);

        var batches = new List<CmoMarkdownImportBatch>();
        var catalogClock = clock ?? new FixedCatalogClock(0);
        using var gate = new CatalogWriteGate(databasePath, catalogClock);

        foreach (var chunk in ChunkWeapons(weapons, Math.Max(1, chunkSize)))
        {
            var batchId = gate.ProposeWeaponBatch(
                chunk,
                "agent",
                "cmo-markdown-import",
                $"catalog_import_markdown:{Path.GetFileName(markdownPath)}");
            batches.Add(new CmoMarkdownImportBatch(batchId, chunk.Length));
        }

        return BuildImportResult(
            weapons.Count,
            weapons.Count,
            0,
            batches,
            [],
            [],
            balanceDrift,
            diffEntityIds: []);
    }

    /// <summary>S26-03: parse platform (+ mounts) markdown, chunk platforms, stage via write gate.</summary>
    public static CmoMarkdownImportResult ProposePlatformsFromMarkdown(
        string databasePath,
        string markdownPath,
        bool mapBalticPlatformIds = false,
        string? weaponMarkdownPath = null,
        int? maxRecords = null,
        int chunkSize = DefaultChunkSize,
        ICatalogClock? clock = null,
        CatalogBalanceDriftPipelineSettings? balanceDrift = null)
    {
        if (string.IsNullOrWhiteSpace(databasePath))
        {
            throw new ArgumentException("Database path required.", nameof(databasePath));
        }

        if (!File.Exists(markdownPath))
        {
            throw new FileNotFoundException($"CMO markdown not found: {markdownPath}");
        }

        var platforms = CmoMarkdownImporter.ReadPlatformBindings(markdownPath, mapBalticIds: mapBalticPlatformIds);
        if (maxRecords.HasValue)
        {
            platforms = platforms.Take(maxRecords.Value).ToArray();
        }

        var allMounts = CmoMarkdownImporter.ReadPlatformMounts(markdownPath, mapBalticIds: mapBalticPlatformIds);
        var mountLookup = allMounts
            .GroupBy(m => m.PlatformId, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.OrderBy(m => m.MountId, StringComparer.Ordinal).ToArray(), StringComparer.Ordinal);

        var weapons = weaponMarkdownPath is not null && File.Exists(weaponMarkdownPath)
            ? CmoMarkdownImporter.ReadWeaponBindings(weaponMarkdownPath)
            : Array.Empty<CatalogWeaponRecord>();
        var weaponLookup = CmoMarkdownImporter.BuildWeaponNameLookup(weapons);
        var allLoadouts = CmoMarkdownImporter.ReadPlatformLoadouts(markdownPath, mapBalticIds: mapBalticPlatformIds);
        var loadoutLookup = allLoadouts
            .GroupBy(l => l.PlatformId, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.OrderBy(l => l.LoadoutId, StringComparer.Ordinal).ToArray(), StringComparer.Ordinal);
        var (allMagazines, fittingQuarantine) = CmoMarkdownImporter.PartitionPlatformMagazines(
            markdownPath,
            mapBalticIds: mapBalticPlatformIds,
            weaponLookup,
            Path.GetFileName(markdownPath));
        var magazineLookup = allMagazines
            .GroupBy(m => m.PlatformId, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.ToArray(), StringComparer.Ordinal);

        EnsureDatabase(databasePath);

        var batches = new List<CmoMarkdownImportBatch>();
        var catalogClock = clock ?? new FixedCatalogClock(0);
        using var gate = new CatalogWriteGate(databasePath, catalogClock);

        foreach (var platformChunk in ChunkPlatforms(platforms, Math.Max(1, chunkSize)))
        {
            var platformBatchId = gate.ProposePlatformBatch(
                platformChunk,
                "agent",
                "cmo-markdown-import",
                $"catalog_import_markdown:{Path.GetFileName(markdownPath)}");
            batches.Add(new CmoMarkdownImportBatch(platformBatchId, platformChunk.Length));

            var mountChunk = platformChunk
                .SelectMany(p => mountLookup.TryGetValue(p.PlatformId, out var mounts) ? mounts : [])
                .OrderBy(m => m.PlatformId, StringComparer.Ordinal)
                .ThenBy(m => m.MountId, StringComparer.Ordinal)
                .ToArray();

            if (mountChunk.Length > 0)
            {
                var mountBatchId = gate.ProposeMountBatch(
                    mountChunk,
                    "agent",
                    "cmo-markdown-import",
                    $"catalog_import_mount:{Path.GetFileName(markdownPath)}");
                batches.Add(new CmoMarkdownImportBatch(mountBatchId, mountChunk.Length));
            }

            var loadoutChunk = platformChunk
                .SelectMany(p => loadoutLookup.TryGetValue(p.PlatformId, out var loadouts) ? loadouts : [])
                .OrderBy(l => l.PlatformId, StringComparer.Ordinal)
                .ThenBy(l => l.LoadoutId, StringComparer.Ordinal)
                .ToArray();

            if (loadoutChunk.Length > 0)
            {
                var loadoutBatchId = gate.ProposeLoadoutBatch(
                    loadoutChunk,
                    "agent",
                    "cmo-markdown-import",
                    $"catalog_import_loadout:{Path.GetFileName(markdownPath)}");
                batches.Add(new CmoMarkdownImportBatch(loadoutBatchId, loadoutChunk.Length));
            }

            var magazineChunk = platformChunk
                .SelectMany(p => magazineLookup.TryGetValue(p.PlatformId, out var magazines) ? magazines : [])
                .OrderBy(m => m.PlatformId, StringComparer.Ordinal)
                .ThenBy(m => m.LoadoutId, StringComparer.Ordinal)
                .ThenBy(m => m.MountId, StringComparer.Ordinal)
                .ThenBy(m => m.WeaponId, StringComparer.Ordinal)
                .ToArray();

            if (magazineChunk.Length > 0)
            {
                var magazineBatchId = gate.ProposeMagazineBatch(
                    magazineChunk,
                    "agent",
                    "cmo-markdown-import",
                    $"catalog_import_magazine:{Path.GetFileName(markdownPath)}");
                batches.Add(new CmoMarkdownImportBatch(magazineBatchId, magazineChunk.Length));
            }
        }

        var diffEntityIds = CatalogPipelineDiffEntityResolver.ResolveFromPlatformIds(
            platforms.Select(platform => platform.PlatformId));

        return BuildImportResult(
            platforms.Count,
            platforms.Count,
            fittingQuarantine.Count,
            batches,
            [],
            fittingQuarantine,
            balanceDrift,
            diffEntityIds);
    }

    public static CatalogWeaponRecord[][] ChunkWeapons(
        IReadOnlyList<CatalogWeaponRecord> weapons,
        int chunkSize)
    {
        if (weapons.Count == 0)
        {
            return [];
        }

        var sorted = CatalogSortKeyComparer.SortWeapons(weapons);
        return ChunkArray(sorted, chunkSize);
    }

    public static CatalogPlatformBinding[][] ChunkPlatforms(
        IReadOnlyList<CatalogPlatformBinding> platforms,
        int chunkSize)
    {
        if (platforms.Count == 0)
        {
            return [];
        }

        var sorted = CatalogSortKeyComparer.SortPlatforms(platforms);
        return ChunkArray(sorted, chunkSize);
    }

    private static void EnsureDatabase(string databasePath)
    {
        if (!File.Exists(databasePath))
        {
            CatalogSeedBootstrap.SeedBalticPatrol(databasePath, overwrite: false);
        }
    }

    private static T[][] ChunkArray<T>(IReadOnlyList<T> sorted, int chunkSize)
    {
        var chunkCount = (sorted.Count + chunkSize - 1) / chunkSize;
        var chunks = new T[chunkCount][];
        for (var i = 0; i < chunkCount; i++)
        {
            chunks[i] = sorted.Skip(i * chunkSize).Take(chunkSize).ToArray();
        }

        return chunks;
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

    private static CmoMarkdownImportResult BuildImportResult(
        int parsedCount,
        int approvedCount,
        int quarantinedCount,
        IReadOnlyList<CmoMarkdownImportBatch> batches,
        IReadOnlyList<CmoMarkdownQuarantineReportEntry> quarantineReport,
        IReadOnlyList<CmoMarkdownFittingQuarantineEntry> fittingQuarantineReport,
        CatalogBalanceDriftPipelineSettings? balanceDrift,
        IReadOnlyList<string> diffEntityIds)
    {
        var advisory = CatalogBalanceDriftPipelineEvaluator.EvaluateForDiff(balanceDrift, diffEntityIds);
        return new CmoMarkdownImportResult(
            parsedCount,
            approvedCount,
            quarantinedCount,
            batches,
            quarantineReport,
            fittingQuarantineReport,
            advisory);
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
    IReadOnlyList<CmoMarkdownImportBatch> Batches,
    IReadOnlyList<CmoMarkdownQuarantineReportEntry> QuarantineReport,
    IReadOnlyList<CmoMarkdownFittingQuarantineEntry> FittingQuarantineReport,
    BalanceDriftReport? BalanceDriftAdvisory = null);

public sealed record CmoMarkdownPlatformImportResult(
    int PlatformCount,
    int WeaponCount,
    int MountCount,
    int LoadoutCount,
    int MagazineCount,
    int FittingQuarantinedCount,
    string? PlatformBatchId,
    string? WeaponBatchId,
    string? MountBatchId,
    string? LoadoutBatchId,
    string? MagazineBatchId,
    IReadOnlyList<CmoMarkdownFittingQuarantineEntry> FittingQuarantineReport);