using Microsoft.Data.Sqlite;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.WriteGate;
using Xunit;

namespace ProjectAegis.Data.Tests.WriteGate;

[Collection("CatalogSqlite")]
public sealed class CatalogWriteGatePlatformApproveTests
{
    [Fact]
    public void ApproveBatch_platform_batch_commits_to_live_table()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-platform-approve-{Guid.NewGuid():N}.db");
        try
        {
            var platform = new CatalogPlatformBinding(
                "u-test-platform",
                "Test Frigate",
                Domain: "surface",
                PlatformClass: "Frigate",
                Nationality: "NATO",
                GameTechnologyLevel: 7,
                ReviewState: CatalogReviewStates.Approved,
                TrlLevel: 9,
                ValueTier: CatalogProvenanceTier.InterpretedValue,
                CitationRef: "unit-test");

            using (var gate = new CatalogWriteGate(dbPath, new FixedCatalogClock(9201)))
            {
                var batchId = gate.ProposePlatformBatch([platform], "agent", "platform-approve-test");
                var decision = gate.ApproveBatch(batchId, "human", "qa-reviewer");
                Assert.True(decision.Committed);
            }

            using var connection = new SqliteConnection($"Data Source={dbPath};Pooling=false");
            connection.Open();
            var live = ReadLivePlatforms(connection);
            var committed = Assert.Single(live, p => p.PlatformId == "u-test-platform");
            Assert.Equal("Test Frigate", committed.DisplayName);
            Assert.Equal("Frigate", committed.PlatformClass);
            Assert.Equal("surface", committed.Domain);
            Assert.Equal("NATO", committed.Nationality);
            Assert.Equal(7, committed.GameTechnologyLevel);
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Fact]
    public void ApproveBatch_weapon_batch_commits_to_live_table()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-weapon-approve-{Guid.NewGuid():N}.db");
        try
        {
            var weapon = new CatalogWeaponRecord(
                "test-weapon-1",
                "Test Missile",
                MinRangeMeters: 1000,
                MaxRangeMeters: 80_000,
                WeaponType: "Guided Weapon",
                Guidance: "active-radar",
                ReviewState: CatalogReviewStates.Approved);

            using (var gate = new CatalogWriteGate(dbPath, new FixedCatalogClock(9202)))
            {
                var batchId = gate.ProposeWeaponBatch([weapon], "agent", "weapon-approve-test");
                var decision = gate.ApproveBatch(batchId, "human", "qa-reviewer");
                Assert.True(decision.Committed);
            }

            using var connection = new SqliteConnection($"Data Source={dbPath};Pooling=false");
            connection.Open();
            var live = ReadLiveWeapons(connection);
            var committed = Assert.Single(live, w => w.WeaponId == "test-weapon-1");
            Assert.Equal("Test Missile", committed.DisplayName);
            Assert.Equal(80_000, committed.MaxRangeMeters, precision: 0);
            Assert.Equal("Guided Weapon", committed.WeaponType);
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Fact]
    public void ApproveBatch_mount_loadout_magazine_comms_commit_paths()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-fitting-approve-{Guid.NewGuid():N}.db");
        try
        {
            var platform = new CatalogPlatformBinding("u1", "Test Hull", Domain: "surface", PlatformClass: "Frigate");
            var mount = new CatalogMount("u1", "vls-fore", MountType: "vls", ArcDeg: 90, Capacity: 8);
            var loadout = new CatalogLoadout("u1", "asuw-default", LoadoutName: "ASUW Default", Role: "asuw", IsDefault: true);
            var magazine = new CatalogMagazineEntry("u1", "asuw-default", "vls-fore", "rim-66", Quantity: 16, ReloadTimeSec: 30, Depth: 0);
            var comms = new CatalogCommsBinding(
                "u1",
                "link-1",
                Role: "txrx",
                SatcomCapable: true,
                ReviewState: CatalogReviewStates.Approved,
                TrlLevel: 9,
                ValueTier: CatalogProvenanceTier.InterpretedValue,
                CitationRef: "unit-test");

            using (var gate = new CatalogWriteGate(dbPath, new FixedCatalogClock(9203)))
            {
                var platformBatch = gate.ProposePlatformBatch([platform], "agent", "fitting-test");
                Assert.True(gate.ApproveBatch(platformBatch, "human", "qa").Committed);

                var mountBatch = gate.ProposeMountBatch([mount], "agent", "fitting-test");
                Assert.True(gate.ApproveBatch(mountBatch, "human", "qa").Committed);

                var loadoutBatch = gate.ProposeLoadoutBatch([loadout], "agent", "fitting-test");
                Assert.True(gate.ApproveBatch(loadoutBatch, "human", "qa").Committed);

                var magazineBatch = gate.ProposeMagazineBatch([magazine], "agent", "fitting-test");
                Assert.True(gate.ApproveBatch(magazineBatch, "human", "qa").Committed);

                var commsBatch = gate.ProposeCommsBatch([comms], "agent", "fitting-test");
                Assert.True(gate.ApproveBatch(commsBatch, "human", "qa").Committed);
            }

            using var connection = new SqliteConnection($"Data Source={dbPath};Pooling=false");
            connection.Open();

            var mounts = ReadLiveMounts(connection);
            var committedMount = Assert.Single(mounts, m => m.MountId == "vls-fore");
            Assert.Equal("vls", committedMount.MountType);
            Assert.Equal(8, committedMount.Capacity);

            var loadouts = ReadLiveLoadouts(connection);
            var committedLoadout = Assert.Single(loadouts, l => l.LoadoutId == "asuw-default");
            Assert.Equal("ASUW Default", committedLoadout.LoadoutName);
            Assert.True(committedLoadout.IsDefault);

            var magazines = ReadLiveMagazines(connection);
            var committedMagazine = Assert.Single(magazines, m => m.WeaponId == "rim-66");
            Assert.Equal(16, committedMagazine.Quantity);

            var commsRows = ReadLiveComms(connection);
            var committedComms = Assert.Single(commsRows, c => c.LinkId == "link-1");
            Assert.Equal("txrx", committedComms.Role);
            Assert.True(committedComms.SatcomCapable);
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Fact]
    public void RejectBatch_purges_all_staging_tables_DBI_1_4()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-reject-all-{Guid.NewGuid():N}.db");
        try
        {
            using (var gate = new CatalogWriteGate(dbPath, new FixedCatalogClock(9204)))
            {
                var platformBatch = gate.ProposePlatformBatch(
                    [new CatalogPlatformBinding("u-reject", "Reject Me")],
                    "agent",
                    "reject-test");
                var weaponBatch = gate.ProposeWeaponBatch(
                    [new CatalogWeaponRecord("w-reject", "Reject Weapon")],
                    "agent",
                    "reject-test");
                var mountBatch = gate.ProposeMountBatch(
                    [new CatalogMount("u-reject", "mount-reject")],
                    "agent",
                    "reject-test");
                var loadoutBatch = gate.ProposeLoadoutBatch(
                    [new CatalogLoadout("u-reject", "loadout-reject")],
                    "agent",
                    "reject-test");
                var magazineBatch = gate.ProposeMagazineBatch(
                    [new CatalogMagazineEntry("u-reject", "loadout-reject", "mount-reject", "w-reject", Quantity: 2)],
                    "agent",
                    "reject-test");
                var commsBatch = gate.ProposeCommsBatch(
                    [new CatalogCommsBinding("u-reject", "link-reject")],
                    "agent",
                    "reject-test");
                var linkBatch = gate.ProposeLinkCatalogBatch(
                    [new CatalogLinkEntry("link-catalog-reject", "Reject Link")],
                    "agent",
                    "reject-test");

                Assert.False(gate.RejectBatch(platformBatch, "human", "qa").Committed);
                Assert.False(gate.RejectBatch(weaponBatch, "human", "qa").Committed);
                Assert.False(gate.RejectBatch(mountBatch, "human", "qa").Committed);
                Assert.False(gate.RejectBatch(loadoutBatch, "human", "qa").Committed);
                Assert.False(gate.RejectBatch(magazineBatch, "human", "qa").Committed);
                Assert.False(gate.RejectBatch(commsBatch, "human", "qa").Committed);
                Assert.False(gate.RejectBatch(linkBatch, "human", "qa").Committed);
            }

            using var connection = new SqliteConnection($"Data Source={dbPath};Pooling=false");
            connection.Open();
            Assert.Equal(0, CountRows(connection, "catalog_staging_platform"));
            Assert.Equal(0, CountRows(connection, "catalog_staging_weapon"));
            Assert.Equal(0, CountRows(connection, "catalog_staging_mount"));
            Assert.Equal(0, CountRows(connection, "catalog_staging_loadout"));
            Assert.Equal(0, CountRows(connection, "catalog_staging_magazine"));
            Assert.Equal(0, CountRows(connection, "catalog_staging_comms"));
            Assert.Equal(0, CountRows(connection, "catalog_staging_link"));
            Assert.Equal(0, CountRows(connection, "platform", "platform_id = 'u-reject'"));
            Assert.Equal(0, CountRows(connection, "weapon_catalog", "weapon_id = 'w-reject'"));
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Fact]
    public void ApproveBatch_writes_change_log_for_platform_commit()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-platform-changelog-{Guid.NewGuid():N}.db");
        try
        {
            using (var gate = new CatalogWriteGate(dbPath, new FixedCatalogClock(9205)))
            {
                var batchId = gate.ProposePlatformBatch(
                    [new CatalogPlatformBinding("u-changelog", "Changelog Frigate", PlatformClass: "Frigate")],
                    "agent",
                    "changelog-test");
                Assert.True(gate.ApproveBatch(batchId, "human", "qa").Committed);
            }

            using var connection = new SqliteConnection($"Data Source={dbPath};Pooling=false");
            connection.Open();
            using var cmd = connection.CreateCommand();
            cmd.CommandText =
                """
                SELECT COUNT(*) FROM catalog_change_log
                WHERE table_name = 'platform' AND entity_key = 'u-changelog'
                """;
            var count = Convert.ToInt32(cmd.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture);
            Assert.True(count >= 1);
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    private static List<CatalogPlatformBinding> ReadLivePlatforms(SqliteConnection connection)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText =
            """
            SELECT platform_id, display_name, domain, platform_class, nationality, game_technology_level
            FROM platform
            ORDER BY platform_id ASC, snapshot_id ASC
            """;
        using var reader = cmd.ExecuteReader();
        var list = new List<CatalogPlatformBinding>();
        while (reader.Read())
        {
            list.Add(new CatalogPlatformBinding(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetString(4),
                reader.GetInt32(5)));
        }

        return list;
    }

    private static List<CatalogWeaponRecord> ReadLiveWeapons(SqliteConnection connection)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText =
            """
            SELECT weapon_id, display_name, min_range_meters, max_range_meters, weapon_type, guidance
            FROM weapon_catalog
            ORDER BY weapon_id ASC
            """;
        using var reader = cmd.ExecuteReader();
        var list = new List<CatalogWeaponRecord>();
        while (reader.Read())
        {
            list.Add(new CatalogWeaponRecord(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetDouble(2),
                reader.GetDouble(3),
                reader.GetString(4),
                reader.GetString(5)));
        }

        return list;
    }

    private static List<CatalogMount> ReadLiveMounts(SqliteConnection connection)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText =
            """
            SELECT platform_id, mount_id, mount_type, arc_deg, capacity, review_state
            FROM platform_mount
            ORDER BY platform_id ASC, mount_id ASC
            """;
        using var reader = cmd.ExecuteReader();
        var list = new List<CatalogMount>();
        while (reader.Read())
        {
            list.Add(new CatalogMount(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetDouble(3),
                reader.GetInt32(4),
                reader.GetString(5)));
        }

        return list;
    }

    private static List<CatalogLoadout> ReadLiveLoadouts(SqliteConnection connection)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText =
            """
            SELECT platform_id, loadout_id, loadout_name, role, is_default
            FROM platform_loadout
            ORDER BY platform_id ASC, loadout_id ASC
            """;
        using var reader = cmd.ExecuteReader();
        var list = new List<CatalogLoadout>();
        while (reader.Read())
        {
            list.Add(new CatalogLoadout(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetInt32(4) == 1));
        }

        return list;
    }

    private static List<CatalogMagazineEntry> ReadLiveMagazines(SqliteConnection connection)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText =
            """
            SELECT platform_id, loadout_id, mount_id, weapon_id, quantity, reload_time_sec, depth
            FROM platform_magazine
            ORDER BY platform_id ASC, loadout_id ASC, mount_id ASC, weapon_id ASC
            """;
        using var reader = cmd.ExecuteReader();
        var list = new List<CatalogMagazineEntry>();
        while (reader.Read())
        {
            list.Add(new CatalogMagazineEntry(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetInt32(4),
                reader.GetInt32(5),
                reader.GetInt32(6)));
        }

        return list;
    }

    private static List<CatalogCommsBinding> ReadLiveComms(SqliteConnection connection)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText =
            """
            SELECT platform_id, link_id, role, satcom_capable, review_state, trl_level, value_tier, citation_ref
            FROM platform_comms
            ORDER BY platform_id ASC, link_id ASC
            """;
        using var reader = cmd.ExecuteReader();
        var list = new List<CatalogCommsBinding>();
        while (reader.Read())
        {
            list.Add(new CatalogCommsBinding(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetInt32(3) == 1,
                reader.GetString(4),
                reader.GetInt32(5),
                reader.GetString(6),
                reader.GetString(7)));
        }

        return list;
    }

    private static int CountRows(SqliteConnection connection, string table, string? whereClause = null)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = whereClause is null
            ? $"SELECT COUNT(*) FROM {table}"
            : $"SELECT COUNT(*) FROM {table} WHERE {whereClause}";
        return Convert.ToInt32(cmd.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture);
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