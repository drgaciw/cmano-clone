using Microsoft.Data.Sqlite;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Import;
using ProjectAegis.Data.WriteGate;
using Xunit;

namespace ProjectAegis.Data.Tests.Import;

[Collection("CatalogSqlite")]
public sealed class CmoMarkdownImporterTests
{
    [Fact]
    public void Baltic_platform_fixture_parses_three_platforms_with_baltic_ids()
    {
        var path = CmoMarkdownImporter.ResolveBalticPlatformFixturePath();
        var first = CmoMarkdownImporter.ReadPlatformBindings(path, mapBalticIds: true);
        var second = CmoMarkdownImporter.ReadPlatformBindings(path, mapBalticIds: true);

        Assert.Equal(3, first.Count);
        Assert.Equal(first.Count, second.Count);
        Assert.Equal(["hostile-1", "hostile-far", "u1"], first.Select(p => p.PlatformId).ToArray());
        Assert.Equal(
            CatalogSortKeyComparer.SortPlatforms(first).Select(CatalogSortKeyComparer.FormatPlatformKey).ToArray(),
            first.Select(CatalogSortKeyComparer.FormatPlatformKey).ToArray());

        var u1 = Assert.Single(first, p => p.PlatformId == "u1");
        Assert.Equal("Patrol Frigate U1 , Baltic Patrol", u1.DisplayName);
        Assert.Equal("Frigate", u1.PlatformClass);
        Assert.Equal("surface", u1.Domain);
        Assert.Equal("NATO", u1.Nationality);
        Assert.Equal(9, u1.TrlLevel);
        Assert.Equal(CatalogProvenanceTier.InterpretedValue, u1.ValueTier);
        Assert.Equal("/ship/9001/", u1.CitationRef);
    }

    [Fact]
    public void Weapon_mini_fixture_parses_three_weapons_deterministically()
    {
        var path = CmoMarkdownImporter.ResolveMiniWeaponFixturePath();
        var first = CmoMarkdownImporter.ReadWeaponBindings(path);
        var second = CmoMarkdownImporter.ReadWeaponBindings(path);

        Assert.Equal(3, first.Count);
        Assert.Equal(first.Count, second.Count);

        var standard = Assert.Single(first, w => w.WeaponId == "cmo-weapon-2001");
        Assert.Equal("RIM-66 Standard MR , USA", standard.DisplayName);
        Assert.Equal("Guided Weapon", standard.WeaponType);
        Assert.Equal(74_000, standard.MaxRangeMeters, precision: 0);
        Assert.Equal(CatalogReviewStates.Provisional, standard.ReviewState);
    }

    [Fact]
    public void Baltic_platform_fixture_parses_weapon_mounts_for_each_platform()
    {
        var path = CmoMarkdownImporter.ResolveBalticPlatformFixturePath();
        var mounts = CmoMarkdownImporter.ReadPlatformMounts(path, mapBalticIds: true);

        Assert.Equal(4, mounts.Count);
        Assert.Contains(mounts, m => m.PlatformId == "u1" && m.MountId == "rim-66-standard-mr" && m.MountType == "rail");
        Assert.Contains(mounts, m => m.PlatformId == "u1" && m.MountId == "76mm-oto-melara" && m.MountType == "gun");
        Assert.Contains(mounts, m => m.PlatformId == "hostile-1" && m.MountId == "ss-n-25-switchblade");
    }

    [Fact]
    public void ProposePlatformBatch_stages_baltic_platform_rows()
    {
        var path = CmoMarkdownImporter.ResolveBalticPlatformFixturePath();
        var platforms = CmoMarkdownImporter.ReadPlatformBindings(path, mapBalticIds: true);
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-cmo-platform-{Guid.NewGuid():N}.db");

        try
        {
            using var gate = new CatalogWriteGate(dbPath, new FixedCatalogClock(9101));
            var batchId = gate.ProposePlatformBatch(platforms, "agent", "cmo-markdown-importer");
            Assert.StartsWith("batch-platform-", batchId, StringComparison.Ordinal);

            using var connection = new SqliteConnection($"Data Source={dbPath};Pooling=false");
            connection.Open();
            using var cmd = connection.CreateCommand();
            cmd.CommandText =
                """
                SELECT COUNT(*) FROM catalog_staging_platform
                WHERE batch_id = $batch
                """;
            cmd.Parameters.AddWithValue("$batch", batchId);
            var count = Convert.ToInt32(cmd.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture);
            Assert.Equal(3, count);
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

    [Fact]
    public void ProposeWeaponBatch_stages_weapon_rows()
    {
        var path = CmoMarkdownImporter.ResolveMiniWeaponFixturePath();
        var weapons = CmoMarkdownImporter.ReadWeaponBindings(path);
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-cmo-weapon-{Guid.NewGuid():N}.db");

        try
        {
            using var gate = new CatalogWriteGate(dbPath, new FixedCatalogClock(9102));
            var batchId = gate.ProposeWeaponBatch(weapons, "agent", "cmo-markdown-importer");
            Assert.StartsWith("batch-weapon-", batchId, StringComparison.Ordinal);

            using var connection = new SqliteConnection($"Data Source={dbPath};Pooling=false");
            connection.Open();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM catalog_staging_weapon WHERE batch_id = $batch";
            cmd.Parameters.AddWithValue("$batch", batchId);
            var count = Convert.ToInt32(cmd.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture);
            Assert.Equal(3, count);
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

    [Fact]
    public void Reject_platform_batch_removes_all_staging_rows_DBI_1_4_orphan_guard()
    {
        var path = CmoMarkdownImporter.ResolveBalticPlatformFixturePath();
        var platforms = CmoMarkdownImporter.ReadPlatformBindings(path, mapBalticIds: true);
        var mounts = CmoMarkdownImporter.ReadPlatformMounts(path, mapBalticIds: true);
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-cmo-orphan-{Guid.NewGuid():N}.db");

        try
        {
            using (var gate = new CatalogWriteGate(dbPath, new FixedCatalogClock(9103)))
            {
                var platformBatchId = gate.ProposePlatformBatch(platforms, "agent", "orphan-test");
                var mountBatchId = gate.ProposeMountBatch(mounts, "agent", "orphan-test");

                var rejectPlatform = gate.RejectBatch(platformBatchId, "human", "qa");
                Assert.False(rejectPlatform.Committed);

                var rejectMount = gate.RejectBatch(mountBatchId, "human", "qa");
                Assert.False(rejectMount.Committed);
            }

            using var connection = new SqliteConnection($"Data Source={dbPath};Pooling=false");
            connection.Open();
            Assert.Equal(0, CountStagingRows(connection, "catalog_staging_platform"));
            Assert.Equal(0, CountStagingRows(connection, "catalog_staging_mount"));
            Assert.Equal(0, CountStagingRows(connection, "catalog_staging_weapon"));
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

    [Fact]
    public void ProposePlatformBatch_rejects_empty_list_DBI_7_1()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-cmo-empty-{Guid.NewGuid():N}.db");
        try
        {
            using var gate = new CatalogWriteGate(dbPath, new FixedCatalogClock(9104));
            Assert.Throws<ArgumentException>(() =>
                gate.ProposePlatformBatch([], "agent", "empty-test"));
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

    [Fact]
    public void ProposePlatformWeaponMounts_stages_all_entity_types_via_write_gate()
    {
        var platformPath = CmoMarkdownImporter.ResolveBalticPlatformFixturePath();
        var weaponPath = CmoMarkdownImporter.ResolveMiniWeaponFixturePath();
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-cmo-all-{Guid.NewGuid():N}.db");

        try
        {
            var result = CmoMarkdownImportProposer.ProposePlatformWeaponMounts(
                dbPath,
                platformPath,
                weaponPath,
                mapBalticPlatformIds: true,
                clock: new FixedCatalogClock(9105));

            Assert.Equal(3, result.PlatformCount);
            Assert.Equal(3, result.WeaponCount);
            Assert.Equal(4, result.MountCount);
            Assert.Equal(3, result.LoadoutCount);
            Assert.Equal(3, result.MagazineCount);
            Assert.Equal(1, result.FittingQuarantinedCount);
            Assert.NotNull(result.PlatformBatchId);
            Assert.NotNull(result.WeaponBatchId);
            Assert.NotNull(result.MountBatchId);
            Assert.NotNull(result.LoadoutBatchId);
            Assert.NotNull(result.MagazineBatchId);

            using var connection = new SqliteConnection($"Data Source={dbPath};Pooling=false");
            connection.Open();
            Assert.Equal(3, CountStagingRows(connection, "catalog_staging_platform"));
            Assert.Equal(3, CountStagingRows(connection, "catalog_staging_weapon"));
            Assert.Equal(4, CountStagingRows(connection, "catalog_staging_mount"));
            Assert.Equal(3, CountStagingRows(connection, "catalog_staging_loadout"));
            Assert.Equal(3, CountStagingRows(connection, "catalog_staging_magazine"));
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

    [Fact]
    public void Sensor_import_still_parses_deterministically_no_regression()
    {
        var path = CmoMarkdownImporter.ResolveMiniFixturePath();
        var bindings = CmoMarkdownImporter.ReadSensorBindings(path);

        Assert.True(bindings.Count >= 10);
        var radar = Assert.Single(bindings, b => b.SensorId == "cmo-sensor-1001");
        Assert.Equal("test-radar-an-spy-1", radar.PlatformId);
        Assert.Equal(0.75, radar.BasePd, precision: 6);
    }

    [Fact]
    public void CatalogSortKey_baltic_cmo_import_ordering_hash_matches_golden()
    {
        var platformPath = CmoMarkdownImporter.ResolveBalticPlatformFixturePath();
        var weaponPath = CmoMarkdownImporter.ResolveMiniWeaponFixturePath();

        var fixture = new CatalogSortKeyFixture(
            Sensors: [],
            Platforms: CmoMarkdownImporter.ReadPlatformBindings(platformPath, mapBalticIds: true),
            Weapons: CmoMarkdownImporter.ReadWeaponBindings(weaponPath),
            Mounts: CmoMarkdownImporter.ReadPlatformMounts(platformPath, mapBalticIds: true),
            Loadouts: [],
            Magazines: [],
            Comms: []);

        var hash = CatalogSortKeyComparer.ComputeOrderingHash(fixture);
        Assert.Equal(hash, CatalogSortKeyComparer.ComputeOrderingHash(fixture));
        Assert.Equal(CatalogSortKeyGoldenHashes.BalticCmoImport, hash);
    }

    [Fact]
    public void ProposePlatformWeaponMounts_approve_readback_reflects_live_rows_in_stable_order()
    {
        var platformPath = CmoMarkdownImporter.ResolveBalticPlatformFixturePath();
        var weaponPath = CmoMarkdownImporter.ResolveMiniWeaponFixturePath();
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-cmo-e2e-{Guid.NewGuid():N}.db");

        try
        {
            var proposed = CmoMarkdownImportProposer.ProposePlatformWeaponMounts(
                dbPath,
                platformPath,
                weaponPath,
                mapBalticPlatformIds: true,
                clock: new FixedCatalogClock(9106));

            using (var gate = new CatalogWriteGate(dbPath, new FixedCatalogClock(9107)))
            {
                Assert.NotNull(proposed.PlatformBatchId);
                Assert.NotNull(proposed.WeaponBatchId);
                Assert.NotNull(proposed.MountBatchId);
                Assert.True(gate.ApproveBatch(proposed.PlatformBatchId!, "human", "cmo-e2e").Committed);
                Assert.True(gate.ApproveBatch(proposed.WeaponBatchId!, "human", "cmo-e2e").Committed);
                Assert.True(gate.ApproveBatch(proposed.MountBatchId!, "human", "cmo-e2e").Committed);
            }

            using var connection = new SqliteConnection($"Data Source={dbPath};Pooling=false");
            connection.Open();

            var platformIds = ReadPlatformIds(connection);
            Assert.Equal(["hostile-1", "hostile-far", "u1"], platformIds);

            var weaponIds = ReadWeaponIds(connection);
            Assert.Equal(["cmo-weapon-2001", "cmo-weapon-2002", "cmo-weapon-2003"], weaponIds);

            var mounts = ReadMountKeys(connection);
            Assert.Equal(4, mounts.Count);
            Assert.Equal(
                mounts.OrderBy(m => m, StringComparer.Ordinal).ToArray(),
                mounts.ToArray());

            var u1 = ReadPlatformRow(connection, "u1");
            Assert.NotNull(u1);
            Assert.Equal("Patrol Frigate U1 , Baltic Patrol", u1.Value.DisplayName);
            Assert.Equal("Frigate", u1.Value.PlatformClass);

            var standard = ReadWeaponRow(connection, "cmo-weapon-2001");
            Assert.NotNull(standard);
            Assert.Equal("RIM-66 Standard MR , USA", standard.Value.DisplayName);
            Assert.Equal(74_000, standard.Value.MaxRangeMeters, precision: 0);
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

    private static List<string> ReadPlatformIds(SqliteConnection connection)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText =
            """
            SELECT DISTINCT platform_id
            FROM platform
            WHERE display_name != ''
            ORDER BY platform_id ASC
            """;
        using var reader = cmd.ExecuteReader();
        var ids = new List<string>();
        while (reader.Read())
        {
            ids.Add(reader.GetString(0));
        }

        return ids;
    }

    private static List<string> ReadWeaponIds(SqliteConnection connection)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT weapon_id FROM weapon_catalog ORDER BY weapon_id ASC";
        using var reader = cmd.ExecuteReader();
        var ids = new List<string>();
        while (reader.Read())
        {
            ids.Add(reader.GetString(0));
        }

        return ids;
    }

    private static List<string> ReadMountKeys(SqliteConnection connection)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText =
            """
            SELECT platform_id || '/' || mount_id
            FROM platform_mount
            ORDER BY platform_id ASC, mount_id ASC
            """;
        using var reader = cmd.ExecuteReader();
        var keys = new List<string>();
        while (reader.Read())
        {
            keys.Add(reader.GetString(0));
        }

        return keys;
    }

    private static (string DisplayName, string PlatformClass)? ReadPlatformRow(SqliteConnection connection, string platformId)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText =
            """
            SELECT display_name, platform_class
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

        return (reader.GetString(0), reader.GetString(1));
    }

    private static (string DisplayName, double MaxRangeMeters)? ReadWeaponRow(SqliteConnection connection, string weaponId)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText =
            """
            SELECT display_name, max_range_meters
            FROM weapon_catalog
            WHERE weapon_id = $weapon
            """;
        cmd.Parameters.AddWithValue("$weapon", weaponId);
        using var reader = cmd.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }

        return (reader.GetString(0), reader.GetDouble(1));
    }

    private static int CountStagingRows(SqliteConnection connection, string table)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = $"SELECT COUNT(*) FROM {table}";
        return Convert.ToInt32(cmd.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture);
    }
}