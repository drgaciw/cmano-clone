using ProjectAegis.Data.Catalog;
using NUnit.Framework;

namespace ProjectAegis.Data.Tests.Catalog;

public sealed class InMemoryCatalogReaderTests
{
    [Test]
    public void GetSortedSensorBindings_orders_by_platform_then_sensor_ordinal()
    {
        var reader = new InMemoryCatalogReader(
        [
            new CatalogSensorBinding("u2", "radar-b", 0.5),
            new CatalogSensorBinding("u1", "radar-b", 0.6),
            new CatalogSensorBinding("u1", "radar-a", 0.7),
        ]);

        var bindings = reader.GetSortedSensorBindings();
        Assert.That(bindings[0].PlatformId, Is.EqualTo("u1"));
        Assert.That(bindings[0].SensorId, Is.EqualTo("radar-a"));
        Assert.That(bindings[1].PlatformId, Is.EqualTo("u1"));
        Assert.That(bindings[1].SensorId, Is.EqualTo("radar-b"));
        Assert.That(bindings[2].PlatformId, Is.EqualTo("u2"));
    }

    [Test]
    public void Baltic_fixture_resolves_radar_basePd()
    {
        var reader = InMemoryCatalogReader.BalticPatrolFixture();
        Assert.That(reader.TryGetBasePd("u1", "radar-1", out var basePd), Is.True);
        Assert.That(basePd, Is.EqualTo(1.0));
    }

    [Test]
    public void Baltic_magazine_fixture_exposes_sorted_loadout_and_magazine_rows()
    {
        var reader = InMemoryCatalogReader.BalticMagazineFixture(magazineQuantity: 16);

        Assert.That(reader.GetSortedLoadouts(), Has.Exactly(1).Items);
        Assert.That(reader.GetSortedLoadouts()[0].LoadoutId, Is.EqualTo("asuw-default"));
        Assert.That(reader.GetSortedLoadouts()[0].IsDefault, Is.True);

        Assert.That(reader.GetSortedMagazines(), Has.Exactly(1).Items);
        Assert.That(reader.GetSortedMagazines()[0].Quantity, Is.EqualTo(16));
    }
}