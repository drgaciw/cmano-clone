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
        Assert.Equal(CatalogSwarmPlatformDefaults.GenericMaxDrones, CatalogSwarmPlatformDefaults.GenericMaxDrones);
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

    private static void ClearDb(string dbPath)
    {
        SqliteConnection.ClearAllPools();
        if (File.Exists(dbPath))
        {
            File.Delete(dbPath);
        }
    }
}
