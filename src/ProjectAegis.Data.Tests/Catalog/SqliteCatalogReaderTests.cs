using Microsoft.Data.Sqlite;
using ProjectAegis.Data.Catalog;
using NUnit.Framework;

namespace ProjectAegis.Data.Tests.Catalog;

[Runner.CatalogSqliteCollection]
public sealed class SqliteCatalogReaderTests
{
    [Test]
    public void Sqlite_reader_loads_sorted_sensor_bindings()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-catalog-{Guid.NewGuid():N}.db");
        try
        {
            using (new SqliteCatalogReader(dbPath))
            {
                using var insert = new SqliteConnection($"Data Source={dbPath};Pooling=false");
                insert.Open();
                using var cmd = insert.CreateCommand();
                cmd.CommandText =
                    """
                    INSERT INTO sensor (platform_id, sensor_id, base_pd, source_fact_id, confidence,
                        import_batch_id, source_file, review_state, trl_level)
                    VALUES ('u2', 'radar-b', 0.5, 'test', 1.0, '', '', 'approved', 9),
                           ('u1', 'radar-a', 0.7, 'test', 1.0, '', '', 'approved', 9)
                    """;
                cmd.ExecuteNonQuery();
            }

            using (var reader = new SqliteCatalogReader(dbPath))
            {
                var bindings = reader.GetSortedSensorBindings();
                Assert.That(bindings.Count, Is.EqualTo(2));
                Assert.That(bindings[0].PlatformId, Is.EqualTo("u1"));
                Assert.That(bindings[0].SensorId, Is.EqualTo("radar-a"));
                Assert.That(reader.TryGetBasePd("u2", "radar-b", out var basePd), Is.True);
                Assert.That(basePd, Is.EqualTo(0.5));
            }
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(dbPath))
            {
                File.Delete(dbPath);
            }
        }
    }

    [Test]
    [NonParallelizable]
    public void Sqlite_reader_resolves_migrations_from_base_directory_when_cwd_is_deleted()
    {
        var originalCwd = Directory.GetCurrentDirectory();
        var doomedCwd = Path.Combine(Path.GetTempPath(), $"aegis-deleted-cwd-{Guid.NewGuid():N}");
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-catalog-deleted-cwd-{Guid.NewGuid():N}.db");
        try
        {
            Directory.CreateDirectory(doomedCwd);
            Directory.SetCurrentDirectory(doomedCwd);
            Directory.Delete(doomedCwd);

            using var reader = new SqliteCatalogReader(dbPath);
            Assert.That(reader.GetSortedSensorBindings(), Is.Empty);
        }
        finally
        {
            Directory.SetCurrentDirectory(originalCwd);
            SqliteConnection.ClearAllPools();
            if (File.Exists(dbPath))
            {
                File.Delete(dbPath);
            }

            if (Directory.Exists(doomedCwd))
            {
                Directory.Delete(doomedCwd, recursive: true);
            }
        }
    }

    [Test]
    public void ResolveMigrationPaths_walks_base_directory_lazily_and_catches_cwd_io_or_auth_errors()
    {
        var tempBase = Path.Combine(Path.GetTempPath(), $"aegis-mig-base-{Guid.NewGuid():N}");
        var tempEmpty = Path.Combine(Path.GetTempPath(), $"aegis-mig-empty-{Guid.NewGuid():N}");
        try
        {
            var migDir = Path.Combine(tempBase, "assets", "data", "catalog", "migrations");
            Directory.CreateDirectory(migDir);
            var sqlFile = Path.Combine(migDir, "001_sensor_base_pd.sql");
            File.WriteAllText(sqlFile, "-- test");
            Directory.CreateDirectory(tempEmpty);

            var cwdCalled = false;
            var fromBase = SqliteCatalogReader.ResolveMigrationPaths(
                tempBase,
                () =>
                {
                    cwdCalled = true;
                    throw new IOException("CWD must not be queried when baseDirectory resolves.");
                });

            Assert.That(cwdCalled, Is.False);
            Assert.That(fromBase, Is.EqualTo(new[] { sqlFile }));

            var fallbackIo = SqliteCatalogReader.ResolveMigrationPaths(
                tempEmpty,
                () => throw new IOException("deleted cwd"));
            Assert.That(fallbackIo, Is.EqualTo(new[] { Path.Combine("assets", "data", "catalog", "migrations", "001_sensor_base_pd.sql") }));

            var fallbackAuth = SqliteCatalogReader.ResolveMigrationPaths(
                tempEmpty,
                () => throw new UnauthorizedAccessException("inaccessible cwd"));
            Assert.That(fallbackAuth, Is.EqualTo(new[] { Path.Combine("assets", "data", "catalog", "migrations", "001_sensor_base_pd.sql") }));
        }
        finally
        {
            if (Directory.Exists(tempBase))
            {
                Directory.Delete(tempBase, recursive: true);
            }

            if (Directory.Exists(tempEmpty))
            {
                Directory.Delete(tempEmpty, recursive: true);
            }
        }
    }
}