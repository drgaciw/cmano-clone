using ProjectAegis.Data.Catalog;
using Xunit;

namespace ProjectAegis.Data.Tests.Catalog;

public sealed class InMemoryCatalogReaderTests
{
    [Fact]
    public void GetSortedSensorBindings_orders_by_platform_then_sensor_ordinal()
    {
        var reader = new InMemoryCatalogReader(
        [
            new CatalogSensorBinding("u2", "radar-b", 0.5),
            new CatalogSensorBinding("u1", "radar-b", 0.6),
            new CatalogSensorBinding("u1", "radar-a", 0.7),
        ]);

        var bindings = reader.GetSortedSensorBindings();
        Assert.Equal("u1", bindings[0].PlatformId);
        Assert.Equal("radar-a", bindings[0].SensorId);
        Assert.Equal("u1", bindings[1].PlatformId);
        Assert.Equal("radar-b", bindings[1].SensorId);
        Assert.Equal("u2", bindings[2].PlatformId);
    }

    [Fact]
    public void Baltic_fixture_resolves_radar_basePd()
    {
        var reader = InMemoryCatalogReader.BalticPatrolFixture();
        Assert.True(reader.TryGetBasePd("u1", "radar-1", out var basePd));
        Assert.Equal(1.0, basePd);
    }

    [Fact]
    public void Baltic_magazine_fixture_exposes_sorted_loadout_and_magazine_rows()
    {
        var reader = InMemoryCatalogReader.BalticMagazineFixture(magazineQuantity: 16);

        Assert.Single(reader.GetSortedLoadouts());
        Assert.Equal("asuw-default", reader.GetSortedLoadouts()[0].LoadoutId);
        Assert.True(reader.GetSortedLoadouts()[0].IsDefault);

        Assert.Single(reader.GetSortedMagazines());
        Assert.Equal(16, reader.GetSortedMagazines()[0].Quantity);
    }
}