using Microsoft.Data.Sqlite;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Import;
using Xunit;

namespace ProjectAegis.Data.Tests.Snapshots;

[Collection("CatalogSqlite")]
public sealed class CatalogTlSnapshotBranchMigrationTests
{
    [Fact]
    public void Migration_010_applies_idempotently_and_defaults_existing_rows_to_tl0()
    {
        var jsonPath = CatalogJsonImporter.ResolveBalticSensorsJsonPath();
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-tl-branch-{Guid.NewGuid():N}.db");
        try
        {
            CatalogJsonImporter.ImportToSqlite(jsonPath, dbPath);
            AssertBranchColumnExists(dbPath);

            using (var reader = new SqliteCatalogReader(dbPath, "tl-branch-idempotent"))
            {
                _ = reader.GetSortedSensorBindings();
            }

            AssertBranchColumnExists(dbPath);
            AssertAllSnapshotBranchesDefaultToTl0(dbPath);
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Fact]
    public void CatalogTlTier_accepts_tl0_through_tl5_only()
    {
        foreach (var tier in CatalogTlTier.All)
        {
            Assert.True(CatalogTlTier.IsValid(tier));
            Assert.Equal(tier, CatalogTlTier.Normalize(tier));
        }

        Assert.False(CatalogTlTier.IsValid("TL-6"));
        Assert.Equal(CatalogTlTier.Default, CatalogTlTier.Normalize("TL-6"));
        Assert.Equal(CatalogTlTier.Default, CatalogTlTier.Normalize(null));
    }

    private static void AssertBranchColumnExists(string dbPath)
    {
        using var connection = new SqliteConnection($"Data Source={dbPath}");
        connection.Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText =
            """
            SELECT COUNT(*) FROM pragma_table_info('catalog_snapshot')
            WHERE name = 'branch'
            """;
        Assert.Equal(1, Convert.ToInt32(cmd.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture));
    }

    private static void AssertAllSnapshotBranchesDefaultToTl0(string dbPath)
    {
        using var connection = new SqliteConnection($"Data Source={dbPath}");
        connection.Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT branch FROM catalog_snapshot ORDER BY snapshot_id ASC";
        using var reader = cmd.ExecuteReader();
        var count = 0;
        while (reader.Read())
        {
            Assert.Equal(CatalogTlTier.Tl0, reader.GetString(0));
            count++;
        }

        Assert.True(count > 0);
    }

    private static void Cleanup(string dbPath)
    {
        SqliteConnection.ClearAllPools();
        if (File.Exists(dbPath))
        {
            File.Delete(dbPath);
        }
    }
}