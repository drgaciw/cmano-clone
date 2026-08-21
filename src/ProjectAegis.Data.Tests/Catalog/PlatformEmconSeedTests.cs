using Microsoft.Data.Sqlite;
using ProjectAegis.Data.Catalog;
using Xunit;

namespace ProjectAegis.Data.Tests.Catalog;

/// <summary>
/// BUG-catalog-emcon-tables-empty: gauntlet T2 Baltic Patrol platforms must have
/// review_state-gated <c>platform_emcon</c> rows bound to existing catalog sensors.
/// </summary>
[Collection("CatalogSqlite")]
public sealed class PlatformEmconSeedTests
{
    /// <summary>
    /// Platforms actually used by <c>gauntlet-t2-escort-passive</c> plus the named
    /// Visby vs Sovremenny catalog ORBAT from that scenario's intent.
    /// </summary>
    public const string SeedBatchId = "batch-emcon-gauntlet-t2-seed-017";

    public static readonly string[] GauntletT2PlatformIds =
    [
        "k-22-gavle-ex-goteborg-class",
        "k-21-goteborg",
        "k-11-stockholm-spica-iii-1986",
        "jas-39e-gripen-ng-2021",
        "mrk-shkval-pr-22800-karakurt",
        "skr-admiral-grigorovich-pr-1135-6m",
        "skr-admiral-sergey-gorshkov-pr-2235-0",
        "ka-27m-helix-a",
        "k-31-visby-2009",
        "em-sovremenny-i-pr-956-sarych",
    ];

    /// <summary>
    /// Baltic/swarm fixtures that keep <c>radar-1</c> / Phase B export counts stable.
    /// Must match the denylist in migration 017.
    /// </summary>
    public static readonly string[] FixturePlatformIds =
    [
        "u1",
        "hostile-1",
        "hostile-far",
        CatalogSwarmPlatformDefaults.GenericSwarmPlatformId,
        CatalogSwarmPlatformDefaults.UsnCecSwarmPlatformId,
        CatalogValidationDefaults.PublicCorpusSensorCatalogPlatformId,
        "ucav-blue",
        "ucav-red",
        "usub-blue",
        "usub-red",
    ];

    [Fact]
    public void Production_catalog_seeds_provisional_off_emcon_for_gauntlet_t2_platforms()
    {
        var sourcePath = CatalogReaderFactory.ResolveBalticPatrolDatabasePath();
        Assert.True(File.Exists(sourcePath), $"Catalog DB missing: {sourcePath}");

        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-emcon-seed-{Guid.NewGuid():N}.db");
        try
        {
            File.Copy(sourcePath, dbPath, overwrite: true);
            ClearEmconSeed(dbPath);

            using var reader = new SqliteCatalogReader(dbPath, "emcon-t2-seed");
            var emcon = reader.GetSortedEmcon()
                .Where(row => GauntletT2PlatformIds.Contains(row.PlatformId, StringComparer.Ordinal))
                .ToArray();

            Assert.True(
                emcon.Length > 0,
                "platform_emcon must contain rows for gauntlet T2 platforms after catalog open.");

            foreach (var platformId in GauntletT2PlatformIds)
            {
                Assert.True(
                    emcon.Any(row => string.Equals(row.PlatformId, platformId, StringComparison.Ordinal)),
                    $"Missing platform_emcon seed for {platformId}");
            }

            Assert.All(emcon, row =>
            {
                var expectedPosture = string.Equals(row.EmitterId, "radar-1", StringComparison.Ordinal) &&
                    string.Equals(row.Condition, "free", StringComparison.Ordinal)
                    ? "active"
                    : "off";
                Assert.Equal(expectedPosture, row.Posture);
                Assert.Equal(CatalogReviewStates.Provisional, row.ReviewState);
                Assert.Contains(row.Condition, (IReadOnlyList<string>)["silent", "restricted", "free"]);
                Assert.False(string.IsNullOrWhiteSpace(row.EmitterId));
            });

            Assert.True(reader.TryGetEmcon("k-31-visby-2009", "silent", "cmo-sensor-1827", out var visby));
            Assert.Equal("off", visby.Posture);
            Assert.Equal(CatalogReviewStates.Provisional, visby.ReviewState);

            Assert.True(
                reader.TryGetEmcon("k-31-visby-2009", "free", "radar-1", out var visbyDefault),
                "Default resolver triple (free/radar-1) must have a CatalogEmcon row.");
            Assert.Equal("active", visbyDefault.Posture);
            Assert.Equal(CatalogReviewStates.Provisional, visbyDefault.ReviewState);
            Assert.True(reader.TryGetEmcon("k-31-visby-2009", "silent", "radar-1", out var visbySilentAlias));
            Assert.Equal("off", visbySilentAlias.Posture);

            using var connection = new SqliteConnection($"Data Source={dbPath};Pooling=false");
            connection.Open();
            using var orphan = connection.CreateCommand();
            orphan.CommandText =
                """
                SELECT COUNT(*)
                FROM platform_emcon e
                LEFT JOIN sensor s
                  ON s.platform_id = e.platform_id AND s.sensor_id = e.emitter_id
                WHERE s.sensor_id IS NULL
                  AND e.emitter_id != 'radar-1'
                """;
            var orphanCount = Convert.ToInt32(orphan.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture);
            Assert.Equal(0, orphanCount);

            Assert.Equal(0, CountUnseededNonFixture(connection));
            Assert.DoesNotContain(
                reader.GetSortedEmcon(),
                row => FixturePlatformIds.Contains(row.PlatformId, StringComparer.Ordinal));

            using var staging = connection.CreateCommand();
            staging.CommandText =
                """
                SELECT COUNT(*) FROM catalog_staging_emcon
                WHERE batch_id = $batch
                """;
            staging.Parameters.AddWithValue("$batch", SeedBatchId);
            var stagingCount = Convert.ToInt32(
                staging.ExecuteScalar(),
                System.Globalization.CultureInfo.InvariantCulture);
            Assert.True(stagingCount > 0, "catalog_staging_emcon must contain review-gated seed rows.");
            Assert.Equal(reader.GetSortedEmcon().Count, stagingCount);

            using var batch = connection.CreateCommand();
            batch.CommandText =
                """
                SELECT actor_type, approval_state, review_state
                FROM catalog_staging_batch b
                INNER JOIN catalog_staging_emcon e ON e.batch_id = b.batch_id
                WHERE b.batch_id = $batch
                ORDER BY e.platform_id ASC, e.condition ASC, e.emitter_id ASC
                LIMIT 1
                """;
            batch.Parameters.AddWithValue("$batch", SeedBatchId);
            using var batchReader = batch.ExecuteReader();
            Assert.True(batchReader.Read());
            Assert.Equal("migration", batchReader.GetString(0));
            Assert.Equal("proposed", batchReader.GetString(1));
            Assert.Equal(CatalogReviewStates.Provisional, batchReader.GetString(2));
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Fact]
    public void Emcon_seed_is_idempotent_and_skips_empty_schema_catalogs()
    {
        var emptyPath = Path.Combine(Path.GetTempPath(), $"aegis-emcon-empty-{Guid.NewGuid():N}.db");
        var sourcePath = CatalogReaderFactory.ResolveBalticPatrolDatabasePath();
        var copyPath = Path.Combine(Path.GetTempPath(), $"aegis-emcon-idemp-{Guid.NewGuid():N}.db");
        try
        {
            using (var emptyReader = new SqliteCatalogReader(emptyPath, "emcon-empty"))
            {
                Assert.Empty(emptyReader.GetSortedEmcon());
                Assert.Equal(0, CountStaging(emptyPath));
            }

            File.Copy(sourcePath, copyPath, overwrite: true);
            ClearEmconSeed(copyPath);
            int firstCount;
            using (var first = new SqliteCatalogReader(copyPath, "emcon-idemp-1"))
            {
                firstCount = first.GetSortedEmcon().Count;
            }

            using (var second = new SqliteCatalogReader(copyPath, "emcon-idemp-2"))
            {
                var secondCount = second.GetSortedEmcon().Count;
                Assert.Equal(firstCount, secondCount);
                Assert.True(firstCount > 0);
                Assert.Equal(firstCount, CountStaging(copyPath));
            }
        }
        finally
        {
            Cleanup(emptyPath);
            Cleanup(copyPath);
        }
    }

    private static int CountUnseededNonFixture(SqliteConnection connection)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText =
            """
            SELECT COUNT(*)
            FROM (
                SELECT p.platform_id
                FROM platform p
                INNER JOIN sensor s ON s.platform_id = p.platform_id
                WHERE p.platform_id NOT IN (
                    'u1','hostile-1','hostile-far','uas-swarm-generic','usn-uas-swarm-cec',
                    'cmo-sensor-catalog','ucav-blue','ucav-red','usub-blue','usub-red')
                  AND NOT EXISTS (
                    SELECT 1 FROM platform_emcon e WHERE e.platform_id = p.platform_id)
                GROUP BY p.platform_id
            )
            """;
        return Convert.ToInt32(cmd.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture);
    }

    private static int CountStaging(string dbPath)
    {
        using var connection = new SqliteConnection($"Data Source={dbPath};Pooling=false");
        connection.Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM catalog_staging_emcon WHERE batch_id = $batch";
        cmd.Parameters.AddWithValue("$batch", SeedBatchId);
        return Convert.ToInt32(cmd.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture);
    }

    private static void ClearEmconSeed(string dbPath)
    {
        SqliteConnection.ClearAllPools();
        using var connection = new SqliteConnection($"Data Source={dbPath};Pooling=false");
        connection.Open();
        using (var staging = connection.CreateCommand())
        {
            staging.CommandText = "DELETE FROM catalog_staging_emcon WHERE batch_id = $batch";
            staging.Parameters.AddWithValue("$batch", SeedBatchId);
            staging.ExecuteNonQuery();
        }

        using (var batch = connection.CreateCommand())
        {
            batch.CommandText = "DELETE FROM catalog_staging_batch WHERE batch_id = $batch";
            batch.Parameters.AddWithValue("$batch", SeedBatchId);
            batch.ExecuteNonQuery();
        }

        using var emcon = connection.CreateCommand();
        emcon.CommandText = "DELETE FROM platform_emcon";
        emcon.ExecuteNonQuery();
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
