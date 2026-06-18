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
        Assert.That(rows[1].PlatformId, Is.EqualTo("b-platform"));
        Assert.That(rows[1].MaxHp, Is.EqualTo(500));
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
}