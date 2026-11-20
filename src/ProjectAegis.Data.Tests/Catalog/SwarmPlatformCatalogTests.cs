using Microsoft.Data.Sqlite;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Scenario.Authoring;
using NUnit.Framework;

namespace ProjectAegis.Data.Tests.Catalog;

/// <summary>DRG-86 / SWARM-A1: catalog schema, generic preset load, spawn integrity, scenario ref.</summary>
[ProjectAegis.Data.Tests.Runner.CatalogSqliteCollection]
public sealed class SwarmPlatformCatalogTests
{
    [Test]
    public void Migration_012_applies_idempotently()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-swarm-mig-{Guid.NewGuid():N}.db");
        try
        {
            CatalogSeedBootstrap.SeedBalticPatrol(dbPath);
            AssertTableExists(dbPath, "platform_swarm");

            using (var reader = new SqliteCatalogReader(dbPath, "swarm-mig-idempotent"))
            {
                _ = reader.GetSortedSwarmPlatforms();
            }

            AssertTableExists(dbPath, "platform_swarm");
        }
        finally
        {
            ClearDb(dbPath);
        }
    }

    [Test]
    public void Baltic_seed_loads_generic_swarm_catalog_entry()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-swarm-seed-{Guid.NewGuid():N}.db");
        try
        {
            CatalogSeedBootstrap.SeedBalticPatrol(dbPath);
            using var reader = new SqliteCatalogReader(dbPath, "swarm-seed");

            Assert.That(reader.TryGetSwarmPlatform(
                CatalogSwarmPlatformDefaults.GenericSwarmPlatformId,
                out var swarm), Is.True);
            Assert.That(swarm.IsSwarm, Is.True);
            Assert.That(swarm.MaxDrones, Is.EqualTo(CatalogSwarmPlatformDefaults.GenericMaxDrones));
            Assert.That(swarm.ArmorClass, Is.EqualTo(CatalogSwarmPlatformDefaults.ArmorClassLightAir));
            Assert.That(swarm.DefaultSensorId, Is.EqualTo(CatalogSwarmPlatformDefaults.DefaultSensorId));
            Assert.That(swarm.DefaultWeaponId, Is.EqualTo(CatalogSwarmPlatformDefaults.DefaultWeaponId));

            Assert.That(reader.TryGetPlatformPosition(
                CatalogSwarmPlatformDefaults.GenericSwarmPlatformId,
                out var lat,
                out var lon), Is.True);
            Assert.That(lat, Is.EqualTo(CatalogSwarmPlatformDefaults.GenericLatDeg));
            Assert.That(lon, Is.EqualTo(CatalogSwarmPlatformDefaults.GenericLonDeg));

            Assert.That(reader.TryGetBasePd(
                CatalogSwarmPlatformDefaults.GenericSwarmPlatformId,
                CatalogSwarmPlatformDefaults.DefaultSensorId,
                out var pd), Is.True);
            Assert.That(pd, Is.EqualTo(0.80).Within(0.000000005));

            Assert.That(                reader.GetSortedSwarmPlatforms().Any(                s => s.PlatformId == CatalogSwarmPlatformDefaults.GenericSwarmPlatformId), Is.True);
        }
        finally
        {
            ClearDb(dbPath);
        }
    }

    [Test]
    public void InMemory_fixture_exposes_generic_swarm_platform()
    {
        var catalog = InMemoryCatalogReader.BalticPatrolFixture();
        Assert.That(catalog.TryGetSwarmPlatform(
            CatalogSwarmPlatformDefaults.GenericSwarmPlatformId,
            out var swarm), Is.True);
        Assert.That(swarm.MaxDrones, Is.EqualTo(40));
        Assert.That(catalog.TryGetCombatRadiusNm(
            CatalogSwarmPlatformDefaults.GenericSwarmPlatformId,
            out var radius), Is.True);
        Assert.That(radius, Is.EqualTo(CatalogSwarmPlatformDefaults.GenericCombatRadiusNm));
    }

    [Test]
    public void Scenario_can_reference_swarm_platform_id_and_spawn_integrity()
    {
        var catalog = InMemoryCatalogReader.BalticPatrolFixture();
        var editor = ScenarioDocumentEditor.CreateNew();
        editor.UpsertOrbatUnit(new ScenarioOrbatUnitDto
        {
            Id = "swarm-1",
            SideId = "blue",
            PlatformId = CatalogSwarmPlatformDefaults.GenericSwarmPlatformId,
            Lat = 57.1,
            Lon = 20.1,
            DroneCount = 25,
        });
        editor.CommitMutation();

        var unit = ProjectAegis.Data.Tests.AssertEx.Single(editor.ToDto().Orbat!.Units);
        Assert.That(unit.PlatformId, Is.EqualTo(CatalogSwarmPlatformDefaults.GenericSwarmPlatformId));
        Assert.That(unit.DroneCount, Is.EqualTo(25));

        // Catalog ref resolves (no broken platform id for spawn path).
        Assert.That(catalog.TryGetSwarmPlatform(unit.PlatformId, out _), Is.True);
        Assert.That(catalog.TryGetPlatformPosition(unit.PlatformId, out _, out _), Is.True);

        Assert.That(SwarmUnitFactory.TryCreate(
            unit.Id,
            unit.PlatformId,
            catalog,
            out var integrity,
            unit.DroneCount), Is.True);
        Assert.That(integrity.UnitId, Is.EqualTo("swarm-1"));
        Assert.That(integrity.DroneCount, Is.EqualTo(25));
        Assert.That(integrity.MaxDrones, Is.EqualTo(40));
        Assert.That(integrity.IsDestroyed, Is.False);
        Assert.That(integrity.IntegrityFraction, Is.EqualTo(0.625).Within(0.000000005));
    }

    [Test]
    public void SwarmUnitFactory_defaults_to_max_and_clamps()
    {
        var catalog = InMemoryCatalogReader.BalticPatrolFixture();

        var full = SwarmUnitFactory.Create(
            "s-full",
            CatalogSwarmPlatformDefaults.GenericSwarmPlatformId,
            catalog);
        Assert.That(full.DroneCount, Is.EqualTo(40));
        Assert.That(full.MaxDrones, Is.EqualTo(40));

        var clamped = SwarmUnitFactory.Create(
            "s-over",
            CatalogSwarmPlatformDefaults.GenericSwarmPlatformId,
            catalog,
            initialDroneCount: 999);
        Assert.That(clamped.DroneCount, Is.EqualTo(40));

        var zero = SwarmUnitFactory.Create(
            "s-zero",
            CatalogSwarmPlatformDefaults.GenericSwarmPlatformId,
            catalog,
            initialDroneCount: 0);
        Assert.That(zero.DroneCount, Is.EqualTo(0));
        Assert.That(zero.IsDestroyed, Is.True);

        Assert.That(SwarmUnitFactory.TryCreate("x", "u1", catalog, out _), Is.False);
    }

    [Test]
    public void CatalogEntityMap_includes_swarm_platform()
    {
        Assert.That(CatalogEntityMap.TryGetTable("CatalogSwarmPlatform", out var binding), Is.True);
        Assert.That(binding.TableName, Is.EqualTo("platform_swarm"));
        Assert.That(binding.DeterministicOrderBy, Is.EqualTo("platform_id ASC"));
    }

    [Test]
    public void CatalogSwarmPlatform_defaults_match_schema_starter_tuning()
    {
        var row = new CatalogSwarmPlatform("x", MaxDrones: 10);
        Assert.That(row.IsSwarm, Is.True);
        Assert.That(row.ArmorClass, Is.EqualTo(CatalogSwarmPlatformDefaults.ArmorClassLightAir));
        Assert.That(row.ReviewState, Is.EqualTo(CatalogReviewStates.Provisional));
        Assert.That(CatalogSwarmPlatformDefaults.GenericMaxDrones, Is.EqualTo(40));
    }

    [Test]
    public void EnsureGenericSwarmPlatform_does_not_overwrite_curated_max_drones()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-swarm-preserve-{Guid.NewGuid():N}.db");
        try
        {
            CatalogSeedBootstrap.SeedBalticPatrol(dbPath);

            using (var connection = new SqliteConnection($"Data Source={dbPath};Pooling=false"))
            {
                connection.Open();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText =
                        """
                        UPDATE platform_swarm
                        SET max_drones = 17
                        WHERE platform_id = $id
                        """;
                    cmd.Parameters.AddWithValue("$id", CatalogSwarmPlatformDefaults.GenericSwarmPlatformId);
                    Assert.That(cmd.ExecuteNonQuery(), Is.EqualTo(1));
                }

                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText =
                        """
                        UPDATE platform
                        SET display_name = 'Curated Swarm Name'
                        WHERE platform_id = $id
                        """;
                    cmd.Parameters.AddWithValue("$id", CatalogSwarmPlatformDefaults.GenericSwarmPlatformId);
                    Assert.That(cmd.ExecuteNonQuery() >= 1, Is.True);
                }
            }

            CatalogSeedBootstrap.EnsureGenericSwarmPlatform(dbPath);

            using var reader = new SqliteCatalogReader(dbPath, "swarm-preserve");
            Assert.That(reader.TryGetSwarmPlatform(
                CatalogSwarmPlatformDefaults.GenericSwarmPlatformId,
                out var swarm), Is.True);
            Assert.That(swarm.MaxDrones, Is.EqualTo(17));

            using var connection2 = new SqliteConnection($"Data Source={dbPath};Pooling=false");
            connection2.Open();
            using var nameCmd = connection2.CreateCommand();
            nameCmd.CommandText =
                """
                SELECT display_name FROM platform
                WHERE platform_id = $id
                ORDER BY snapshot_id ASC
                LIMIT 1
                """;
            nameCmd.Parameters.AddWithValue("$id", CatalogSwarmPlatformDefaults.GenericSwarmPlatformId);
            Assert.That(nameCmd.ExecuteScalar() as string, Is.EqualTo("Curated Swarm Name"));
        }
        finally
        {
            ClearDb(dbPath);
        }
    }

    private static void AssertTableExists(string dbPath, string table)
    {
        using var connection = new SqliteConnection($"Data Source={dbPath}");
        connection.Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name=$name";
        cmd.Parameters.AddWithValue("$name", table);
        Assert.That(Convert.ToInt32(cmd.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture), Is.EqualTo(1));
    }

[Test]
    public void Phase_B_generic_is_not_cec_capable_and_usn_exemplar_is()
    {
        var generic = CatalogValidationDefaults.GenericSwarmPlatform();
        var usn = CatalogValidationDefaults.UsnCecSwarmPlatform();
        Assert.That(generic.CecCapable, Is.False);
        Assert.That(generic.DefaultMode, Is.EqualTo(CatalogSwarmPlatformDefaults.ModeHold));
        Assert.That(usn.CecCapable, Is.True);
        Assert.That(usn.DefaultMode, Is.EqualTo(CatalogSwarmPlatformDefaults.ModeScreen));
        Assert.That(usn.RequiresHost, Is.True);
        Assert.That(usn.AllowedHostClasses, Does.Contain("ship"));
        Assert.That(string.CompareOrdinal(generic.PlatformId, usn.PlatformId) < 0, Is.True);
    }

    [Test]
    public void Baltic_seed_loads_usn_cec_swarm_with_phase_b_columns()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-swarm-b2-{Guid.NewGuid():N}.db");
        try
        {
            CatalogSeedBootstrap.SeedBalticPatrol(dbPath);
            using var reader = new SqliteCatalogReader(dbPath, "swarm-b2");

            Assert.That(reader.TryGetSwarmPlatform(
                CatalogSwarmPlatformDefaults.GenericSwarmPlatformId,
                out var generic), Is.True);
            Assert.That(generic.CecCapable, Is.False);

            Assert.That(reader.TryGetSwarmPlatform(
                CatalogSwarmPlatformDefaults.UsnCecSwarmPlatformId,
                out var usn), Is.True);
            Assert.That(usn.CecCapable, Is.True);
            Assert.That(usn.DefaultMode, Is.EqualTo(CatalogSwarmPlatformDefaults.ModeScreen));
            Assert.That(usn.RequiresHost, Is.True);

            var sorted = reader.GetSortedSwarmPlatforms();
            Assert.That(sorted.Any(s => s.PlatformId == CatalogSwarmPlatformDefaults.UsnCecSwarmPlatformId), Is.True);
        }
        finally
        {
            ClearDb(dbPath);
        }
    }

    [Test]
    public void Migration_013_is_idempotent_with_cec_column()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-swarm-013-{Guid.NewGuid():N}.db");
        try
        {
            CatalogSeedBootstrap.SeedBalticPatrol(dbPath);
            using (var reader = new SqliteCatalogReader(dbPath, "swarm-013-a"))
            {
                _ = reader.GetSortedSwarmPlatforms();
            }

            using var reader2 = new SqliteCatalogReader(dbPath, "swarm-013-b");
            Assert.That(reader2.TryGetSwarmPlatform(
                CatalogSwarmPlatformDefaults.UsnCecSwarmPlatformId,
                out var usn), Is.True);
            Assert.That(usn.CecCapable, Is.True);
        }
        finally
        {
            ClearDb(dbPath);
        }
    }

    private static void ClearDb(string dbPath)
    {
        SqliteConnection.ClearAllPools();
        if (File.Exists(dbPath))
        {
            File.Delete(dbPath);
        }
    }
}
