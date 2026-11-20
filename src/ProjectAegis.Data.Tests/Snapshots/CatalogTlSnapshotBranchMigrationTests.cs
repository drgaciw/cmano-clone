using Microsoft.Data.Sqlite;
using ProjectAegis.Data.Catalog;
using NUnit.Framework;

namespace ProjectAegis.Data.Tests.Snapshots;

[Runner.CatalogSqliteCollection]
public sealed class CatalogTlSnapshotBranchMigrationTests
{
    [Test]
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

    [Test]
    public void CatalogTlTier_accepts_tl0_through_tl5_only()
    {
        foreach (var tier in CatalogTlTier.All)
        {
            Assert.That(CatalogTlTier.IsValid(tier), Is.True);
            Assert.That(CatalogTlTier.Normalize(tier), Is.EqualTo(tier));
        }

        Assert.That(CatalogTlTier.IsValid("TL-6"), Is.False);
        Assert.That(CatalogTlTier.Normalize("TL-6"), Is.EqualTo(CatalogTlTier.Default));
        Assert.That(CatalogTlTier.Normalize(null), Is.EqualTo(CatalogTlTier.Default));
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
        Assert.That(Convert.ToInt32(cmd.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture), Is.EqualTo(1));
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
            Assert.That(reader.GetString(0), Is.EqualTo(CatalogTlTier.Tl0));
            count++;
        }

        Assert.That(count > 0, Is.True);
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