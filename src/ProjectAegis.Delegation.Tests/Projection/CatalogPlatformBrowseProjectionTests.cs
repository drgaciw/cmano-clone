using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Platform;
using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

[TestFixture]
public sealed class CatalogPlatformBrowseProjectionTests
{
    [Test]
    public void FromExportData_sorts_platforms_and_joins_phase_b_rows()
    {
        var data = new PlatformCatalogExportData(
            Platforms:
            [
                new CatalogPlatformEntry("b-platform", 55.0, 18.0, 120),
                new CatalogPlatformEntry("a-platform", 54.0, 17.0, 80),
            ],
            Sensors: [],
            Mounts: [],
            Loadouts: [],
            Magazines: [],
            Comms: [],
            Mobility: [new CatalogMobility("a-platform", MaxSpeedKnots: 30)],
            Damage: [new CatalogPlatformDamage("b-platform", MaxHp: 500)]);

        var rows = CatalogPlatformBrowseProjection.FromExportData(data);

        Assert.That(rows.Count, Is.EqualTo(2));
        Assert.That(rows[0].PlatformId, Is.EqualTo("a-platform"));
        Assert.That(rows[0].MaxSpeedKnots, Is.EqualTo(30));
        Assert.That(rows[0].MountCount, Is.EqualTo(0));
        Assert.That(rows[0].SensorCount, Is.EqualTo(0));
        Assert.That(rows[1].PlatformId, Is.EqualTo("b-platform"));
        Assert.That(rows[1].MaxHp, Is.EqualTo(500));
        Assert.That(rows[1].MountCount, Is.EqualTo(0));
        Assert.That(rows[1].SensorCount, Is.EqualTo(0));
    }

    [Test]
    public void FromExportData_counts_mounts_and_sensors_per_platform()
    {
        var data = new PlatformCatalogExportData(
            Platforms:
            [
                new CatalogPlatformEntry("a-platform", 54.0, 17.0, 80),
                new CatalogPlatformEntry("b-platform", 55.0, 18.0, 120),
            ],
            Sensors:
            [
                new CatalogSensorBinding("a-platform", "radar-1", 0.5),
                new CatalogSensorBinding("a-platform", "radar-2", 0.6),
                new CatalogSensorBinding("b-platform", "radar-3", 0.7),
            ],
            Mounts:
            [
                new CatalogMount("a-platform", "mount-1"),
                new CatalogMount("b-platform", "mount-2"),
                new CatalogMount("b-platform", "mount-3"),
            ],
            Loadouts: [],
            Magazines: [],
            Comms: []);

        var rows = CatalogPlatformBrowseProjection.FromExportData(data);

        Assert.That(rows[0].PlatformId, Is.EqualTo("a-platform"));
        Assert.That(rows[0].SensorCount, Is.EqualTo(2));
        Assert.That(rows[0].MountCount, Is.EqualTo(1));
        Assert.That(rows[1].PlatformId, Is.EqualTo("b-platform"));
        Assert.That(rows[1].SensorCount, Is.EqualTo(1));
        Assert.That(rows[1].MountCount, Is.EqualTo(2));
    }

    [Test]
    public void FromReader_uses_sorted_distinct_platform_ids()
    {
        var reader = new InMemoryCatalogReader(
            bindings:
            [
                new CatalogSensorBinding("z-ship", "radar-1", 0.5),
                new CatalogSensorBinding("a-ship", "radar-2", 0.6),
            ],
            platforms:
            [
                new CatalogPlatformEntry("z-ship", 1, 2, 10),
                new CatalogPlatformEntry("a-ship", 3, 4, 20),
            ]);

        var rows = CatalogPlatformBrowseProjection.FromReader(reader);

        Assert.That(rows.Select(r => r.PlatformId).ToArray(), Is.EqualTo(new[] { "a-ship", "z-ship" }));
    }

    [Test]
    public void FromReader_counts_match_sorted_reader_queries()
    {
        var reader = new InMemoryCatalogReader(
            bindings:
            [
                new CatalogSensorBinding("u1", "radar-1", 1.0),
                new CatalogSensorBinding("u1", "radar-2", 0.75),
                new CatalogSensorBinding("hostile-1", "radar-1", 0.5),
            ],
            platforms: CatalogValidationDefaults.BalticPlatforms(),
            mounts:
            [
                new CatalogMount("u1", "mount-a"),
                new CatalogMount("hostile-1", "mount-b"),
                new CatalogMount("hostile-1", "mount-c"),
            ]);

        var expectedSensorCounts = reader.GetSortedSensorBindings()
            .GroupBy(b => b.PlatformId, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);
        var expectedMountCounts = reader.GetSortedMounts()
            .GroupBy(m => m.PlatformId, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);

        var rows = CatalogPlatformBrowseProjection.FromReader(reader);

        Assert.That(rows.Single(r => r.PlatformId == "u1").SensorCount, Is.EqualTo(expectedSensorCounts["u1"]));
        Assert.That(rows.Single(r => r.PlatformId == "u1").MountCount, Is.EqualTo(expectedMountCounts["u1"]));
        Assert.That(rows.Single(r => r.PlatformId == "hostile-1").SensorCount, Is.EqualTo(expectedSensorCounts["hostile-1"]));
        Assert.That(rows.Single(r => r.PlatformId == "hostile-1").MountCount, Is.EqualTo(expectedMountCounts["hostile-1"]));
        Assert.That(rows.Select(r => r.PlatformId).ToArray(), Is.EqualTo(new[] { "hostile-1", "u1" }));
    }

    [Test]
    public void FromReader_platform_with_zero_mounts_has_zero_mount_count()
    {
        var reader = new InMemoryCatalogReader(
            bindings: [new CatalogSensorBinding("solo-ship", "radar-1", 1.0)],
            platforms: [new CatalogPlatformEntry("solo-ship", 1, 2, 10)]);

        var row = CatalogPlatformBrowseProjection.FromReader(reader).Single();

        Assert.That(row.MountCount, Is.EqualTo(0));
        Assert.That(row.SensorCount, Is.EqualTo(1));
    }
}