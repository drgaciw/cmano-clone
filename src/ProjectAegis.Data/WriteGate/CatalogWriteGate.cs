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
        var sorted = proposed
            .OrderBy(b => b.PlatformId, StringComparer.Ordinal)
            .ThenBy(b => b.SensorId, StringComparer.Ordinal)
            .ToArray();

        using var tx = _connection.BeginTransaction();
        InsertBatchHeader(tx, batchId, actorType, actorId, sorted.Length, rationale, "proposed");
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
        var sorted = proposed
            .OrderBy(m => m.PlatformId, StringComparer.Ordinal)
            .ThenBy(m => m.MountId, StringComparer.Ordinal)
            .ToArray();

        using var tx = _connection.BeginTransaction();
        InsertBatchHeader(tx, batchId, actorType, actorId, sorted.Length, rationale, "proposed");
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
        var sorted = proposed
            .OrderBy(l => l.PlatformId, StringComparer.Ordinal)
            .ThenBy(l => l.LoadoutId, StringComparer.Ordinal)
            .ToArray();

        using var tx = _connection.BeginTransaction();
        InsertBatchHeader(tx, batchId, actorType, actorId, sorted.Length, rationale, "proposed");
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
        var sorted = proposed
            .OrderBy(m => m.PlatformId, StringComparer.Ordinal)
            .ThenBy(m => m.LoadoutId, StringComparer.Ordinal)
            .ThenBy(m => m.MountId, StringComparer.Ordinal)
            .ThenBy(m => m.WeaponId, StringComparer.Ordinal)
            .ToArray();

        using var tx = _connection.BeginTransaction();
        InsertBatchHeader(tx, batchId, actorType, actorId, sorted.Length, rationale, "proposed");
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
        var sorted = proposed
            .OrderBy(c => c.PlatformId, StringComparer.Ordinal)
            .ThenBy(c => c.LinkId, StringComparer.Ordinal)
            .ToArray();

        using var tx = _connection.BeginTransaction();
        InsertBatchHeader(tx, batchId, actorType, actorId, sorted.Length, rationale, "proposed");
        foreach (var row in sorted)
        {
            InsertStagingComms(tx, batchId, row);
        }

        tx.Commit();
        return batchId;
    }

    public WriteGateDecision ApproveBatch(string batchId, string actorType, string actorId)
    {
        var staged = LoadStagingRows(batchId);
        if (staged.Count == 0)
        {
            return new WriteGateDecision(false, batchId, ["staging_batch_not_found"]);
        }

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
        using var cmd = tx.Connection!.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText =
            """
            INSERT INTO catalog_change_log
                (batch_id, table_name, entity_key, field_name, previous_value, new_value,
                 actor_type, actor_id, rationale, approval_state, revised_utc_ticks, release_version)
            VALUES ($batch, 'sensor', $entityKey, $field, $prev, $next, $actorType, $actorId, '', 'approved', $ticks, '')
            """;
        cmd.Parameters.AddWithValue("$batch", batchId);
        cmd.Parameters.AddWithValue("$entityKey", $"{row.PlatformId}/{row.SensorId}");
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
        // S22-01 / DBI-1.4: purge mount/loadout/magazine/comms staging on reject.
        using var cmd = tx.Connection!.CreateCommand();
        cmd.Transaction = tx;
        foreach (var table in new[]
        {
            "catalog_staging_sensor",
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

    private List<CatalogSensorBinding> LoadStagingRows(string batchId)
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
}