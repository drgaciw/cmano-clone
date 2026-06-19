using Microsoft.Data.Sqlite;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.WriteGate;
using Xunit;

namespace ProjectAegis.Data.Tests.Catalog;

/// <summary>S33-02 / DBI-1.5 dependency graph index and commit invalidation.</summary>
[Collection("CatalogSqlite")]
public sealed class DependencyGraphIndexTests
{
    [Fact]
    public void DependencyGraph_BuildFrom_orders_edges_by_stable_sort_key()
    {
        var edges = CatalogDependencyGraphIndex.BuildFrom(
            mounts:
            [
                new CatalogMount("u2", "mount-b", ReviewState: CatalogReviewStates.Approved),
                new CatalogMount("u1", "mount-b", ReviewState: CatalogReviewStates.Approved),
                new CatalogMount("u1", "mount-a", ReviewState: CatalogReviewStates.Approved),
            ],
            magazines:
            [
                new CatalogMagazineEntry("u1", "loadout-a", "mount-a", "weapon-z"),
                new CatalogMagazineEntry("u1", "loadout-a", "mount-a", "weapon-a"),
            ],
            sensors:
            [
                new CatalogSensorBinding("u1", "sensor-b", 0.5),
                new CatalogSensorBinding("u1", "sensor-a", 0.6),
            ]);

        Assert.Equal(
            [
                ("u1", "", "", "sensor-a"),
                ("u1", "", "", "sensor-b"),
                ("u1", "mount-a", "", ""),
                ("u1", "mount-a", "weapon-a", ""),
                ("u1", "mount-a", "weapon-z", ""),
                ("u1", "mount-b", "", ""),
                ("u2", "mount-b", "", ""),
            ],
            edges.Select(e => (e.PlatformId, e.MountId, e.WeaponId, e.SensorId)).ToArray());
    }

    [Fact]
    public void DependencyGraph_BuildFrom_emits_platform_mount_edges()
    {
        var edges = CatalogDependencyGraphIndex.BuildFrom(
            [new CatalogMount("u1", "vls-fwd", ReviewState: CatalogReviewStates.Approved)],
            [],
            []);

        var mountEdge = Assert.Single(edges);
        Assert.Equal(CatalogDependencyEdgeKind.PlatformToMount, mountEdge.Kind);
        Assert.Equal("u1", mountEdge.PlatformId);
        Assert.Equal("vls-fwd", mountEdge.MountId);
        Assert.Equal("", mountEdge.WeaponId);
        Assert.Equal("", mountEdge.SensorId);
    }

    [Fact]
    public void DependencyGraph_BuildFrom_emits_platform_mount_weapon_chain_edges()
    {
        var edges = CatalogDependencyGraphIndex.BuildFrom(
            [new CatalogMount("u1", "vls-fwd", ReviewState: CatalogReviewStates.Approved)],
            [new CatalogMagazineEntry("u1", "asuw-default", "vls-fwd", CatalogWeaponIds.MvpDefault, 8)],
            []);

        Assert.Equal(2, edges.Count);
        Assert.Contains(edges, e => e.Kind == CatalogDependencyEdgeKind.PlatformToMount);
        var weaponEdge = Assert.Single(edges, e => e.Kind == CatalogDependencyEdgeKind.PlatformToMountToWeapon);
        Assert.Equal("u1", weaponEdge.PlatformId);
        Assert.Equal("vls-fwd", weaponEdge.MountId);
        Assert.Equal(CatalogWeaponIds.MvpDefault, weaponEdge.WeaponId);
        Assert.Equal("", weaponEdge.SensorId);
    }

    [Fact]
    public void DependencyGraph_BuildFrom_emits_platform_sensor_edges()
    {
        var edges = CatalogDependencyGraphIndex.BuildFrom(
            [],
            [],
            [new CatalogSensorBinding("u1", "radar-1", 1.0, ReviewState: CatalogReviewStates.Approved)]);

        var sensorEdge = Assert.Single(edges);
        Assert.Equal(CatalogDependencyEdgeKind.PlatformToSensor, sensorEdge.Kind);
        Assert.Equal("u1", sensorEdge.PlatformId);
        Assert.Equal("", sensorEdge.MountId);
        Assert.Equal("", sensorEdge.WeaponId);
        Assert.Equal("radar-1", sensorEdge.SensorId);
    }

    [Fact]
    public void DependencyGraph_BuildFrom_excludes_rejected_mounts()
    {
        var edges = CatalogDependencyGraphIndex.BuildFrom(
            [new CatalogMount("u1", "vls-fwd", ReviewState: CatalogReviewStates.Rejected)],
            [new CatalogMagazineEntry("u1", "asuw-default", "vls-fwd", CatalogWeaponIds.MvpDefault, 4)],
            []);

        Assert.Empty(edges);
    }

    [Fact]
    public void DependencyGraph_BuildFrom_excludes_rejected_sensors()
    {
        var edges = CatalogDependencyGraphIndex.BuildFrom(
            [],
            [],
            [new CatalogSensorBinding("u1", "radar-1", 1.0, ReviewState: CatalogReviewStates.Rejected)]);

        Assert.Empty(edges);
    }

    [Fact]
    public void DependencyGraph_BalticMagazineFixture_exposes_weapon_chain_edges()
    {
        var reader = new InMemoryCatalogReader(
            InMemoryCatalogReader.BalticMagazineFixture(magazineQuantity: 12).GetSortedSensorBindings(),
            "p0-baltic-magazine-dep-graph",
            CatalogValidationDefaults.BalticPlatforms(),
            mounts: [new CatalogMount("u1", "vls-fwd", ReviewState: CatalogReviewStates.Approved)],
            loadouts: InMemoryCatalogReader.BalticMagazineFixture().GetSortedLoadouts(),
            magazines: InMemoryCatalogReader.BalticMagazineFixture(magazineQuantity: 12).GetSortedMagazines());
        var edges = reader.GetSortedDependencyEdges();

        Assert.Contains(edges, e => e.Kind == CatalogDependencyEdgeKind.PlatformToSensor);
        Assert.Contains(edges, e =>
            e.Kind == CatalogDependencyEdgeKind.PlatformToMountToWeapon &&
            e.PlatformId == "u1" &&
            e.MountId == "vls-fwd" &&
            e.WeaponId == CatalogWeaponIds.MvpDefault);
    }

    [Fact]
    public void DependencyGraph_BalticPatrolFixture_exposes_sensor_edges_only()
    {
        var reader = InMemoryCatalogReader.BalticPatrolFixture();
        var edges = reader.GetSortedDependencyEdges();

        Assert.Equal(2, edges.Count);
        Assert.All(edges, e => Assert.Equal(CatalogDependencyEdgeKind.PlatformToSensor, e.Kind));
        Assert.Equal(["radar-1", "radar-2"], edges.Select(e => e.SensorId).ToArray());
    }

    [Fact]
    public void DependencyGraph_BuildFrom_is_deterministic_across_rebuilds()
    {
        CatalogMount[] mounts = [new CatalogMount("u1", "mount-a", ReviewState: CatalogReviewStates.Approved)];
        CatalogMagazineEntry[] magazines = [new CatalogMagazineEntry("u1", "loadout", "mount-a", "weapon-a", 2)];
        CatalogSensorBinding[] sensors = [new CatalogSensorBinding("u1", "radar-a", 0.8)];

        var first = CatalogDependencyGraphIndex.BuildFrom(mounts, magazines, sensors);
        var second = CatalogDependencyGraphIndex.BuildFrom(mounts, magazines, sensors);

        Assert.Equal(first.Count, second.Count);
        for (var i = 0; i < first.Count; i++)
        {
            Assert.Equal(first[i], second[i]);
        }
    }

    [Fact]
    public void DependencyGraph_NullCatalogReader_returns_empty_edges()
    {
        ICatalogReader reader = NullCatalogReader.Instance;
        var edges = reader.GetSortedDependencyEdges();
        Assert.Empty(edges);
    }

    [Fact]
    public void DependencyGraph_SqliteCatalogReader_caches_edges_until_invalidated()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-dep-graph-cache-{Guid.NewGuid():N}.db");
        try
        {
            CatalogSeedBootstrap.SeedBalticPatrol(dbPath, overwrite: true);
            using var reader = new SqliteCatalogReader(dbPath, "dep-graph-cache");

            var first = reader.GetSortedDependencyEdges();
            var second = reader.GetSortedDependencyEdges();
            Assert.Same(first, second);

            CatalogDependencyGraphCacheInvalidator.InvalidateForDatabase(dbPath);
            var third = reader.GetSortedDependencyEdges();
            Assert.NotSame(first, third);
            Assert.Equal(first, third);
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
    public void DependencyGraph_ApproveBatch_mount_commit_invalidates_reader_cache()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-dep-graph-commit-{Guid.NewGuid():N}.db");
        try
        {
            CatalogSeedBootstrap.SeedBalticPatrol(dbPath, overwrite: true);
            using var reader = new SqliteCatalogReader(dbPath, "dep-graph-commit");

            var before = reader.GetSortedDependencyEdges();
            Assert.DoesNotContain(before, e => e.Kind == CatalogDependencyEdgeKind.PlatformToMount);

            using (var gate = new CatalogWriteGate(dbPath, new FixedCatalogClock(33002)))
            {
                var mount = new CatalogMount("u1", "vls-fwd", MountType: "vls", ReviewState: CatalogReviewStates.Approved);
                var batchId = gate.ProposeMountBatch([mount], "agent", "dep-graph-test");
                var decision = gate.ApproveBatch(batchId, "human", "qa-reviewer");
                Assert.True(decision.Committed);
            }

            var after = reader.GetSortedDependencyEdges();
            Assert.NotSame(before, after);
            Assert.Contains(after, e =>
                e.Kind == CatalogDependencyEdgeKind.PlatformToMount &&
                e.PlatformId == "u1" &&
                e.MountId == "vls-fwd");
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
    public void DependencyGraph_ApproveBatch_sensor_commit_refreshes_sensor_edges()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-dep-graph-sensor-{Guid.NewGuid():N}.db");
        try
        {
            CatalogSeedBootstrap.SeedBalticPatrol(dbPath, overwrite: true);
            using var reader = new SqliteCatalogReader(dbPath, "dep-graph-sensor");

            var beforeCount = reader.GetSortedDependencyEdges().Count;

            using (var gate = new CatalogWriteGate(dbPath, new FixedCatalogClock(33003)))
            {
                var sensor = new CatalogSensorBinding(
                    "u1",
                    "radar-new",
                    0.42,
                    ReviewState: CatalogReviewStates.Approved,
                    TrlLevel: 9,
                    ValueTier: CatalogProvenanceTier.InterpretedValue);
                var batchId = gate.ProposeSensorBatch([sensor], "agent", "dep-graph-sensor");
                var decision = gate.ApproveBatch(batchId, "human", "qa-reviewer");
                Assert.True(decision.Committed);
            }

            var after = reader.GetSortedDependencyEdges();
            Assert.Equal(beforeCount + 1, after.Count);
            Assert.Contains(after, e => e.SensorId == "radar-new");
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
}