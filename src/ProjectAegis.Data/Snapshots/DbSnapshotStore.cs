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

    public void Dispose()
    {
        _connection.Close();
        _connection.Dispose();
    }

    private bool TableExists(string table)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name=$name";
        cmd.Parameters.AddWithValue("$name", table);
        return Convert.ToInt32(cmd.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture) > 0;
    }

    // P2-3: Record snapshot after approve (Sprint 18 closeout). Deterministic hash, writes to catalog_snapshot for GetSorted + replay binding.
    public DbSnapshotRecord RecordApprovedImport(IReadOnlyList<string> approvedIds, string sourceFile, string importBatchId)
    {
        var canonical = string.Join("|", approvedIds.OrderBy(id => id, StringComparer.Ordinal));
        var input = $"{canonical}|{sourceFile}|{importBatchId}";
        using var sha = System.Security.Cryptography.SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(input);
        var hashBytes = sha.ComputeHash(bytes);
        // Compatible hex (netstandard2.1 / Unity): BitConverter + lowercase, no Substring on int
        var hash = BitConverter.ToString(hashBytes).Replace("-", string.Empty).ToLowerInvariant();
        var shortHash = hash.Length >= 8 ? hash.Substring(0, 8) : hash;
        var id = $"snap-{importBatchId}-{shortHash}";

        // Persist (idempotent for stable re-runs)
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "INSERT OR IGNORE INTO catalog_snapshot (snapshot_id) VALUES ($id)";
        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();

        return new DbSnapshotRecord(id, hash, sourceFile, importBatchId);
    }
}

public sealed record DbSnapshotRecord(string Id, string ContentHash, string SourceFile, string ImportBatchId);