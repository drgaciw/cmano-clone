using Microsoft.Data.Sqlite;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.WriteGate;
using NUnit.Framework;

namespace ProjectAegis.Data.Tests.WriteGate;

[Runner.CatalogSqliteCollection]
public sealed class CatalogWriteGatePlatformApproveTests
{
    [Test]
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
                Assert.That(decision.Committed, Is.True);
            }

            using var connection = new SqliteConnection($"Data Source={dbPath};Pooling=false");
            connection.Open();
            var live = ReadLivePlatforms(connection);
            var committed = AssertEx.Single(live, p => p.PlatformId == "u-test-platform");
            Assert.That(committed.DisplayName, Is.EqualTo("Test Frigate"));
            Assert.That(committed.PlatformClass, Is.EqualTo("Frigate"));
            Assert.That(committed.Domain, Is.EqualTo("surface"));
            Assert.That(committed.Nationality, Is.EqualTo("NATO"));
            Assert.That(committed.GameTechnologyLevel, Is.EqualTo(7));
            // ApplyCorePosition false → new rows keep default combat radius 1.0
            Assert.That(ReadLiveCorePosition(connection, "u-test-platform").CombatRadiusNm, Is.EqualTo(1.0).Within(0.00005));
            Assert.That(ReadLiveCorePosition(connection, "u-test-platform").LatDeg, Is.EqualTo(0).Within(0.00005));
            Assert.That(ReadLiveCorePosition(connection, "u-test-platform").LonDeg, Is.EqualTo(0).Within(0.00005));
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Test]
    public void ApproveBatch_ApplyCorePosition_true_writes_lat_lon_combat_radius()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-platform-core-pos-{Guid.NewGuid():N}.db");
        try
        {
            var platform = new CatalogPlatformBinding(
                "u-core-pos",
                "Core Pos Hull",
                Domain: "surface",
                PlatformClass: "OPV",
                LatDeg: 57.25,
                LonDeg: 20.75,
                CombatRadiusNm: 145.5,
                ApplyCorePosition: true);

            using (var gate = new CatalogWriteGate(dbPath, new FixedCatalogClock(9220)))
            {
                var batchId = gate.ProposePlatformBatch([platform], "agent", "core-pos-test");
                Assert.That(gate.ApproveBatch(batchId, "human", "qa-reviewer").Committed, Is.True);
            }

            using var connection = new SqliteConnection($"Data Source={dbPath};Pooling=false");
            connection.Open();
            var core = ReadLiveCorePosition(connection, "u-core-pos");
            Assert.That(core.LatDeg, Is.EqualTo(57.25).Within(0.00005));
            Assert.That(core.LonDeg, Is.EqualTo(20.75).Within(0.00005));
            Assert.That(core.CombatRadiusNm, Is.EqualTo(145.5).Within(0.00005));
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Test]
    public void ApproveBatch_ApplyCorePosition_true_zero_radius_defaults_to_one()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-platform-core-zero-r-{Guid.NewGuid():N}.db");
        try
        {
            var platform = new CatalogPlatformBinding(
                "u-core-zero-r",
                "Zero Radius",
                LatDeg: 10,
                LonDeg: 20,
                CombatRadiusNm: 0,
                ApplyCorePosition: true);

            using (var gate = new CatalogWriteGate(dbPath, new FixedCatalogClock(9221)))
            {
                var batchId = gate.ProposePlatformBatch([platform], "agent", "core-zero-r");
                Assert.That(gate.ApproveBatch(batchId, "human", "qa").Committed, Is.True);
            }

            using var connection = new SqliteConnection($"Data Source={dbPath};Pooling=false");
            connection.Open();
            var core = ReadLiveCorePosition(connection, "u-core-zero-r");
            Assert.That(core.LatDeg, Is.EqualTo(10).Within(0.00005));
            Assert.That(core.LonDeg, Is.EqualTo(20).Within(0.00005));
            Assert.That(core.CombatRadiusNm, Is.EqualTo(1.0).Within(0.00005));
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    /// <summary>
    /// Adversarial: ProposePlatformBatch without ApproveBatch must never surface staged
    /// platforms as live catalog rows. Staging ≠ live (doc 06 / write-gate dual-track).
    /// Mirrors ProposeSensorBatch_without_ApproveBatch_not_readable_as_live_catalog.
    /// </summary>
    [Test]
    public void ProposePlatformBatch_without_ApproveBatch_not_readable_as_live_catalog()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-platform-propose-only-{Guid.NewGuid():N}.db");
        try
        {
            CatalogSeedBootstrap.SeedBalticPatrol(dbPath, overwrite: true);
            var staged = new CatalogPlatformBinding(
                "u-platform-staged-only",
                "Staged-Only Hull",
                Domain: "surface",
                PlatformClass: "Frigate",
                Nationality: "NATO",
                GameTechnologyLevel: 6,
                ReviewState: CatalogReviewStates.Approved,
                TrlLevel: 7,
                ValueTier: CatalogProvenanceTier.InterpretedValue,
                CitationRef: "no-human-approve");

            string batchId;
            using (var gate = new CatalogWriteGate(dbPath, new FixedCatalogClock(9210)))
            {
                batchId = gate.ProposePlatformBatch([staged], "agent", "no-approve");
                var pending = gate.ListPendingBatches();
                Assert.That(pending.Any(p => p.BatchId == batchId && p.ApprovalState == "proposed"), Is.True);
            }

            using var connection = new SqliteConnection($"Data Source={dbPath};Pooling=false");
            connection.Open();
            var live = ReadLivePlatforms(connection);
            Assert.That(live.Any(p => p.PlatformId == "u-platform-staged-only"), Is.False);
            // Seeded Baltic hull remains live; only the unapproved proposal is absent.
            Assert.That(live.Any(p => p.PlatformId == "u1"), Is.True);
            Assert.That(CountRows(connection, "platform", "platform_id = 'u-platform-staged-only'"), Is.EqualTo(0));
            Assert.That(CountRows(connection, "catalog_staging_platform", "batch_id = '" + batchId.Replace("'", "''") + "'") >= 1, Is.True);
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Test]
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
                Assert.That(decision.Committed, Is.True);
            }

            using var connection = new SqliteConnection($"Data Source={dbPath};Pooling=false");
            connection.Open();
            var live = ReadLiveWeapons(connection);
            var committed = AssertEx.Single(live, w => w.WeaponId == "test-weapon-1");
            Assert.That(committed.DisplayName, Is.EqualTo("Test Missile"));
            Assert.That(committed.MaxRangeMeters, Is.EqualTo(80_000).Within(0.5));
            Assert.That(committed.WeaponType, Is.EqualTo("Guided Weapon"));
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Test]
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
                Assert.That(gate.ApproveBatch(platformBatch, "human", "qa").Committed, Is.True);

                var mountBatch = gate.ProposeMountBatch([mount], "agent", "fitting-test");
                Assert.That(gate.ApproveBatch(mountBatch, "human", "qa").Committed, Is.True);

                var loadoutBatch = gate.ProposeLoadoutBatch([loadout], "agent", "fitting-test");
                Assert.That(gate.ApproveBatch(loadoutBatch, "human", "qa").Committed, Is.True);

                var magazineBatch = gate.ProposeMagazineBatch([magazine], "agent", "fitting-test");
                Assert.That(gate.ApproveBatch(magazineBatch, "human", "qa").Committed, Is.True);

                var commsBatch = gate.ProposeCommsBatch([comms], "agent", "fitting-test");
                Assert.That(gate.ApproveBatch(commsBatch, "human", "qa").Committed, Is.True);
            }

            using var connection = new SqliteConnection($"Data Source={dbPath};Pooling=false");
            connection.Open();

            var mounts = ReadLiveMounts(connection);
            var committedMount = AssertEx.Single(mounts, m => m.MountId == "vls-fore");
            Assert.That(committedMount.MountType, Is.EqualTo("vls"));
            Assert.That(committedMount.Capacity, Is.EqualTo(8));

            var loadouts = ReadLiveLoadouts(connection);
            var committedLoadout = AssertEx.Single(loadouts, l => l.LoadoutId == "asuw-default");
            Assert.That(committedLoadout.LoadoutName, Is.EqualTo("ASUW Default"));
            Assert.That(committedLoadout.IsDefault, Is.True);

            var magazines = ReadLiveMagazines(connection);
            var committedMagazine = AssertEx.Single(magazines, m => m.WeaponId == "rim-66");
            Assert.That(committedMagazine.Quantity, Is.EqualTo(16));

            var commsRows = ReadLiveComms(connection);
            var committedComms = AssertEx.Single(commsRows, c => c.LinkId == "link-1");
            Assert.That(committedComms.Role, Is.EqualTo("txrx"));
            Assert.That(committedComms.SatcomCapable, Is.True);
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Test]
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

                Assert.That(gate.RejectBatch(platformBatch, "human", "qa").Committed, Is.False);
                Assert.That(gate.RejectBatch(weaponBatch, "human", "qa").Committed, Is.False);
                Assert.That(gate.RejectBatch(mountBatch, "human", "qa").Committed, Is.False);
                Assert.That(gate.RejectBatch(loadoutBatch, "human", "qa").Committed, Is.False);
                Assert.That(gate.RejectBatch(magazineBatch, "human", "qa").Committed, Is.False);
                Assert.That(gate.RejectBatch(commsBatch, "human", "qa").Committed, Is.False);
                Assert.That(gate.RejectBatch(linkBatch, "human", "qa").Committed, Is.False);
            }

            using var connection = new SqliteConnection($"Data Source={dbPath};Pooling=false");
            connection.Open();
            Assert.That(CountRows(connection, "catalog_staging_platform"), Is.EqualTo(0));
            Assert.That(CountRows(connection, "catalog_staging_weapon"), Is.EqualTo(0));
            Assert.That(CountRows(connection, "catalog_staging_mount"), Is.EqualTo(0));
            Assert.That(CountRows(connection, "catalog_staging_loadout"), Is.EqualTo(0));
            Assert.That(CountRows(connection, "catalog_staging_magazine"), Is.EqualTo(0));
            Assert.That(CountRows(connection, "catalog_staging_comms"), Is.EqualTo(0));
            Assert.That(CountRows(connection, "catalog_staging_link"), Is.EqualTo(0));
            Assert.That(CountRows(connection, "platform", "platform_id = 'u-reject'"), Is.EqualTo(0));
            Assert.That(CountRows(connection, "weapon_catalog", "weapon_id = 'w-reject'"), Is.EqualTo(0));
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Test]
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
                Assert.That(gate.ApproveBatch(batchId, "human", "qa").Committed, Is.True);
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
            Assert.That(count >= 1, Is.True);
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

    private static (double LatDeg, double LonDeg, double CombatRadiusNm) ReadLiveCorePosition(
        SqliteConnection connection,
        string platformId)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText =
            """
            SELECT lat_deg, lon_deg, combat_radius_nm
            FROM platform
            WHERE platform_id = $id
            LIMIT 1
            """;
        cmd.Parameters.AddWithValue("$id", platformId);
        using var reader = cmd.ExecuteReader();
        Assert.That(reader.Read(), Is.True, $"expected live platform row for '{platformId}'");
        return (reader.GetDouble(0), reader.GetDouble(1), reader.GetDouble(2));
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