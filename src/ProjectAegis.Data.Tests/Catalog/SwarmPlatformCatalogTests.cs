using Microsoft.Data.Sqlite;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Scenario.Authoring;
using Xunit;

namespace ProjectAegis.Data.Tests.Catalog;

/// <summary>DRG-86 / SWARM-A1: catalog schema, generic preset load, spawn integrity, scenario ref.</summary>
[Collection("CatalogSqlite")]
public sealed class SwarmPlatformCatalogTests
{
    [Fact]
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

    [Fact]
    public void Baltic_seed_loads_generic_swarm_catalog_entry()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-swarm-seed-{Guid.NewGuid():N}.db");
        try
        {
            CatalogSeedBootstrap.SeedBalticPatrol(dbPath);
            using var reader = new SqliteCatalogReader(dbPath, "swarm-seed");

            Assert.True(reader.TryGetSwarmPlatform(
                CatalogSwarmPlatformDefaults.GenericSwarmPlatformId,
                out var swarm));
            Assert.True(swarm.IsSwarm);
            Assert.Equal(CatalogSwarmPlatformDefaults.GenericMaxDrones, swarm.MaxDrones);
            Assert.Equal(CatalogSwarmPlatformDefaults.ArmorClassLightAir, swarm.ArmorClass);
            Assert.Equal(CatalogSwarmPlatformDefaults.DefaultSensorId, swarm.DefaultSensorId);
            Assert.Equal(CatalogSwarmPlatformDefaults.DefaultWeaponId, swarm.DefaultWeaponId);

            Assert.True(reader.TryGetPlatformPosition(
                CatalogSwarmPlatformDefaults.GenericSwarmPlatformId,
                out var lat,
                out var lon));
            Assert.Equal(CatalogSwarmPlatformDefaults.GenericLatDeg, lat);
            Assert.Equal(CatalogSwarmPlatformDefaults.GenericLonDeg, lon);

            Assert.True(reader.TryGetBasePd(
                CatalogSwarmPlatformDefaults.GenericSwarmPlatformId,
                CatalogSwarmPlatformDefaults.DefaultSensorId,
                out var pd));
            Assert.Equal(0.80, pd, precision: 9);

            Assert.Contains(
                reader.GetSortedSwarmPlatforms(),
                s => s.PlatformId == CatalogSwarmPlatformDefaults.GenericSwarmPlatformId);
        }
        finally
        {
            ClearDb(dbPath);
        }
    }

    [Fact]
    public void InMemory_fixture_exposes_generic_swarm_platform()
    {
        var catalog = InMemoryCatalogReader.BalticPatrolFixture();
        Assert.True(catalog.TryGetSwarmPlatform(
            CatalogSwarmPlatformDefaults.GenericSwarmPlatformId,
            out var swarm));
        Assert.Equal(40, swarm.MaxDrones);
        Assert.True(catalog.TryGetCombatRadiusNm(
            CatalogSwarmPlatformDefaults.GenericSwarmPlatformId,
            out var radius));
        Assert.Equal(CatalogSwarmPlatformDefaults.GenericCombatRadiusNm, radius);
    }

    [Fact]
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

        var unit = Assert.Single(editor.ToDto().Orbat!.Units);
        Assert.Equal(CatalogSwarmPlatformDefaults.GenericSwarmPlatformId, unit.PlatformId);
        Assert.Equal(25, unit.DroneCount);

        // Catalog ref resolves (no broken platform id for spawn path).
        Assert.True(catalog.TryGetSwarmPlatform(unit.PlatformId, out _));
        Assert.True(catalog.TryGetPlatformPosition(unit.PlatformId, out _, out _));

        Assert.True(SwarmUnitFactory.TryCreate(
            unit.Id,
            unit.PlatformId,
            catalog,
            out var integrity,
            unit.DroneCount));
        Assert.Equal("swarm-1", integrity.UnitId);
        Assert.Equal(25, integrity.DroneCount);
        Assert.Equal(40, integrity.MaxDrones);
        Assert.False(integrity.IsDestroyed);
        Assert.Equal(0.625, integrity.IntegrityFraction, precision: 9);
    }

    [Fact]
    public void SwarmUnitFactory_defaults_to_max_and_clamps()
    {
        var catalog = InMemoryCatalogReader.BalticPatrolFixture();

        var full = SwarmUnitFactory.Create(
            "s-full",
            CatalogSwarmPlatformDefaults.GenericSwarmPlatformId,
            catalog);
        Assert.Equal(40, full.DroneCount);
        Assert.Equal(40, full.MaxDrones);

        var clamped = SwarmUnitFactory.Create(
            "s-over",
            CatalogSwarmPlatformDefaults.GenericSwarmPlatformId,
            catalog,
            initialDroneCount: 999);
        Assert.Equal(40, clamped.DroneCount);

        var zero = SwarmUnitFactory.Create(
            "s-zero",
            CatalogSwarmPlatformDefaults.GenericSwarmPlatformId,
            catalog,
            initialDroneCount: 0);
        Assert.Equal(0, zero.DroneCount);
        Assert.True(zero.IsDestroyed);

        Assert.False(SwarmUnitFactory.TryCreate("x", "u1", catalog, out _));
    }

    [Fact]
    public void CatalogEntityMap_includes_swarm_platform()
    {
        Assert.True(CatalogEntityMap.TryGetTable("CatalogSwarmPlatform", out var binding));
        Assert.Equal("platform_swarm", binding.TableName);
        Assert.Equal("platform_id ASC", binding.DeterministicOrderBy);
    }

    [Fact]
    public void CatalogSwarmPlatform_defaults_match_schema_starter_tuning()
    {
        var row = new CatalogSwarmPlatform("x", MaxDrones: 10);
        Assert.True(row.IsSwarm);
        Assert.Equal(CatalogSwarmPlatformDefaults.ArmorClassLightAir, row.ArmorClass);
        Assert.Equal(CatalogReviewStates.Provisional, row.ReviewState);
        Assert.Equal(40, CatalogSwarmPlatformDefaults.GenericMaxDrones);
    }

    [Fact]
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
                    Assert.Equal(1, cmd.ExecuteNonQuery());
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
                    Assert.True(cmd.ExecuteNonQuery() >= 1);
                }
            }

            CatalogSeedBootstrap.EnsureGenericSwarmPlatform(dbPath);

            using var reader = new SqliteCatalogReader(dbPath, "swarm-preserve");
            Assert.True(reader.TryGetSwarmPlatform(
                CatalogSwarmPlatformDefaults.GenericSwarmPlatformId,
                out var swarm));
            Assert.Equal(17, swarm.MaxDrones);

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
            Assert.Equal("Curated Swarm Name", nameCmd.ExecuteScalar() as string);
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
        Assert.Equal(1, Convert.ToInt32(cmd.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture));
    }

[Fact]
    public void Phase_B_generic_is_not_cec_capable_and_usn_exemplar_is()
    {
        var generic = CatalogValidationDefaults.GenericSwarmPlatform();
        var usn = CatalogValidationDefaults.UsnCecSwarmPlatform();
        Assert.False(generic.CecCapable);
        Assert.Equal(CatalogSwarmPlatformDefaults.ModeHold, generic.DefaultMode);
        Assert.True(usn.CecCapable);
        Assert.Equal(CatalogSwarmPlatformDefaults.ModeScreen, usn.DefaultMode);
        Assert.True(usn.RequiresHost);
        Assert.Contains("ship", usn.AllowedHostClasses, StringComparison.Ordinal);
        Assert.True(string.CompareOrdinal(generic.PlatformId, usn.PlatformId) < 0);
    }

    [Fact]
    public void Baltic_seed_loads_usn_cec_swarm_with_phase_b_columns()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-swarm-b2-{Guid.NewGuid():N}.db");
        try
        {
            CatalogSeedBootstrap.SeedBalticPatrol(dbPath);
            using var reader = new SqliteCatalogReader(dbPath, "swarm-b2");

            Assert.True(reader.TryGetSwarmPlatform(
                CatalogSwarmPlatformDefaults.GenericSwarmPlatformId,
                out var generic));
            Assert.False(generic.CecCapable);

            Assert.True(reader.TryGetSwarmPlatform(
                CatalogSwarmPlatformDefaults.UsnCecSwarmPlatformId,
                out var usn));
            Assert.True(usn.CecCapable);
            Assert.Equal(CatalogSwarmPlatformDefaults.ModeScreen, usn.DefaultMode);
            Assert.True(usn.RequiresHost);

            var sorted = reader.GetSortedSwarmPlatforms();
            Assert.Contains(sorted, s => s.PlatformId == CatalogSwarmPlatformDefaults.UsnCecSwarmPlatformId);
        }
        finally
        {
            ClearDb(dbPath);
        }
    }

    [Fact]
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
            Assert.True(reader2.TryGetSwarmPlatform(
                CatalogSwarmPlatformDefaults.UsnCecSwarmPlatformId,
                out var usn));
            Assert.True(usn.CecCapable);
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
