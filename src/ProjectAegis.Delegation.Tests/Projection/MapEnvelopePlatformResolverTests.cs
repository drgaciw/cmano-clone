using ProjectAegis.Data.Catalog;
using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

public sealed class MapEnvelopePlatformResolverTests
{
    [Test]
    public void Resolve_null_catalog_returns_unit_id()
    {
        Assert.That(MapEnvelopePlatformResolver.Resolve(null, "u1"), Is.EqualTo("u1"));
    }

    [Test]
    public void Resolve_direct_platform_match_returns_unit_id()
    {
        var catalog = InMemoryCatalogReader.BalticPatrolFixture();
        Assert.That(MapEnvelopePlatformResolver.Resolve(catalog, "u1"), Is.EqualTo("u1"));
    }

    [Test]
    public void Resolve_prefixed_unit_maps_to_catalog_platform()
    {
        var catalog = new InMemoryCatalogReader(
            [new CatalogSensorBinding("u1", "radar-1", 1.0, "fixture")],
            platforms: [new CatalogPlatformEntry("u1", 57.0, 20.0, 400.0)],
            mobility: [new CatalogMobility("u1", MaxSpeedKnots: 20, RangeNm: 1000)]);

        Assert.That(MapEnvelopePlatformResolver.Resolve(catalog, "u1-alpha"), Is.EqualTo("u1"));
    }

    [Test]
    public void Resolve_unknown_unit_falls_back_to_unit_id()
    {
        var catalog = InMemoryCatalogReader.BalticPatrolFixture();
        Assert.That(MapEnvelopePlatformResolver.Resolve(catalog, "no-such-unit"), Is.EqualTo("no-such-unit"));
    }
}
