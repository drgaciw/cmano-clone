namespace ProjectAegis.Data.Snapshots;

using Microsoft.Data.Sqlite;
using ProjectAegis.Data.Catalog;

/// <summary>Reads catalog snapshot and release-train metadata (req-06 P0).</summary>
public sealed class DbSnapshotStore : IDisposable
{
    private readonly SqliteConnection _connection;

    public DbSnapshotStore(string databasePath)
    {
        _connection = new SqliteConnection($"Data Source={databasePath};Pooling=false");
        _connection.Open();
        using var _ = new SqliteCatalogReader(databasePath, "snapshot-store-bootstrap");
    }

    public IReadOnlyList<string> GetSortedSnapshotIds()
    {
        if (!TableExists("catalog_snapshot"))
        {
            return [CatalogValidationDefaults.BalticSnapshotId];
        }

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT snapshot_id FROM catalog_snapshot ORDER BY snapshot_id ASC";
        using var reader = cmd.ExecuteReader();
        var ids = new List<string>();
        while (reader.Read())
        {
            ids.Add(reader.GetString(0));
        }

        return ids.Count > 0 ? ids : [CatalogValidationDefaults.BalticSnapshotId];
    }

    public bool TryGetContentHash(string snapshotId, out string contentHashSha256)
    {
        contentHashSha256 = string.Empty;
        if (!TableExists("catalog_snapshot") || !TableHasColumn("catalog_snapshot", "content_hash_sha256"))
        {
            return false;
        }

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT content_hash_sha256 FROM catalog_snapshot WHERE snapshot_id = $id";
        cmd.Parameters.AddWithValue("$id", snapshotId);
        var scalar = cmd.ExecuteScalar();
        if (scalar == null || scalar is DBNull)
        {
            return false;
        }

        contentHashSha256 = (string)scalar;
        return !string.IsNullOrEmpty(contentHashSha256);
    }

    public bool TryGetBranch(string snapshotId, out string branch)
    {
        branch = CatalogTlTier.Default;
        if (!TableExists("catalog_snapshot") || !TableHasColumn("catalog_snapshot", "branch"))
        {
            return false;
        }

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT branch FROM catalog_snapshot WHERE snapshot_id = $id";
        cmd.Parameters.AddWithValue("$id", snapshotId);
        var scalar = cmd.ExecuteScalar();
        if (scalar == null || scalar is DBNull)
        {
            return false;
        }

        branch = CatalogTlTier.Normalize((string)scalar);
        return true;
    }

    public void RecordRelease(
        string releaseVersion,
        string snapshotId,
        string contentHashSha256,
        long createdUtcTicks,
        string schemaVersion = "006",
        string notes = "",
        string? branch = null)
    {
        if (string.IsNullOrWhiteSpace(releaseVersion))
        {
            throw new ArgumentException("Release version required.", nameof(releaseVersion));
        }

        if (string.IsNullOrWhiteSpace(snapshotId))
        {
            throw new ArgumentException("Snapshot id required.", nameof(snapshotId));
        }

        if (string.IsNullOrWhiteSpace(contentHashSha256))
        {
            throw new ArgumentException("Content hash required.", nameof(contentHashSha256));
        }

        var resolvedBranch = CatalogTlTier.Normalize(branch);
        using var tx = _connection.BeginTransaction();

        using (var snap = tx.Connection!.CreateCommand())
        {
            snap.Transaction = tx;
            if (TableHasColumn("catalog_snapshot", "branch"))
            {
                snap.CommandText =
                    """
                    INSERT INTO catalog_snapshot (snapshot_id, content_hash_sha256, branch)
                    VALUES ($id, $hash, $branch)
                    ON CONFLICT(snapshot_id) DO UPDATE SET
                        content_hash_sha256 = excluded.content_hash_sha256,
                        branch = excluded.branch
                    """;
                snap.Parameters.AddWithValue("$branch", resolvedBranch);
            }
            else
            {
                snap.CommandText =
                    """
                    INSERT INTO catalog_snapshot (snapshot_id, content_hash_sha256)
                    VALUES ($id, $hash)
                    ON CONFLICT(snapshot_id) DO UPDATE SET content_hash_sha256 = excluded.content_hash_sha256
                    """;
            }

            snap.Parameters.AddWithValue("$id", snapshotId);
            snap.Parameters.AddWithValue("$hash", contentHashSha256);
            snap.ExecuteNonQuery();
        }

        using (var release = tx.Connection!.CreateCommand())
        {
            release.Transaction = tx;
            release.CommandText =
                """
                INSERT OR REPLACE INTO db_release
                    (release_version, snapshot_id, schema_version, created_utc_ticks, notes)
                VALUES ($version, $snapshot, $schema, $ticks, $notes)
                """;
            release.Parameters.AddWithValue("$version", releaseVersion);
            release.Parameters.AddWithValue("$snapshot", snapshotId);
            release.Parameters.AddWithValue("$schema", schemaVersion);
            release.Parameters.AddWithValue("$ticks", createdUtcTicks);
            release.Parameters.AddWithValue("$notes", notes);
            release.ExecuteNonQuery();
        }

        tx.Commit();
    }

    /// <summary>
    /// S32-02: consolidate per-domain nightly <c>releaseVersion</c> rows into one curator drop manifest.
    /// </summary>
    public UnifiedReleaseTrainManifest RecordUnifiedRelease(
        string unifiedReleaseVersion,
        string snapshotId,
        string tlTier,
        IReadOnlyList<string> domainReleaseVersions,
        long createdUtcTicks,
        string schemaVersion = "010")
    {
        var manifest = UnifiedReleaseTrainManifest.Consolidate(
            this,
            unifiedReleaseVersion,
            snapshotId,
            tlTier,
            domainReleaseVersions);

        RecordRelease(
            manifest.ReleaseVersion,
            manifest.SnapshotId,
            manifest.ContentHashSha256,
            createdUtcTicks,
            schemaVersion: schemaVersion,
            notes: manifest.ToNotesJson(),
            branch: manifest.TlTier);

        return manifest;
    }

    public bool TryResolveReleaseVersion(string releaseVersion, out string snapshotId)
    {
        snapshotId = "";
        if (string.IsNullOrWhiteSpace(releaseVersion))
        {
            return false;
        }

        foreach (var release in GetSortedReleases())
        {
            if (string.Equals(release.ReleaseVersion, releaseVersion.Trim(), StringComparison.Ordinal))
            {
                snapshotId = release.SnapshotId;
                return true;
            }
        }

        return false;
    }

    public bool TryGetUnifiedManifest(string releaseVersion, out UnifiedReleaseTrainManifest manifest)
    {
        manifest = null!;
        if (string.IsNullOrWhiteSpace(releaseVersion))
        {
            return false;
        }

        foreach (var release in GetSortedReleases())
        {
            if (!string.Equals(release.ReleaseVersion, releaseVersion.Trim(), StringComparison.Ordinal))
            {
                continue;
            }

            if (UnifiedReleaseTrainManifest.TryParseFromNotes(release.Notes, release.ReleaseVersion, out manifest))
            {
                return true;
            }

            return false;
        }

        return false;
    }

    public bool TryGetLatestUnifiedManifestForSnapshot(string snapshotId, out UnifiedReleaseTrainManifest manifest)
    {
        manifest = null!;
        UnifiedReleaseTrainManifest? latest = null;
        foreach (var release in GetSortedReleases())
        {
            if (!string.Equals(release.SnapshotId, snapshotId, StringComparison.Ordinal))
            {
                continue;
            }

            if (!UnifiedReleaseTrainManifest.TryParseFromNotes(release.Notes, release.ReleaseVersion, out var candidate))
            {
                continue;
            }

            latest = candidate;
        }

        if (latest == null)
        {
            return false;
        }

        manifest = latest;
        return true;
    }

    public IReadOnlyList<DbReleaseRecord> GetSortedReleases()
    {
        if (!TableExists("db_release"))
        {
            return [];
        }

        using var cmd = _connection.CreateCommand();
        cmd.CommandText =
            """
            SELECT release_version, snapshot_id, schema_version, created_utc_ticks, notes
            FROM db_release
            ORDER BY release_version ASC
            """;
        using var reader = cmd.ExecuteReader();
        var list = new List<DbReleaseRecord>();
        while (reader.Read())
        {
            list.Add(new DbReleaseRecord(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetInt64(3),
                reader.GetString(4)));
        }

        return list;
    }

    /// <summary>
    /// S31-03: resolve snapshotId+dbRef for a normalized TL branch from catalog_snapshot + db_release.
    /// </summary>
    public bool TryResolveSnapshotForBranch(string tlBranch, out string snapshotId, out string dbRef)
    {
        snapshotId = "";
        dbRef = "";

        if (!TableExists("catalog_snapshot") || !TableHasColumn("catalog_snapshot", "branch"))
        {
            return false;
        }

        var normalized = CatalogTlTier.Normalize(tlBranch);
        var matchingIds = new List<string>();
        using (var cmd = _connection.CreateCommand())
        {
            cmd.CommandText =
                """
                SELECT snapshot_id
                FROM catalog_snapshot
                WHERE branch = $branch
                ORDER BY snapshot_id ASC
                """;
            cmd.Parameters.AddWithValue("$branch", normalized);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                matchingIds.Add(reader.GetString(0));
            }
        }

        if (!CatalogReleaseTrainResolver.TryResolveFromCandidates(
                matchingIds,
                GetSortedReleases(),
                out snapshotId,
                out dbRef))
        {
            return false;
        }

        if (TryGetLatestUnifiedManifestForSnapshot(snapshotId, out var manifest))
        {
            dbRef = manifest.ReleaseVersion;
        }

        return true;
    }

    /// <summary>Record snapshot after approve (gate auto-record). Deterministic hash for replay binding.</summary>
    public DbSnapshotRecord RecordApprovedImport(
        IReadOnlyList<string> approvedIds,
        string sourceFile,
        string importBatchId,
        string? branch = null)
    {
        var canonical = string.Join("|", approvedIds.OrderBy(id => id, StringComparer.Ordinal));
        var input = $"{canonical}|{sourceFile}|{importBatchId}";
        using var sha = System.Security.Cryptography.SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(input);
        var hashBytes = sha.ComputeHash(bytes);
        var hash = BitConverter.ToString(hashBytes).Replace("-", string.Empty).ToLowerInvariant();
        var shortHash = hash.Length >= 8 ? hash.Substring(0, 8) : hash;
        var id = $"snap-{importBatchId}-{shortHash}";

        var resolvedBranch = CatalogTlTier.Normalize(branch);
        using var cmd = _connection.CreateCommand();
        if (TableHasColumn("catalog_snapshot", "branch"))
        {
            cmd.CommandText =
                """
                INSERT OR IGNORE INTO catalog_snapshot (snapshot_id, branch)
                VALUES ($id, $branch)
                """;
            cmd.Parameters.AddWithValue("$branch", resolvedBranch);
        }
        else
        {
            cmd.CommandText = "INSERT OR IGNORE INTO catalog_snapshot (snapshot_id) VALUES ($id)";
        }

        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();

        return new DbSnapshotRecord(id, hash, sourceFile, importBatchId, resolvedBranch);
    }

    public void Dispose()
    {
        _connection.Close();
        _connection.Dispose();
        SqliteConnection.ClearAllPools();
    }

    private bool TableExists(string table)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name=$name";
        cmd.Parameters.AddWithValue("$name", table);
        return Convert.ToInt32(cmd.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture) > 0;
    }

    private bool TableHasColumn(string table, string column)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = $"SELECT COUNT(*) FROM pragma_table_info('{table}') WHERE name = $col";
        cmd.Parameters.AddWithValue("$col", column);
        return Convert.ToInt32(cmd.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture) > 0;
    }
}

public sealed record DbSnapshotRecord(
    string Id,
    string ContentHash,
    string SourceFile,
    string ImportBatchId,
    string Branch = CatalogTlTier.Default);