using Microsoft.Data.Sqlite;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Import;
using ProjectAegis.Data.WriteGate;
using NUnit.Framework;

namespace ProjectAegis.Data.Tests.Import;

[Runner.CatalogSqliteCollection]
public sealed class CmoMarkdownImporterTests
{
    [Test]
    public void Baltic_platform_fixture_parses_three_platforms_with_baltic_ids()
    {
        var path = CmoMarkdownImporter.ResolveBalticPlatformFixturePath();
        var first = CmoMarkdownImporter.ReadPlatformBindings(path, mapBalticIds: true);
        var second = CmoMarkdownImporter.ReadPlatformBindings(path, mapBalticIds: true);

        Assert.That(first.Count, Is.EqualTo(3));
        Assert.That(second.Count, Is.EqualTo(first.Count));
        Assert.That(first.Select(p => p.PlatformId).ToArray(), Is.EqualTo(["hostile-1", "hostile-far", "u1"]));
        Assert.That(            first.Select(CatalogSortKeyComparer.FormatPlatformKey).ToArray(), Is.EqualTo(            CatalogSortKeyComparer.SortPlatforms(first).Select(CatalogSortKeyComparer.FormatPlatformKey).ToArray()));

        var u1 = AssertEx.Single(first, p => p.PlatformId == "u1");
        Assert.That(u1.DisplayName, Is.EqualTo("Patrol Frigate U1 , Baltic Patrol"));
        Assert.That(u1.PlatformClass, Is.EqualTo("Frigate"));
        Assert.That(u1.Domain, Is.EqualTo("surface"));
        Assert.That(u1.Nationality, Is.EqualTo("NATO"));
        Assert.That(u1.TrlLevel, Is.EqualTo(9));
        Assert.That(u1.ValueTier, Is.EqualTo(CatalogProvenanceTier.InterpretedValue));
        Assert.That(u1.CitationRef, Is.EqualTo("/ship/9001/"));
    }

    [Test]
    public void Weapon_mini_fixture_parses_three_weapons_deterministically()
    {
        var path = CmoMarkdownImporter.ResolveMiniWeaponFixturePath();
        var first = CmoMarkdownImporter.ReadWeaponBindings(path);
        var second = CmoMarkdownImporter.ReadWeaponBindings(path);

        Assert.That(first.Count, Is.EqualTo(3));
        Assert.That(second.Count, Is.EqualTo(first.Count));

        var standard = AssertEx.Single(first, w => w.WeaponId == "cmo-weapon-2001");
        Assert.That(standard.DisplayName, Is.EqualTo("RIM-66 Standard MR , USA"));
        Assert.That(standard.WeaponType, Is.EqualTo("Guided Weapon"));
        Assert.That(standard.MaxRangeMeters, Is.EqualTo(74_000).Within(0.5));
        Assert.That(standard.ReviewState, Is.EqualTo(CatalogReviewStates.Provisional));
    }

    [Test]
    public void Baltic_platform_fixture_parses_weapon_mounts_for_each_platform()
    {
        var path = CmoMarkdownImporter.ResolveBalticPlatformFixturePath();
        var mounts = CmoMarkdownImporter.ReadPlatformMounts(path, mapBalticIds: true);

        Assert.That(mounts.Count, Is.EqualTo(4));
        Assert.That(mounts.Any(m => m.PlatformId == "u1" && m.MountId == "rim-66-standard-mr" && m.MountType == "rail"), Is.True);
        Assert.That(mounts.Any(m => m.PlatformId == "u1" && m.MountId == "76mm-oto-melara" && m.MountType == "gun"), Is.True);
        Assert.That(mounts.Any(m => m.PlatformId == "hostile-1" && m.MountId == "ss-n-25-switchblade"), Is.True);
    }

    [Test]
    public void ProposePlatformBatch_stages_baltic_platform_rows()
    {
        var path = CmoMarkdownImporter.ResolveBalticPlatformFixturePath();
        var platforms = CmoMarkdownImporter.ReadPlatformBindings(path, mapBalticIds: true);
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-cmo-platform-{Guid.NewGuid():N}.db");

        try
        {
            using var gate = new CatalogWriteGate(dbPath, new FixedCatalogClock(9101));
            var batchId = gate.ProposePlatformBatch(platforms, "agent", "cmo-markdown-importer");
            Assert.That(batchId, Does.StartWith("batch-platform-"));

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
            Assert.That(count, Is.EqualTo(3));
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
    public void ProposeWeaponBatch_stages_weapon_rows()
    {
        var path = CmoMarkdownImporter.ResolveMiniWeaponFixturePath();
        var weapons = CmoMarkdownImporter.ReadWeaponBindings(path);
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-cmo-weapon-{Guid.NewGuid():N}.db");

        try
        {
            using var gate = new CatalogWriteGate(dbPath, new FixedCatalogClock(9102));
            var batchId = gate.ProposeWeaponBatch(weapons, "agent", "cmo-markdown-importer");
            Assert.That(batchId, Does.StartWith("batch-weapon-"));

            using var connection = new SqliteConnection($"Data Source={dbPath};Pooling=false");
            connection.Open();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM catalog_staging_weapon WHERE batch_id = $batch";
            cmd.Parameters.AddWithValue("$batch", batchId);
            var count = Convert.ToInt32(cmd.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture);
            Assert.That(count, Is.EqualTo(3));
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
                Assert.That(rejectPlatform.Committed, Is.False);

                var rejectMount = gate.RejectBatch(mountBatchId, "human", "qa");
                Assert.That(rejectMount.Committed, Is.False);
            }

            using var connection = new SqliteConnection($"Data Source={dbPath};Pooling=false");
            connection.Open();
            Assert.That(CountStagingRows(connection, "catalog_staging_platform"), Is.EqualTo(0));
            Assert.That(CountStagingRows(connection, "catalog_staging_mount"), Is.EqualTo(0));
            Assert.That(CountStagingRows(connection, "catalog_staging_weapon"), Is.EqualTo(0));
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
    public void ProposePlatformBatch_rejects_empty_list_DBI_7_1()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-cmo-empty-{Guid.NewGuid():N}.db");
        try
        {
            var gate = new CatalogWriteGate(dbPath, new FixedCatalogClock(9104));
            Exception? thrown = null;
            try
            {
                gate.ProposePlatformBatch([], "agent", "empty-test");
            }
            catch (Exception ex)
            {
                thrown = ex;
            }
            finally
            {
                gate.Dispose();
            }

            Assert.That(thrown, Is.TypeOf<ArgumentException>());
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

            Assert.That(result.PlatformCount, Is.EqualTo(3));
            Assert.That(result.WeaponCount, Is.EqualTo(3));
            Assert.That(result.MountCount, Is.EqualTo(4));
            Assert.That(result.LoadoutCount, Is.EqualTo(3));
            Assert.That(result.MagazineCount, Is.EqualTo(3));
            Assert.That(result.FittingQuarantinedCount, Is.EqualTo(1));
            Assert.That(result.PlatformBatchId, Is.Not.Null);
            Assert.That(result.WeaponBatchId, Is.Not.Null);
            Assert.That(result.MountBatchId, Is.Not.Null);
            Assert.That(result.LoadoutBatchId, Is.Not.Null);
            Assert.That(result.MagazineBatchId, Is.Not.Null);

            using var connection = new SqliteConnection($"Data Source={dbPath};Pooling=false");
            connection.Open();
            Assert.That(CountStagingRows(connection, "catalog_staging_platform"), Is.EqualTo(3));
            Assert.That(CountStagingRows(connection, "catalog_staging_weapon"), Is.EqualTo(3));
            Assert.That(CountStagingRows(connection, "catalog_staging_mount"), Is.EqualTo(4));
            Assert.That(CountStagingRows(connection, "catalog_staging_loadout"), Is.EqualTo(3));
            Assert.That(CountStagingRows(connection, "catalog_staging_magazine"), Is.EqualTo(3));
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
    public void Sensor_import_still_parses_deterministically_no_regression()
    {
        var path = CmoMarkdownImporter.ResolveMiniFixturePath();
        var bindings = CmoMarkdownImporter.ReadSensorBindings(path);

        Assert.That(bindings.Count >= 10, Is.True);
        var radar = AssertEx.Single(bindings, b => b.SensorId == "cmo-sensor-1001");
        Assert.That(radar.PlatformId, Is.EqualTo("test-radar-an-spy-1"));
        Assert.That(radar.BasePd, Is.EqualTo(0.75).Within(0.0000005));
    }

    [Test]
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
        Assert.That(CatalogSortKeyComparer.ComputeOrderingHash(fixture), Is.EqualTo(hash));
        Assert.That(hash, Is.EqualTo(CatalogSortKeyGoldenHashes.BalticCmoImport));
    }

    [Test]
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
                Assert.That(proposed.PlatformBatchId, Is.Not.Null);
                Assert.That(proposed.WeaponBatchId, Is.Not.Null);
                Assert.That(proposed.MountBatchId, Is.Not.Null);
                Assert.That(gate.ApproveBatch(proposed.PlatformBatchId!, "human", "cmo-e2e").Committed, Is.True);
                Assert.That(gate.ApproveBatch(proposed.WeaponBatchId!, "human", "cmo-e2e").Committed, Is.True);
                Assert.That(gate.ApproveBatch(proposed.MountBatchId!, "human", "cmo-e2e").Committed, Is.True);
            }

            using var connection = new SqliteConnection($"Data Source={dbPath};Pooling=false");
            connection.Open();

            var platformIds = ReadPlatformIds(connection);
            Assert.That(                platformIds, Is.EqualTo(                [
                    "hostile-1",
                    "hostile-far",
                    "u1",
                    CatalogSwarmPlatformDefaults.GenericSwarmPlatformId,
                    CatalogSwarmPlatformDefaults.UsnCecSwarmPlatformId,
                ]));

            var weaponIds = ReadWeaponIds(connection);
            // Baltic seed ships baltic-rim-66 / baltic-oto-76 + SWARM-A1 generic munition; CMO import adds cmo-weapon-*.
            Assert.That(                weaponIds, Is.EqualTo(                [
                    "baltic-oto-76",
                    "baltic-rim-66",
                    "cmo-weapon-2001",
                    "cmo-weapon-2002",
                    "cmo-weapon-2003",
                    CatalogSwarmPlatformDefaults.DefaultWeaponId,
                    CatalogSwarmPlatformDefaults.UsnCecWeaponId,
                ]));

            var mounts = ReadMountKeys(connection);
            // Seed mounts (gun-76, vls-fwd) + 4 CMO fixture mounts.
            Assert.That(mounts.Count, Is.EqualTo(6));
            Assert.That(                mounts.ToArray(), Is.EqualTo(                mounts.OrderBy(m => m, StringComparer.Ordinal).ToArray()));

            var u1 = ReadPlatformRow(connection, "u1");
            Assert.That(u1, Is.Not.Null);
            Assert.That(u1!.Value.DisplayName, Is.EqualTo("Patrol Frigate U1 , Baltic Patrol"));
            Assert.That(u1.Value.PlatformClass, Is.EqualTo("Frigate"));

            var standard = ReadWeaponRow(connection, "cmo-weapon-2001");
            Assert.That(standard, Is.Not.Null);
            Assert.That(standard!.Value.DisplayName, Is.EqualTo("RIM-66 Standard MR , USA"));
            Assert.That(standard.Value.MaxRangeMeters, Is.EqualTo(74_000).Within(0.5));
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