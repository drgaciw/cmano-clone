namespace ProjectAegis.Data.WriteGate;

using Microsoft.Data.Sqlite;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Snapshots;

/// <summary>SQLite-backed write gate for sensor catalog rows (req-06 P0).</summary>
public sealed class CatalogWriteGate : IWriteGate, IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ICatalogClock _clock;

    public CatalogWriteGate(string databasePath, ICatalogClock? clock = null)
    {
        _clock = clock ?? new FixedCatalogClock(0);
        _connection = new SqliteConnection($"Data Source={databasePath};Pooling=false");
        _connection.Open();
        EnsureSchema();
    }

    public string ProposeSensorBatch(
        IReadOnlyList<CatalogSensorBinding> proposed,
        string actorType,
        string actorId,
        string rationale = "")
    {
        if (proposed.Count == 0)
        {
            throw new ArgumentException("At least one sensor row required.", nameof(proposed));
        }

        var batchId = $"batch-{proposed.Count}-{_clock.UtcTicks}";
        var sorted = CatalogSortKeyComparer.SortSensors(proposed);

        using var tx = _connection.BeginTransaction();
        InsertBatchHeader(tx, batchId, actorType, actorId, sorted.Count, rationale, "proposed");
        foreach (var row in sorted)
        {
            InsertStagingSensor(tx, batchId, row);
        }

        tx.Commit();
        return batchId;
    }

    public string ProposeMountBatch(
        IReadOnlyList<CatalogMount> proposed,
        string actorType,
        string actorId,
        string rationale = "")
    {
        if (proposed.Count == 0)
        {
            throw new ArgumentException("At least one mount row required.", nameof(proposed));
        }

        var batchId = $"batch-mount-{proposed.Count}-{_clock.UtcTicks}";
        var sorted = CatalogSortKeyComparer.SortMounts(proposed);

        using var tx = _connection.BeginTransaction();
        InsertBatchHeader(tx, batchId, actorType, actorId, sorted.Count, rationale, "proposed");
        foreach (var row in sorted)
        {
            InsertStagingMount(tx, batchId, row);
        }

        tx.Commit();
        return batchId;
    }

    public string ProposeLoadoutBatch(
        IReadOnlyList<CatalogLoadout> proposed,
        string actorType,
        string actorId,
        string rationale = "")
    {
        if (proposed.Count == 0)
        {
            throw new ArgumentException("At least one loadout row required.", nameof(proposed));
        }

        var batchId = $"batch-loadout-{proposed.Count}-{_clock.UtcTicks}";
        var sorted = CatalogSortKeyComparer.SortLoadouts(proposed);

        using var tx = _connection.BeginTransaction();
        InsertBatchHeader(tx, batchId, actorType, actorId, sorted.Count, rationale, "proposed");
        foreach (var row in sorted)
        {
            InsertStagingLoadout(tx, batchId, row);
        }

        tx.Commit();
        return batchId;
    }

    public string ProposeMagazineBatch(
        IReadOnlyList<CatalogMagazineEntry> proposed,
        string actorType,
        string actorId,
        string rationale = "")
    {
        if (proposed.Count == 0)
        {
            throw new ArgumentException("At least one magazine row required.", nameof(proposed));
        }

        var batchId = $"batch-magazine-{proposed.Count}-{_clock.UtcTicks}";
        var sorted = CatalogSortKeyComparer.SortMagazines(proposed);

        using var tx = _connection.BeginTransaction();
        InsertBatchHeader(tx, batchId, actorType, actorId, sorted.Count, rationale, "proposed");
        foreach (var row in sorted)
        {
            InsertStagingMagazine(tx, batchId, row);
        }

        tx.Commit();
        return batchId;
    }

    public string ProposeCommsBatch(
        IReadOnlyList<CatalogCommsBinding> proposed,
        string actorType,
        string actorId,
        string rationale = "")
    {
        if (proposed.Count == 0)
        {
            throw new ArgumentException("At least one comms row required.", nameof(proposed));
        }

        var batchId = $"batch-comms-{proposed.Count}-{_clock.UtcTicks}";
        var sorted = CatalogSortKeyComparer.SortComms(proposed);

        using var tx = _connection.BeginTransaction();
        InsertBatchHeader(tx, batchId, actorType, actorId, sorted.Count, rationale, "proposed");
        foreach (var row in sorted)
        {
            InsertStagingComms(tx, batchId, row);
        }

        tx.Commit();
        return batchId;
    }

    public string ProposePlatformBatch(
        IReadOnlyList<CatalogPlatformBinding> proposed,
        string actorType,
        string actorId,
        string rationale = "")
    {
        if (proposed.Count == 0)
        {
            throw new ArgumentException("At least one platform row required.", nameof(proposed));
        }

        var batchId = $"batch-platform-{proposed.Count}-{_clock.UtcTicks}";
        var sorted = CatalogSortKeyComparer.SortPlatforms(proposed);

        using var tx = _connection.BeginTransaction();
        InsertBatchHeader(tx, batchId, actorType, actorId, sorted.Count, rationale, "proposed");
        foreach (var row in sorted)
        {
            InsertStagingPlatform(tx, batchId, row);
        }

        tx.Commit();
        return batchId;
    }

    public string ProposeWeaponBatch(
        IReadOnlyList<CatalogWeaponRecord> proposed,
        string actorType,
        string actorId,
        string rationale = "")
    {
        if (proposed.Count == 0)
        {
            throw new ArgumentException("At least one weapon row required.", nameof(proposed));
        }

        var batchId = $"batch-weapon-{proposed.Count}-{_clock.UtcTicks}";
        var sorted = CatalogSortKeyComparer.SortWeapons(proposed);

        using var tx = _connection.BeginTransaction();
        InsertBatchHeader(tx, batchId, actorType, actorId, sorted.Count, rationale, "proposed");
        foreach (var row in sorted)
        {
            InsertStagingWeapon(tx, batchId, row);
        }

        tx.Commit();
        return batchId;
    }

    public WriteGateDecision ApproveBatch(string batchId, string actorType, string actorId)
    {
        var content = LoadStagingContent(batchId);
        if (content.IsEmpty)
        {
            return new WriteGateDecision(false, batchId, ["staging_batch_not_found"]);
        }

        if (content.PopulatedTableCount > 1)
        {
            return new WriteGateDecision(false, batchId, ["ambiguous_staging_batch"]);
        }

        if (content.Sensors.Count > 0)
        {
            return ApproveSensorStaging(batchId, actorType, actorId, content.Sensors);
        }

        if (content.Platforms.Count > 0)
        {
            return ApprovePlatformStaging(batchId, actorType, actorId, content.Platforms);
        }

        if (content.Weapons.Count > 0)
        {
            return ApproveWeaponStaging(batchId, actorType, actorId, content.Weapons);
        }

        if (content.Mounts.Count > 0)
        {
            return ApproveMountStaging(batchId, actorType, actorId, content.Mounts);
        }

        if (content.Loadouts.Count > 0)
        {
            return ApproveLoadoutStaging(batchId, actorType, actorId, content.Loadouts);
        }

        if (content.Magazines.Count > 0)
        {
            return ApproveMagazineStaging(batchId, actorType, actorId, content.Magazines);
        }

        if (content.Comms.Count > 0)
        {
            return ApproveCommsStaging(batchId, actorType, actorId, content.Comms);
        }

        return new WriteGateDecision(false, batchId, ["staging_batch_not_found"]);
    }

    private WriteGateDecision ApproveSensorStaging(
        string batchId,
        string actorType,
        string actorId,
        IReadOnlyList<CatalogSensorBinding> staged)
    {
        var (approved, quarantined) = CatalogImportGate.PartitionForImport(staged);
        if (quarantined.Length > 0)
        {
            return new WriteGateDecision(
                false,
                batchId,
                quarantined.Select(q => $"quarantine:{q.Binding.PlatformId}/{q.Binding.SensorId}:{q.RejectionReason}").ToArray());
        }

        using var tx = _connection.BeginTransaction();
        foreach (var row in approved)
        {
            var previous = TryReadCurrentBasePd(tx, row.PlatformId, row.SensorId);
            UpsertSensor(tx, row, actorId);
            AppendChangeLog(
                tx,
                batchId,
                row,
                "base_pd",
                previous?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "",
                row.BasePd.ToString(System.Globalization.CultureInfo.InvariantCulture),
                actorType,
                actorId);
        }

        MarkBatchState(tx, batchId, "approved", actorType, actorId);
        tx.Commit();

        // P2-3: record stable snapshot for approved batch (enables replay binding / scenario package). Deterministic, non-fatal.
        try
        {
            using var store = new DbSnapshotStore(_connection.DataSource);
            var sensorIds = approved.Select(a => a.SensorId).OrderBy(s => s, StringComparer.Ordinal).ToList();
            var src = approved.FirstOrDefault()?.SourceFile ?? "phase2-import";
            _ = store.RecordApprovedImport(sensorIds, src, batchId);
        }
        catch
        {
            // Snapshot is auxiliary for P2; do not fail the approve commit.
        }

        return new WriteGateDecision(true, batchId, []);
    }

    private WriteGateDecision ApprovePlatformStaging(
        string batchId,
        string actorType,
        string actorId,
        IReadOnlyList<CatalogPlatformBinding> staged)
    {
        using var tx = _connection.BeginTransaction();
        EnsureSnapshotRow(tx, CatalogValidationDefaults.BalticSnapshotId);
        foreach (var row in staged)
        {
            var previous = TryReadCurrentPlatformDisplayName(tx, row.PlatformId);
            UpsertPlatform(tx, row);
            AppendEntityChangeLog(
                tx,
                batchId,
                "platform",
                row.PlatformId,
                "display_name",
                previous ?? "",
                row.DisplayName,
                actorType,
                actorId);
        }

        MarkBatchState(tx, batchId, "approved", actorType, actorId);
        tx.Commit();
        return new WriteGateDecision(true, batchId, []);
    }

    private WriteGateDecision ApproveWeaponStaging(
        string batchId,
        string actorType,
        string actorId,
        IReadOnlyList<CatalogWeaponRecord> staged)
    {
        using var tx = _connection.BeginTransaction();
        foreach (var row in staged)
        {
            var previous = TryReadCurrentWeaponDisplayName(tx, row.WeaponId);
            UpsertWeapon(tx, row);
            AppendEntityChangeLog(
                tx,
                batchId,
                "weapon_catalog",
                row.WeaponId,
                "display_name",
                previous ?? "",
                row.DisplayName,
                actorType,
                actorId);
        }

        MarkBatchState(tx, batchId, "approved", actorType, actorId);
        tx.Commit();
        return new WriteGateDecision(true, batchId, []);
    }

    private WriteGateDecision ApproveMountStaging(
        string batchId,
        string actorType,
        string actorId,
        IReadOnlyList<CatalogMount> staged)
    {
        using var tx = _connection.BeginTransaction();
        foreach (var row in staged)
        {
            UpsertMount(tx, row);
            AppendEntityChangeLog(
                tx,
                batchId,
                "platform_mount",
                $"{row.PlatformId}/{row.MountId}",
                "mount_type",
                "",
                row.MountType,
                actorType,
                actorId);
        }

        MarkBatchState(tx, batchId, "approved", actorType, actorId);
        tx.Commit();
        return new WriteGateDecision(true, batchId, []);
    }

    private WriteGateDecision ApproveLoadoutStaging(
        string batchId,
        string actorType,
        string actorId,
        IReadOnlyList<CatalogLoadout> staged)
    {
        using var tx = _connection.BeginTransaction();
        foreach (var row in staged)
        {
            UpsertLoadout(tx, row);
            AppendEntityChangeLog(
                tx,
                batchId,
                "platform_loadout",
                $"{row.PlatformId}/{row.LoadoutId}",
                "loadout_name",
                "",
                row.LoadoutName,
                actorType,
                actorId);
        }

        MarkBatchState(tx, batchId, "approved", actorType, actorId);
        tx.Commit();
        return new WriteGateDecision(true, batchId, []);
    }

    private WriteGateDecision ApproveMagazineStaging(
        string batchId,
        string actorType,
        string actorId,
        IReadOnlyList<CatalogMagazineEntry> staged)
    {
        using var tx = _connection.BeginTransaction();
        foreach (var row in staged)
        {
            var previous = TryReadCurrentMagazineQuantity(tx, row);
            UpsertMagazine(tx, row);
            AppendEntityChangeLog(
                tx,
                batchId,
                "platform_magazine",
                $"{row.PlatformId}/{row.LoadoutId}/{row.MountId}/{row.WeaponId}",
                "quantity",
                previous?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "",
                row.Quantity.ToString(System.Globalization.CultureInfo.InvariantCulture),
                actorType,
                actorId);
        }

        MarkBatchState(tx, batchId, "approved", actorType, actorId);
        tx.Commit();
        return new WriteGateDecision(true, batchId, []);
    }

    private WriteGateDecision ApproveCommsStaging(
        string batchId,
        string actorType,
        string actorId,
        IReadOnlyList<CatalogCommsBinding> staged)
    {
        using var tx = _connection.BeginTransaction();
        foreach (var row in staged)
        {
            UpsertComms(tx, row);
            AppendEntityChangeLog(
                tx,
                batchId,
                "platform_comms",
                $"{row.PlatformId}/{row.LinkId}",
                "role",
                "",
                row.Role,
                actorType,
                actorId);
        }

        MarkBatchState(tx, batchId, "approved", actorType, actorId);
        tx.Commit();
        return new WriteGateDecision(true, batchId, []);
    }

    public WriteGateDecision RejectBatch(string batchId, string actorType, string actorId, string rationale = "")
    {
        using var tx = _connection.BeginTransaction();
        if (!BatchExists(tx, batchId))
        {
            return new WriteGateDecision(false, batchId, ["staging_batch_not_found"]);
        }

        MarkBatchState(tx, batchId, "rejected", actorType, actorId, rationale);
        DeleteStagingRows(tx, batchId);
        tx.Commit();
        return new WriteGateDecision(false, batchId, []);
    }

    public IReadOnlyList<CatalogStagingBatchSummary> ListPendingBatches()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText =
            """
            SELECT batch_id, actor_type, actor_id, record_count, approval_state, proposed_utc_ticks
            FROM catalog_staging_batch
            WHERE approval_state = 'proposed'
            ORDER BY proposed_utc_ticks ASC, batch_id ASC
            """;
        using var reader = cmd.ExecuteReader();
        var list = new List<CatalogStagingBatchSummary>();
        while (reader.Read())
        {
            list.Add(new CatalogStagingBatchSummary(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetInt32(3),
                reader.GetString(4),
                reader.GetInt64(5)));
        }

        return list;
    }

    public void Dispose()
    {
        _connection.Close();
        _connection.Dispose();
        SqliteConnection.ClearAllPools();
    }

    private void EnsureSchema()
    {
        using var bootstrap = new SqliteCatalogReader(_connection.DataSource, "write-gate-bootstrap");
    }

    private void InsertBatchHeader(
        SqliteTransaction tx,
        string batchId,
        string actorType,
        string actorId,
        int recordCount,
        string rationale,
        string approvalState)
    {
        using var cmd = tx.Connection!.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText =
            """
            INSERT OR REPLACE INTO catalog_staging_batch
                (batch_id, actor_type, actor_id, proposed_utc_ticks, approval_state, record_count, rationale)
            VALUES ($id, $actorType, $actorId, $ticks, $state, $count, $rationale)
            """;
        cmd.Parameters.AddWithValue("$id", batchId);
        cmd.Parameters.AddWithValue("$actorType", actorType);
        cmd.Parameters.AddWithValue("$actorId", actorId);
        cmd.Parameters.AddWithValue("$ticks", _clock.UtcTicks);
        cmd.Parameters.AddWithValue("$state", approvalState);
        cmd.Parameters.AddWithValue("$count", recordCount);
        cmd.Parameters.AddWithValue("$rationale", rationale);
        cmd.ExecuteNonQuery();
    }

    private static void InsertStagingSensor(SqliteTransaction tx, string batchId, CatalogSensorBinding row)
    {
        using var cmd = tx.Connection!.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText =
            """
            INSERT OR REPLACE INTO catalog_staging_sensor
                (batch_id, platform_id, sensor_id, base_pd, source_fact_id, confidence,
                 import_batch_id, source_file, review_state, trl_level, value_tier, reviewer_id, citation_ref)
            VALUES ($batch, $platform, $sensor, $basePd, $source, $confidence, $importBatch, $sourceFile,
                    $review, $trl, $tier, $reviewer, $citation)
            """;
        BindSensorParameters(cmd, batchId, row);
        cmd.ExecuteNonQuery();
    }

    private static void InsertStagingMount(SqliteTransaction tx, string batchId, CatalogMount row)
    {
        using var cmd = tx.Connection!.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText =
            """
            INSERT OR REPLACE INTO catalog_staging_mount
                (batch_id, platform_id, mount_id, mount_type, arc_deg, capacity, review_state)
            VALUES ($batch, $platform, $mount, $type, $arc, $capacity, $review)
            """;
        cmd.Parameters.AddWithValue("$batch", batchId);
        cmd.Parameters.AddWithValue("$platform", row.PlatformId);
        cmd.Parameters.AddWithValue("$mount", row.MountId);
        cmd.Parameters.AddWithValue("$type", row.MountType);
        cmd.Parameters.AddWithValue("$arc", row.ArcDeg);
        cmd.Parameters.AddWithValue("$capacity", row.Capacity);
        cmd.Parameters.AddWithValue("$review", row.ReviewState);
        cmd.ExecuteNonQuery();
    }

    private static void InsertStagingLoadout(SqliteTransaction tx, string batchId, CatalogLoadout row)
    {
        using var cmd = tx.Connection!.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText =
            """
            INSERT OR REPLACE INTO catalog_staging_loadout
                (batch_id, platform_id, loadout_id, loadout_name, role, is_default)
            VALUES ($batch, $platform, $loadout, $name, $role, $default)
            """;
        cmd.Parameters.AddWithValue("$batch", batchId);
        cmd.Parameters.AddWithValue("$platform", row.PlatformId);
        cmd.Parameters.AddWithValue("$loadout", row.LoadoutId);
        cmd.Parameters.AddWithValue("$name", row.LoadoutName);
        cmd.Parameters.AddWithValue("$role", row.Role);
        cmd.Parameters.AddWithValue("$default", row.IsDefault ? 1 : 0);
        cmd.ExecuteNonQuery();
    }

    private static void InsertStagingMagazine(SqliteTransaction tx, string batchId, CatalogMagazineEntry row)
    {
        using var cmd = tx.Connection!.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText =
            """
            INSERT OR REPLACE INTO catalog_staging_magazine
                (batch_id, platform_id, loadout_id, mount_id, weapon_id, quantity, reload_time_sec, depth)
            VALUES ($batch, $platform, $loadout, $mount, $weapon, $qty, $reload, $depth)
            """;
        cmd.Parameters.AddWithValue("$batch", batchId);
        cmd.Parameters.AddWithValue("$platform", row.PlatformId);
        cmd.Parameters.AddWithValue("$loadout", row.LoadoutId);
        cmd.Parameters.AddWithValue("$mount", row.MountId);
        cmd.Parameters.AddWithValue("$weapon", row.WeaponId);
        cmd.Parameters.AddWithValue("$qty", row.Quantity);
        cmd.Parameters.AddWithValue("$reload", row.ReloadTimeSec);
        cmd.Parameters.AddWithValue("$depth", row.Depth);
        cmd.ExecuteNonQuery();
    }

    private static void InsertStagingComms(SqliteTransaction tx, string batchId, CatalogCommsBinding row)
    {
        using var cmd = tx.Connection!.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText =
            """
            INSERT OR REPLACE INTO catalog_staging_comms
                (batch_id, platform_id, link_id, role, satcom_capable, review_state, trl_level, value_tier, citation_ref)
            VALUES ($batch, $platform, $link, $role, $satcom, $review, $trl, $tier, $citation)
            """;
        cmd.Parameters.AddWithValue("$batch", batchId);
        cmd.Parameters.AddWithValue("$platform", row.PlatformId);
        cmd.Parameters.AddWithValue("$link", row.LinkId);
        cmd.Parameters.AddWithValue("$role", row.Role);
        cmd.Parameters.AddWithValue("$satcom", row.SatcomCapable ? 1 : 0);
        cmd.Parameters.AddWithValue("$review", row.ReviewState);
        cmd.Parameters.AddWithValue("$trl", row.TrlLevel);
        cmd.Parameters.AddWithValue("$tier", row.ValueTier);
        cmd.Parameters.AddWithValue("$citation", row.CitationRef);
        cmd.ExecuteNonQuery();
    }

    private static void InsertStagingPlatform(SqliteTransaction tx, string batchId, CatalogPlatformBinding row)
    {
        using var cmd = tx.Connection!.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText =
            """
            INSERT OR REPLACE INTO catalog_staging_platform
                (batch_id, platform_id, display_name, domain, platform_class, nationality,
                 game_technology_level, review_state, trl_level, value_tier, citation_ref)
            VALUES ($batch, $platform, $name, $domain, $class, $nat, $gtl, $review, $trl, $tier, $citation)
            """;
        cmd.Parameters.AddWithValue("$batch", batchId);
        cmd.Parameters.AddWithValue("$platform", row.PlatformId);
        cmd.Parameters.AddWithValue("$name", row.DisplayName);
        cmd.Parameters.AddWithValue("$domain", row.Domain);
        cmd.Parameters.AddWithValue("$class", row.PlatformClass);
        cmd.Parameters.AddWithValue("$nat", row.Nationality);
        cmd.Parameters.AddWithValue("$gtl", row.GameTechnologyLevel);
        cmd.Parameters.AddWithValue("$review", row.ReviewState);
        cmd.Parameters.AddWithValue("$trl", row.TrlLevel);
        cmd.Parameters.AddWithValue("$tier", CatalogProvenanceTier.Normalize(row.ValueTier));
        cmd.Parameters.AddWithValue("$citation", row.CitationRef);
        cmd.ExecuteNonQuery();
    }

    private static void InsertStagingWeapon(SqliteTransaction tx, string batchId, CatalogWeaponRecord row)
    {
        using var cmd = tx.Connection!.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText =
            """
            INSERT OR REPLACE INTO catalog_staging_weapon
                (batch_id, weapon_id, display_name, min_range_meters, max_range_meters, weapon_type, guidance, review_state)
            VALUES ($batch, $weapon, $name, $min, $max, $type, $guidance, $review)
            """;
        cmd.Parameters.AddWithValue("$batch", batchId);
        cmd.Parameters.AddWithValue("$weapon", row.WeaponId);
        cmd.Parameters.AddWithValue("$name", row.DisplayName);
        cmd.Parameters.AddWithValue("$min", row.MinRangeMeters);
        cmd.Parameters.AddWithValue("$max", row.MaxRangeMeters);
        cmd.Parameters.AddWithValue("$type", row.WeaponType);
        cmd.Parameters.AddWithValue("$guidance", row.Guidance);
        cmd.Parameters.AddWithValue("$review", row.ReviewState);
        cmd.ExecuteNonQuery();
    }

    private static void UpsertSensor(SqliteTransaction tx, CatalogSensorBinding row, string actorId)
    {
        using var cmd = tx.Connection!.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText =
            """
            INSERT OR REPLACE INTO sensor
                (platform_id, sensor_id, base_pd, source_fact_id, confidence, import_batch_id, source_file,
                 review_state, trl_level, value_tier, reviewer_id, revised_utc_ticks, citation_ref)
            VALUES ($platform, $sensor, $basePd, $source, $confidence, $importBatch, $sourceFile,
                    $review, $trl, $tier, $reviewer, $revised, $citation)
            """;
        cmd.Parameters.AddWithValue("$platform", row.PlatformId);
        cmd.Parameters.AddWithValue("$sensor", row.SensorId);
        cmd.Parameters.AddWithValue("$basePd", row.BasePd);
        cmd.Parameters.AddWithValue("$source", row.SourceFactId);
        cmd.Parameters.AddWithValue("$confidence", row.Confidence);
        cmd.Parameters.AddWithValue("$importBatch", row.ImportBatchId);
        cmd.Parameters.AddWithValue("$sourceFile", row.SourceFile);
        cmd.Parameters.AddWithValue("$review", row.ReviewState);
        cmd.Parameters.AddWithValue("$trl", row.TrlLevel);
        cmd.Parameters.AddWithValue("$tier", CatalogProvenanceTier.Normalize(row.ValueTier));
        cmd.Parameters.AddWithValue("$reviewer", string.IsNullOrEmpty(row.ReviewerId) ? actorId : row.ReviewerId);
        cmd.Parameters.AddWithValue("$revised", row.RevisedUtcTicks);
        cmd.Parameters.AddWithValue("$citation", row.CitationRef);
        cmd.ExecuteNonQuery();
    }

    private static void BindSensorParameters(SqliteCommand cmd, string batchId, CatalogSensorBinding row)
    {
        cmd.Parameters.AddWithValue("$batch", batchId);
        cmd.Parameters.AddWithValue("$platform", row.PlatformId);
        cmd.Parameters.AddWithValue("$sensor", row.SensorId);
        cmd.Parameters.AddWithValue("$basePd", row.BasePd);
        cmd.Parameters.AddWithValue("$source", row.SourceFactId);
        cmd.Parameters.AddWithValue("$confidence", row.Confidence);
        cmd.Parameters.AddWithValue("$importBatch", row.ImportBatchId);
        cmd.Parameters.AddWithValue("$sourceFile", row.SourceFile);
        cmd.Parameters.AddWithValue("$review", row.ReviewState);
        cmd.Parameters.AddWithValue("$trl", row.TrlLevel);
        cmd.Parameters.AddWithValue("$tier", CatalogProvenanceTier.Normalize(row.ValueTier));
        cmd.Parameters.AddWithValue("$reviewer", row.ReviewerId);
        cmd.Parameters.AddWithValue("$citation", row.CitationRef);
    }

    private static double? TryReadCurrentBasePd(SqliteTransaction tx, string platformId, string sensorId)
    {
        using var cmd = tx.Connection!.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText =
            "SELECT base_pd FROM sensor WHERE platform_id = $platform AND sensor_id = $sensor";
        cmd.Parameters.AddWithValue("$platform", platformId);
        cmd.Parameters.AddWithValue("$sensor", sensorId);
        var scalar = cmd.ExecuteScalar();
        return scalar == null || scalar is DBNull ? null : Convert.ToDouble(scalar, System.Globalization.CultureInfo.InvariantCulture);
    }

    private void AppendChangeLog(
        SqliteTransaction tx,
        string batchId,
        CatalogSensorBinding row,
        string fieldName,
        string previousValue,
        string newValue,
        string actorType,
        string actorId)
    {
        AppendEntityChangeLog(
            tx,
            batchId,
            "sensor",
            $"{row.PlatformId}/{row.SensorId}",
            fieldName,
            previousValue,
            newValue,
            actorType,
            actorId);
    }

    private void AppendEntityChangeLog(
        SqliteTransaction tx,
        string batchId,
        string tableName,
        string entityKey,
        string fieldName,
        string previousValue,
        string newValue,
        string actorType,
        string actorId)
    {
        using var cmd = tx.Connection!.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText =
            """
            INSERT INTO catalog_change_log
                (batch_id, table_name, entity_key, field_name, previous_value, new_value,
                 actor_type, actor_id, rationale, approval_state, revised_utc_ticks, release_version)
            VALUES ($batch, $table, $entityKey, $field, $prev, $next, $actorType, $actorId, '', 'approved', $ticks, '')
            """;
        cmd.Parameters.AddWithValue("$batch", batchId);
        cmd.Parameters.AddWithValue("$table", tableName);
        cmd.Parameters.AddWithValue("$entityKey", entityKey);
        cmd.Parameters.AddWithValue("$field", fieldName);
        cmd.Parameters.AddWithValue("$prev", previousValue);
        cmd.Parameters.AddWithValue("$next", newValue);
        cmd.Parameters.AddWithValue("$actorType", actorType);
        cmd.Parameters.AddWithValue("$actorId", actorId);
        cmd.Parameters.AddWithValue("$ticks", _clock.UtcTicks);
        cmd.ExecuteNonQuery();
    }

    private static void MarkBatchState(
        SqliteTransaction tx,
        string batchId,
        string state,
        string actorType,
        string actorId,
        string rationale = "")
    {
        using var cmd = tx.Connection!.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText =
            """
            UPDATE catalog_staging_batch
            SET approval_state = $state, actor_type = $actorType, actor_id = $actorId, rationale = $rationale
            WHERE batch_id = $id
            """;
        cmd.Parameters.AddWithValue("$state", state);
        cmd.Parameters.AddWithValue("$actorType", actorType);
        cmd.Parameters.AddWithValue("$actorId", actorId);
        cmd.Parameters.AddWithValue("$rationale", rationale);
        cmd.Parameters.AddWithValue("$id", batchId);
        cmd.ExecuteNonQuery();
    }

    private static void DeleteStagingRows(SqliteTransaction tx, string batchId)
    {
        // S22-04 / DBI-1.4: purge all staging tables so RejectBatch never leaves orphan rows.
        using var cmd = tx.Connection!.CreateCommand();
        cmd.Transaction = tx;
        foreach (var table in new[]
        {
            "catalog_staging_sensor",
            "catalog_staging_platform",
            "catalog_staging_weapon",
            "catalog_staging_mount",
            "catalog_staging_loadout",
            "catalog_staging_magazine",
            "catalog_staging_comms",
        })
        {
            cmd.CommandText = $"DELETE FROM {table} WHERE batch_id = $id";
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("$id", batchId);
            cmd.ExecuteNonQuery();
        }
    }

    private static bool BatchExists(SqliteTransaction tx, string batchId)
    {
        using var cmd = tx.Connection!.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = "SELECT COUNT(*) FROM catalog_staging_batch WHERE batch_id = $id";
        cmd.Parameters.AddWithValue("$id", batchId);
        return Convert.ToInt32(cmd.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture) > 0;
    }

    private StagingBatchContent LoadStagingContent(string batchId)
    {
        var content = new StagingBatchContent();
        content.Sensors.AddRange(LoadStagingSensorRows(batchId));
        content.Platforms.AddRange(LoadStagingPlatformRows(batchId));
        content.Weapons.AddRange(LoadStagingWeaponRows(batchId));
        content.Mounts.AddRange(LoadStagingMountRows(batchId));
        content.Loadouts.AddRange(LoadStagingLoadoutRows(batchId));
        content.Magazines.AddRange(LoadStagingMagazineRows(batchId));
        content.Comms.AddRange(LoadStagingCommsRows(batchId));
        return content;
    }

    private List<CatalogSensorBinding> LoadStagingSensorRows(string batchId)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText =
            """
            SELECT platform_id, sensor_id, base_pd, source_fact_id, confidence, import_batch_id, source_file,
                   review_state, trl_level, value_tier, reviewer_id, citation_ref
            FROM catalog_staging_sensor
            WHERE batch_id = $batch
            ORDER BY platform_id ASC, sensor_id ASC
            """;
        cmd.Parameters.AddWithValue("$batch", batchId);
        using var reader = cmd.ExecuteReader();
        var list = new List<CatalogSensorBinding>();
        while (reader.Read())
        {
            list.Add(new CatalogSensorBinding(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetDouble(2),
                reader.GetString(3),
                reader.GetDouble(4),
                reader.GetString(5),
                reader.GetString(6),
                reader.GetString(7),
                reader.GetInt32(8),
                reader.GetString(9),
                reader.GetString(10),
                _clock.UtcTicks,
                reader.GetString(11)));
        }

        return list;
    }

    private List<CatalogPlatformBinding> LoadStagingPlatformRows(string batchId)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText =
            """
            SELECT platform_id, display_name, domain, platform_class, nationality, game_technology_level,
                   review_state, trl_level, value_tier, citation_ref
            FROM catalog_staging_platform
            WHERE batch_id = $batch
            ORDER BY platform_id ASC
            """;
        cmd.Parameters.AddWithValue("$batch", batchId);
        using var reader = cmd.ExecuteReader();
        var list = new List<CatalogPlatformBinding>();
        while (reader.Read())
        {
            list.Add(new CatalogPlatformBinding(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetString(4),
                reader.GetInt32(5),
                reader.GetString(6),
                reader.GetInt32(7),
                reader.GetString(8),
                reader.GetString(9)));
        }

        return list;
    }

    private List<CatalogWeaponRecord> LoadStagingWeaponRows(string batchId)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText =
            """
            SELECT weapon_id, display_name, min_range_meters, max_range_meters, weapon_type, guidance, review_state
            FROM catalog_staging_weapon
            WHERE batch_id = $batch
            ORDER BY weapon_id ASC
            """;
        cmd.Parameters.AddWithValue("$batch", batchId);
        using var reader = cmd.ExecuteReader();
        var list = new List<CatalogWeaponRecord>();
        while (reader.Read())
        {
            list.Add(new CatalogWeaponRecord(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetDouble(2),
                reader.GetDouble(3),
                reader.GetString(4),
                reader.GetString(5),
                reader.GetString(6)));
        }

        return list;
    }

    private List<CatalogMount> LoadStagingMountRows(string batchId)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText =
            """
            SELECT platform_id, mount_id, mount_type, arc_deg, capacity, review_state
            FROM catalog_staging_mount
            WHERE batch_id = $batch
            ORDER BY platform_id ASC, mount_id ASC
            """;
        cmd.Parameters.AddWithValue("$batch", batchId);
        using var reader = cmd.ExecuteReader();
        var list = new List<CatalogMount>();
        while (reader.Read())
        {
            list.Add(new CatalogMount(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetDouble(3),
                reader.GetInt32(4),
                reader.GetString(5)));
        }

        return list;
    }

    private List<CatalogLoadout> LoadStagingLoadoutRows(string batchId)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText =
            """
            SELECT platform_id, loadout_id, loadout_name, role, is_default
            FROM catalog_staging_loadout
            WHERE batch_id = $batch
            ORDER BY platform_id ASC, loadout_id ASC
            """;
        cmd.Parameters.AddWithValue("$batch", batchId);
        using var reader = cmd.ExecuteReader();
        var list = new List<CatalogLoadout>();
        while (reader.Read())
        {
            list.Add(new CatalogLoadout(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetInt32(4) == 1));
        }

        return list;
    }

    private List<CatalogMagazineEntry> LoadStagingMagazineRows(string batchId)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText =
            """
            SELECT platform_id, loadout_id, mount_id, weapon_id, quantity, reload_time_sec, depth
            FROM catalog_staging_magazine
            WHERE batch_id = $batch
            ORDER BY platform_id ASC, loadout_id ASC, mount_id ASC, weapon_id ASC
            """;
        cmd.Parameters.AddWithValue("$batch", batchId);
        using var reader = cmd.ExecuteReader();
        var list = new List<CatalogMagazineEntry>();
        while (reader.Read())
        {
            list.Add(new CatalogMagazineEntry(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetInt32(4),
                reader.GetInt32(5),
                reader.GetInt32(6)));
        }

        return list;
    }

    private List<CatalogCommsBinding> LoadStagingCommsRows(string batchId)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText =
            """
            SELECT platform_id, link_id, role, satcom_capable, review_state, trl_level, value_tier, citation_ref
            FROM catalog_staging_comms
            WHERE batch_id = $batch
            ORDER BY platform_id ASC, link_id ASC
            """;
        cmd.Parameters.AddWithValue("$batch", batchId);
        using var reader = cmd.ExecuteReader();
        var list = new List<CatalogCommsBinding>();
        while (reader.Read())
        {
            list.Add(new CatalogCommsBinding(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetInt32(3) == 1,
                reader.GetString(4),
                reader.GetInt32(5),
                reader.GetString(6),
                reader.GetString(7)));
        }

        return list;
    }

    private static void UpsertPlatform(SqliteTransaction tx, CatalogPlatformBinding row)
    {
        var existing = TryReadCurrentPlatformRow(tx, row.PlatformId);
        var snapshotId = existing?.SnapshotId ?? CatalogValidationDefaults.BalticSnapshotId;
        var lat = existing?.LatDeg ?? 0;
        var lon = existing?.LonDeg ?? 0;
        var radius = existing?.CombatRadiusNm ?? 1.0;

        using var cmd = tx.Connection!.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText =
            """
            INSERT OR REPLACE INTO platform
                (platform_id, snapshot_id, lat_deg, lon_deg, combat_radius_nm,
                 display_name, domain, platform_class, nationality, game_technology_level)
            VALUES ($platform, $snapshot, $lat, $lon, $radius, $name, $domain, $class, $nat, $gtl)
            """;
        cmd.Parameters.AddWithValue("$platform", row.PlatformId);
        cmd.Parameters.AddWithValue("$snapshot", snapshotId);
        cmd.Parameters.AddWithValue("$lat", lat);
        cmd.Parameters.AddWithValue("$lon", lon);
        cmd.Parameters.AddWithValue("$radius", radius);
        cmd.Parameters.AddWithValue("$name", row.DisplayName);
        cmd.Parameters.AddWithValue("$domain", row.Domain);
        cmd.Parameters.AddWithValue("$class", row.PlatformClass);
        cmd.Parameters.AddWithValue("$nat", row.Nationality);
        cmd.Parameters.AddWithValue("$gtl", row.GameTechnologyLevel);
        cmd.ExecuteNonQuery();
    }

    private static void UpsertWeapon(SqliteTransaction tx, CatalogWeaponRecord row)
    {
        using var cmd = tx.Connection!.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText =
            """
            INSERT OR REPLACE INTO weapon_catalog
                (weapon_id, display_name, min_range_meters, max_range_meters, weapon_type, guidance)
            VALUES ($weapon, $name, $min, $max, $type, $guidance)
            """;
        cmd.Parameters.AddWithValue("$weapon", row.WeaponId);
        cmd.Parameters.AddWithValue("$name", row.DisplayName);
        cmd.Parameters.AddWithValue("$min", row.MinRangeMeters);
        cmd.Parameters.AddWithValue("$max", row.MaxRangeMeters);
        cmd.Parameters.AddWithValue("$type", row.WeaponType);
        cmd.Parameters.AddWithValue("$guidance", row.Guidance);
        cmd.ExecuteNonQuery();
    }

    private static void UpsertMount(SqliteTransaction tx, CatalogMount row)
    {
        using var cmd = tx.Connection!.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText =
            """
            INSERT OR REPLACE INTO platform_mount
                (platform_id, mount_id, mount_type, arc_deg, capacity, review_state)
            VALUES ($platform, $mount, $type, $arc, $capacity, $review)
            """;
        cmd.Parameters.AddWithValue("$platform", row.PlatformId);
        cmd.Parameters.AddWithValue("$mount", row.MountId);
        cmd.Parameters.AddWithValue("$type", row.MountType);
        cmd.Parameters.AddWithValue("$arc", row.ArcDeg);
        cmd.Parameters.AddWithValue("$capacity", row.Capacity);
        cmd.Parameters.AddWithValue("$review", row.ReviewState);
        cmd.ExecuteNonQuery();
    }

    private static void UpsertLoadout(SqliteTransaction tx, CatalogLoadout row)
    {
        using var cmd = tx.Connection!.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText =
            """
            INSERT OR REPLACE INTO platform_loadout
                (platform_id, loadout_id, loadout_name, role, is_default)
            VALUES ($platform, $loadout, $name, $role, $default)
            """;
        cmd.Parameters.AddWithValue("$platform", row.PlatformId);
        cmd.Parameters.AddWithValue("$loadout", row.LoadoutId);
        cmd.Parameters.AddWithValue("$name", row.LoadoutName);
        cmd.Parameters.AddWithValue("$role", row.Role);
        cmd.Parameters.AddWithValue("$default", row.IsDefault ? 1 : 0);
        cmd.ExecuteNonQuery();
    }

    private static void UpsertMagazine(SqliteTransaction tx, CatalogMagazineEntry row)
    {
        using var cmd = tx.Connection!.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText =
            """
            INSERT OR REPLACE INTO platform_magazine
                (platform_id, loadout_id, mount_id, weapon_id, quantity, reload_time_sec, depth)
            VALUES ($platform, $loadout, $mount, $weapon, $qty, $reload, $depth)
            """;
        cmd.Parameters.AddWithValue("$platform", row.PlatformId);
        cmd.Parameters.AddWithValue("$loadout", row.LoadoutId);
        cmd.Parameters.AddWithValue("$mount", row.MountId);
        cmd.Parameters.AddWithValue("$weapon", row.WeaponId);
        cmd.Parameters.AddWithValue("$qty", row.Quantity);
        cmd.Parameters.AddWithValue("$reload", row.ReloadTimeSec);
        cmd.Parameters.AddWithValue("$depth", row.Depth);
        cmd.ExecuteNonQuery();
    }

    private static void UpsertComms(SqliteTransaction tx, CatalogCommsBinding row)
    {
        using var cmd = tx.Connection!.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText =
            """
            INSERT OR REPLACE INTO platform_comms
                (platform_id, link_id, role, satcom_capable, review_state, trl_level, value_tier, citation_ref)
            VALUES ($platform, $link, $role, $satcom, $review, $trl, $tier, $citation)
            """;
        cmd.Parameters.AddWithValue("$platform", row.PlatformId);
        cmd.Parameters.AddWithValue("$link", row.LinkId);
        cmd.Parameters.AddWithValue("$role", row.Role);
        cmd.Parameters.AddWithValue("$satcom", row.SatcomCapable ? 1 : 0);
        cmd.Parameters.AddWithValue("$review", row.ReviewState);
        cmd.Parameters.AddWithValue("$trl", row.TrlLevel);
        cmd.Parameters.AddWithValue("$tier", CatalogProvenanceTier.Normalize(row.ValueTier));
        cmd.Parameters.AddWithValue("$citation", row.CitationRef);
        cmd.ExecuteNonQuery();
    }

    private static void EnsureSnapshotRow(SqliteTransaction tx, string snapshotId)
    {
        using var cmd = tx.Connection!.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = "INSERT OR IGNORE INTO catalog_snapshot (snapshot_id) VALUES ($id)";
        cmd.Parameters.AddWithValue("$id", snapshotId);
        cmd.ExecuteNonQuery();
    }

    private static PlatformRowSnapshot? TryReadCurrentPlatformRow(SqliteTransaction tx, string platformId)
    {
        using var cmd = tx.Connection!.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText =
            """
            SELECT snapshot_id, lat_deg, lon_deg, combat_radius_nm
            FROM platform
            WHERE platform_id = $platform
            ORDER BY snapshot_id ASC
            LIMIT 1
            """;
        cmd.Parameters.AddWithValue("$platform", platformId);
        using var reader = cmd.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }

        return new PlatformRowSnapshot(
            reader.GetString(0),
            reader.GetDouble(1),
            reader.GetDouble(2),
            reader.GetDouble(3));
    }

    private static string? TryReadCurrentPlatformDisplayName(SqliteTransaction tx, string platformId)
    {
        using var cmd = tx.Connection!.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText =
            """
            SELECT display_name
            FROM platform
            WHERE platform_id = $platform
            ORDER BY snapshot_id ASC
            LIMIT 1
            """;
        cmd.Parameters.AddWithValue("$platform", platformId);
        var scalar = cmd.ExecuteScalar();
        return scalar == null || scalar is DBNull ? null : Convert.ToString(scalar, System.Globalization.CultureInfo.InvariantCulture);
    }

    private static string? TryReadCurrentWeaponDisplayName(SqliteTransaction tx, string weaponId)
    {
        using var cmd = tx.Connection!.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = "SELECT display_name FROM weapon_catalog WHERE weapon_id = $weapon";
        cmd.Parameters.AddWithValue("$weapon", weaponId);
        var scalar = cmd.ExecuteScalar();
        return scalar == null || scalar is DBNull ? null : Convert.ToString(scalar, System.Globalization.CultureInfo.InvariantCulture);
    }

    private static int? TryReadCurrentMagazineQuantity(SqliteTransaction tx, CatalogMagazineEntry row)
    {
        using var cmd = tx.Connection!.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText =
            """
            SELECT quantity
            FROM platform_magazine
            WHERE platform_id = $platform AND loadout_id = $loadout AND mount_id = $mount AND weapon_id = $weapon
            """;
        cmd.Parameters.AddWithValue("$platform", row.PlatformId);
        cmd.Parameters.AddWithValue("$loadout", row.LoadoutId);
        cmd.Parameters.AddWithValue("$mount", row.MountId);
        cmd.Parameters.AddWithValue("$weapon", row.WeaponId);
        var scalar = cmd.ExecuteScalar();
        return scalar == null || scalar is DBNull
            ? null
            : Convert.ToInt32(scalar, System.Globalization.CultureInfo.InvariantCulture);
    }

    private sealed class StagingBatchContent
    {
        public List<CatalogSensorBinding> Sensors { get; } = [];
        public List<CatalogPlatformBinding> Platforms { get; } = [];
        public List<CatalogWeaponRecord> Weapons { get; } = [];
        public List<CatalogMount> Mounts { get; } = [];
        public List<CatalogLoadout> Loadouts { get; } = [];
        public List<CatalogMagazineEntry> Magazines { get; } = [];
        public List<CatalogCommsBinding> Comms { get; } = [];

        public bool IsEmpty =>
            Sensors.Count == 0 &&
            Platforms.Count == 0 &&
            Weapons.Count == 0 &&
            Mounts.Count == 0 &&
            Loadouts.Count == 0 &&
            Magazines.Count == 0 &&
            Comms.Count == 0;

        public int PopulatedTableCount =>
            (Sensors.Count > 0 ? 1 : 0) +
            (Platforms.Count > 0 ? 1 : 0) +
            (Weapons.Count > 0 ? 1 : 0) +
            (Mounts.Count > 0 ? 1 : 0) +
            (Loadouts.Count > 0 ? 1 : 0) +
            (Magazines.Count > 0 ? 1 : 0) +
            (Comms.Count > 0 ? 1 : 0);
    }

    private sealed record PlatformRowSnapshot(
        string SnapshotId,
        double LatDeg,
        double LonDeg,
        double CombatRadiusNm);
}