using Microsoft.Data.Sqlite;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.WriteGate;
using NUnit.Framework;

namespace ProjectAegis.Data.Tests.Catalog;

/// <summary>S33-02 / DBI-1.5 dependency graph index and commit invalidation.</summary>
[Runner.CatalogSqliteCollection]
public sealed class DependencyGraphIndexTests
{
    [Test]
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

        Assert.That(            edges.Select(e => (e.PlatformId, e.MountId, e.WeaponId, e.SensorId)).ToArray(), Is.EqualTo(            [
                ("u1", "", "", "sensor-a"),
                ("u1", "", "", "sensor-b"),
                ("u1", "mount-a", "", ""),
                ("u1", "mount-a", "weapon-a", ""),
                ("u1", "mount-a", "weapon-z", ""),
                ("u1", "mount-b", "", ""),
                ("u2", "mount-b", "", ""),
            ]));
    }

    [Test]
    public void DependencyGraph_BuildFrom_emits_platform_mount_edges()
    {
        var edges = CatalogDependencyGraphIndex.BuildFrom(
            [new CatalogMount("u1", "vls-fwd", ReviewState: CatalogReviewStates.Approved)],
            [],
            []);

        var mountEdge = AssertEx.Single(edges);
        Assert.That(mountEdge.Kind, Is.EqualTo(CatalogDependencyEdgeKind.PlatformToMount));
        Assert.That(mountEdge.PlatformId, Is.EqualTo("u1"));
        Assert.That(mountEdge.MountId, Is.EqualTo("vls-fwd"));
        Assert.That(mountEdge.WeaponId, Is.EqualTo(""));
        Assert.That(mountEdge.SensorId, Is.EqualTo(""));
    }

    [Test]
    public void DependencyGraph_BuildFrom_emits_platform_mount_weapon_chain_edges()
    {
        var edges = CatalogDependencyGraphIndex.BuildFrom(
            [new CatalogMount("u1", "vls-fwd", ReviewState: CatalogReviewStates.Approved)],
            [new CatalogMagazineEntry("u1", "asuw-default", "vls-fwd", CatalogWeaponIds.MvpDefault, 8)],
            []);

        Assert.That(edges.Count, Is.EqualTo(2));
        Assert.That(edges.Any(e => e.Kind == CatalogDependencyEdgeKind.PlatformToMount), Is.True);
        var weaponEdge = AssertEx.Single(edges, e => e.Kind == CatalogDependencyEdgeKind.PlatformToMountToWeapon);
        Assert.That(weaponEdge.PlatformId, Is.EqualTo("u1"));
        Assert.That(weaponEdge.MountId, Is.EqualTo("vls-fwd"));
        Assert.That(weaponEdge.WeaponId, Is.EqualTo(CatalogWeaponIds.MvpDefault));
        Assert.That(weaponEdge.SensorId, Is.EqualTo(""));
    }

    [Test]
    public void DependencyGraph_BuildFrom_emits_platform_sensor_edges()
    {
        var edges = CatalogDependencyGraphIndex.BuildFrom(
            [],
            [],
            [new CatalogSensorBinding("u1", "radar-1", 1.0, ReviewState: CatalogReviewStates.Approved)]);

        var sensorEdge = AssertEx.Single(edges);
        Assert.That(sensorEdge.Kind, Is.EqualTo(CatalogDependencyEdgeKind.PlatformToSensor));
        Assert.That(sensorEdge.PlatformId, Is.EqualTo("u1"));
        Assert.That(sensorEdge.MountId, Is.EqualTo(""));
        Assert.That(sensorEdge.WeaponId, Is.EqualTo(""));
        Assert.That(sensorEdge.SensorId, Is.EqualTo("radar-1"));
    }

    [Test]
    public void DependencyGraph_BuildFrom_excludes_rejected_mounts()
    {
        var edges = CatalogDependencyGraphIndex.BuildFrom(
            [new CatalogMount("u1", "vls-fwd", ReviewState: CatalogReviewStates.Rejected)],
            [new CatalogMagazineEntry("u1", "asuw-default", "vls-fwd", CatalogWeaponIds.MvpDefault, 4)],
            []);

        Assert.That(edges, Is.Empty);
    }

    [Test]
    public void DependencyGraph_BuildFrom_excludes_rejected_sensors()
    {
        var edges = CatalogDependencyGraphIndex.BuildFrom(
            [],
            [],
            [new CatalogSensorBinding("u1", "radar-1", 1.0, ReviewState: CatalogReviewStates.Rejected)]);

        Assert.That(edges, Is.Empty);
    }

    [Test]
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

        Assert.That(edges.Any(e => e.Kind == CatalogDependencyEdgeKind.PlatformToSensor), Is.True);
        Assert.That(edges.Any(e =>
            e.Kind == CatalogDependencyEdgeKind.PlatformToMountToWeapon &&
            e.PlatformId == "u1" &&
            e.MountId == "vls-fwd" &&
            e.WeaponId == CatalogWeaponIds.MvpDefault), Is.True);
    }

    [Test]
    public void DependencyGraph_BalticPatrolFixture_exposes_sensor_edges_only()
    {
        var reader = InMemoryCatalogReader.BalticPatrolFixture();
        var edges = reader.GetSortedDependencyEdges();

        Assert.That(edges.Count, Is.EqualTo(4));
        foreach (var e in edges)
{
Assert.That(e.Kind, Is.EqualTo(CatalogDependencyEdgeKind.PlatformToSensor));
}
        Assert.That(            edges.Select(e => e.SensorId).ToArray(), Is.EqualTo(            [
                "radar-1",
                "radar-2",
                CatalogSwarmPlatformDefaults.DefaultSensorId,
                CatalogSwarmPlatformDefaults.UsnCecSensorId,
            ]));
        Assert.That(edges.Any(e => e.PlatformId == CatalogSwarmPlatformDefaults.GenericSwarmPlatformId), Is.True);
        Assert.That(edges.Any(e => e.PlatformId == CatalogSwarmPlatformDefaults.UsnCecSwarmPlatformId), Is.True);
    }

    [Test]
    public void DependencyGraph_BuildFrom_is_deterministic_across_rebuilds()
    {
        CatalogMount[] mounts = [new CatalogMount("u1", "mount-a", ReviewState: CatalogReviewStates.Approved)];
        CatalogMagazineEntry[] magazines = [new CatalogMagazineEntry("u1", "loadout", "mount-a", "weapon-a", 2)];
        CatalogSensorBinding[] sensors = [new CatalogSensorBinding("u1", "radar-a", 0.8)];

        var first = CatalogDependencyGraphIndex.BuildFrom(mounts, magazines, sensors);
        var second = CatalogDependencyGraphIndex.BuildFrom(mounts, magazines, sensors);

        Assert.That(second.Count, Is.EqualTo(first.Count));
        for (var i = 0; i < first.Count; i++)
        {
            Assert.That(second[i], Is.EqualTo(first[i]));
        }
    }

    [Test]
    public void DependencyGraph_NullCatalogReader_returns_empty_edges()
    {
        ICatalogReader reader = NullCatalogReader.Instance;
        var edges = reader.GetSortedDependencyEdges();
        Assert.That(edges, Is.Empty);
    }

    [Test]
    public void DependencyGraph_SqliteCatalogReader_caches_edges_until_invalidated()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-dep-graph-cache-{Guid.NewGuid():N}.db");
        try
        {
            CatalogSeedBootstrap.SeedBalticPatrol(dbPath, overwrite: true);
            using var reader = new SqliteCatalogReader(dbPath, "dep-graph-cache");

            var first = reader.GetSortedDependencyEdges();
            var second = reader.GetSortedDependencyEdges();
            Assert.That(second, Is.SameAs(first));

            CatalogDependencyGraphCacheInvalidator.InvalidateForDatabase(dbPath);
            var third = reader.GetSortedDependencyEdges();
            Assert.That(third, Is.Not.SameAs(first));
            Assert.That(third, Is.EqualTo(first));
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
    public void DependencyGraph_ApproveBatch_mount_commit_invalidates_reader_cache()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-dep-graph-commit-{Guid.NewGuid():N}.db");
        try
        {
            CatalogSeedBootstrap.SeedBalticPatrol(dbPath, overwrite: true);
            using var reader = new SqliteCatalogReader(dbPath, "dep-graph-commit");

            var before = reader.GetSortedDependencyEdges();
            // Seed already has vls-fwd/gun-76; assert a new mount id is absent pre-commit.
            Assert.That(before.Any(e =>
                e.Kind == CatalogDependencyEdgeKind.PlatformToMount &&
                e.MountId == "vls-aft"), Is.False);

            using (var gate = new CatalogWriteGate(dbPath, new FixedCatalogClock(33002)))
            {
                var mount = new CatalogMount("u1", "vls-aft", MountType: "vls", ReviewState: CatalogReviewStates.Approved);
                var batchId = gate.ProposeMountBatch([mount], "agent", "dep-graph-test");
                var decision = gate.ApproveBatch(batchId, "human", "qa-reviewer");
                Assert.That(decision.Committed, Is.True);
            }

            var after = reader.GetSortedDependencyEdges();
            Assert.That(after, Is.Not.SameAs(before));
            Assert.That(after.Any(e =>
                e.Kind == CatalogDependencyEdgeKind.PlatformToMount &&
                e.PlatformId == "u1" &&
                e.MountId == "vls-aft"), Is.True);
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
                Assert.That(decision.Committed, Is.True);
            }

            var after = reader.GetSortedDependencyEdges();
            Assert.That(after.Count, Is.EqualTo(beforeCount + 1));
            Assert.That(after.Any(e => e.SensorId == "radar-new"), Is.True);
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

    // S36-03: PlatformToLink edges + tests + invalidation coverage
    [Test]
    public void DependencyGraph_BuildFrom_emits_platform_link_edges_for_approved_comms()
    {
        var links = new[]
        {
            new CatalogLinkEntry("NATO_TADIL_J"),
            new CatalogLinkEntry("SATCOM_B"),
        };
        var comms = new[]
        {
            new CatalogCommsBinding("u1", "NATO_TADIL_J", ReviewState: CatalogReviewStates.Approved),
            new CatalogCommsBinding("u1", "SATCOM_B", ReviewState: CatalogReviewStates.Approved),
        };

        var edges = CatalogDependencyGraphIndex.BuildFrom([], [], [], comms, links);

        Assert.That(edges.Count, Is.EqualTo(2));
        foreach (var e in edges)
{
Assert.That(e.Kind, Is.EqualTo(CatalogDependencyEdgeKind.PlatformToLink));
}
        Assert.That(edges.Any(e => e.PlatformId == "u1" && e.LinkId == "NATO_TADIL_J" && e.CommsFittingId == CatalogSortKeyComparer.FormatCommsKey(new CatalogCommsBinding("u1", "NATO_TADIL_J"))), Is.True);
        Assert.That(edges.Any(e => e.PlatformId == "u1" && e.LinkId == "SATCOM_B"), Is.True);
    }

    [Test]
    public void DependencyGraph_BuildFrom_excludes_rejected_comms_and_orphan_links()
    {
        var links = new[] { new CatalogLinkEntry("NATO_TADIL_J") };
        var comms = new[]
        {
            new CatalogCommsBinding("u1", "NATO_TADIL_J", ReviewState: CatalogReviewStates.Rejected),
            new CatalogCommsBinding("u1", "ORPHAN_LINK", ReviewState: CatalogReviewStates.Approved),
        };

        var edges = CatalogDependencyGraphIndex.BuildFrom([], [], [], comms, links);

        Assert.That(edges, Is.Empty);
    }

    [Test]
    public void DependencyGraph_BuildFrom_unifies_sort_keys_including_link_edges()
    {
        var links = new[] { new CatalogLinkEntry("L1") };
        var comms = new[] { new CatalogCommsBinding("p2", "L1", ReviewState: CatalogReviewStates.Approved) };
        var mounts = new[] { new CatalogMount("p1", "m1", ReviewState: CatalogReviewStates.Approved) };

        var edges = CatalogDependencyGraphIndex.BuildFrom(mounts, [], [], comms, links);

        // mount first (p1), then link (p2) by platform ordinal
        Assert.That(edges.Count, Is.EqualTo(2));
        Assert.That(edges[0].PlatformId, Is.EqualTo("p1"));
        Assert.That(edges[0].Kind, Is.EqualTo(CatalogDependencyEdgeKind.PlatformToMount));
        Assert.That(edges[1].PlatformId, Is.EqualTo("p2"));
        Assert.That(edges[1].Kind, Is.EqualTo(CatalogDependencyEdgeKind.PlatformToLink));
    }

    [Test]
    public void DependencyGraph_BuildFrom_is_deterministic_with_link_edges()
    {
        var links = new[] { new CatalogLinkEntry("L1") };
        var comms = new[] { new CatalogCommsBinding("u1", "L1", ReviewState: CatalogReviewStates.Approved) };

        var first = CatalogDependencyGraphIndex.BuildFrom([], [], [], comms, links);
        var second = CatalogDependencyGraphIndex.BuildFrom([], [], [], comms, links);

        Assert.That(second.Count, Is.EqualTo(first.Count));
        for (var i = 0; i < first.Count; i++)
        {
            Assert.That(second[i], Is.EqualTo(first[i]));
        }
    }

    // S37-03: full kill-chain surfacing (platform→link + weapon→mount→sensor chains + API)
    [Test]
    public void DependencyGraph_BuildFullKillChain_emits_platform_link_plus_weapon_mount_sensor_chains()
    {
        var mounts = new[]
        {
            new CatalogMount("u1", "vls-fwd", ReviewState: CatalogReviewStates.Approved),
        };
        var magazines = new[]
        {
            new CatalogMagazineEntry("u1", "asuw-default", "vls-fwd", CatalogWeaponIds.MvpDefault, 4),
        };
        var sensors = new[]
        {
            new CatalogSensorBinding("u1", "radar-1", 1.0, ReviewState: CatalogReviewStates.Approved),
        };
        var links = new[] { new CatalogLinkEntry("NATO_TADIL_J") };
        var comms = new[]
        {
            new CatalogCommsBinding("u1", "NATO_TADIL_J", ReviewState: CatalogReviewStates.Approved),
        };

        // Use new S37-03 full API
        var edgesViaFull = CatalogDependencyGraphIndex.BuildFullKillChain(mounts, magazines, sensors, comms, links);
        var edgesViaReader = CatalogDependencyGraphIndex.BuildFullKillChain(
            new InMemoryCatalogReader(
                bindings: sensors,
                mounts: mounts,
                magazines: magazines,
                comms: comms,
                links: links)
        );

        Assert.That(edgesViaFull.Count, Is.EqualTo(4)); // 1 mount + 1 weapon + 1 sensor + 1 link (link uses comms key)
        Assert.That(edgesViaReader.Count, Is.EqualTo(edgesViaFull.Count));

        var kinds = edgesViaFull.Select(e => e.Kind).ToHashSet();
        Assert.That(kinds, Does.Contain(CatalogDependencyEdgeKind.PlatformToMount));
        Assert.That(kinds, Does.Contain(CatalogDependencyEdgeKind.PlatformToMountToWeapon));
        Assert.That(kinds, Does.Contain(CatalogDependencyEdgeKind.PlatformToSensor));
        Assert.That(kinds, Does.Contain(CatalogDependencyEdgeKind.PlatformToLink));

        // Deterministic: full chains identical across calls
        var second = CatalogDependencyGraphIndex.BuildFullKillChain(mounts, magazines, sensors, comms, links);
        Assert.That(second.Count, Is.EqualTo(edgesViaFull.Count));
        for (int i = 0; i < edgesViaFull.Count; i++)
        {
            Assert.That(second[i], Is.EqualTo(edgesViaFull[i]));
        }
    }
}