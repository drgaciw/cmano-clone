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
}