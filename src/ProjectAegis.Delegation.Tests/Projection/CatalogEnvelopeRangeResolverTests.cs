using ProjectAegis.Data.Catalog;
using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

public sealed class CatalogEnvelopeRangeResolverTests
{
    [Test]
    public void MetersToNauticalMiles_uses_1852()
    {
        Assert.That(CatalogEnvelopeRangeResolver.MetersToNauticalMiles(1852), Is.EqualTo(1.0).Within(1e-12));
        Assert.That(CatalogEnvelopeRangeResolver.MetersToNauticalMiles(100_000),
            Is.EqualTo(100_000 / 1852.0).Within(1e-9));
    }

    [Test]
    public void TryResolveWeaponRangeNm_null_catalog_returns_false()
    {
        Assert.That(
            CatalogEnvelopeRangeResolver.TryResolveWeaponRangeNm(null, CatalogWeaponIds.MvpDefault, out var nm),
            Is.False);
        Assert.That(nm, Is.EqualTo(0));
    }

    [Test]
    public void TryResolveWeaponRangeNm_blank_weapon_returns_false()
    {
        var catalog = InMemoryCatalogReader.BalticPatrolFixture();
        Assert.That(CatalogEnvelopeRangeResolver.TryResolveWeaponRangeNm(catalog, "", out _), Is.False);
        Assert.That(CatalogEnvelopeRangeResolver.TryResolveWeaponRangeNm(catalog, "   ", out _), Is.False);
    }

    [Test]
    public void TryResolveWeaponRangeNm_mvp_default_from_baltic_fixture()
    {
        var catalog = InMemoryCatalogReader.BalticPatrolFixture();
        Assert.That(
            CatalogEnvelopeRangeResolver.TryResolveWeaponRangeNm(
                catalog,
                CatalogWeaponIds.MvpDefault,
                out var maxRangeNm),
            Is.True);

        var expected = CatalogWeaponDefaults.MvpEnvelope.MaxRangeMeters / 1852.0;
        Assert.That(maxRangeNm, Is.EqualTo(expected).Within(1e-9));
    }

    [Test]
    public void TryResolveWeaponRangeNm_unknown_weapon_returns_false()
    {
        var catalog = InMemoryCatalogReader.BalticPatrolFixture();
        Assert.That(
            CatalogEnvelopeRangeResolver.TryResolveWeaponRangeNm(catalog, "no-such-weapon", out var nm),
            Is.False);
        Assert.That(nm, Is.EqualTo(0));
    }

    [Test]
    public void ResolveSelectedUnitRanges_null_catalog_uses_defaults()
    {
        var (sensor, weapon) = CatalogEnvelopeRangeResolver.ResolveSelectedUnitRanges(null, "u1");
        Assert.That(sensor, Is.EqualTo(CatalogEnvelopeRangeResolver.DefaultSensorRangeNm));
        Assert.That(weapon, Is.EqualTo(CatalogEnvelopeRangeResolver.DefaultWeaponRangeNm));
    }

    [Test]
    public void ResolveSelectedUnitRanges_catalog_hit_resolves_weapon_keeps_default_sensor()
    {
        var catalog = InMemoryCatalogReader.BalticPatrolFixture();
        var (sensor, weapon) = CatalogEnvelopeRangeResolver.ResolveSelectedUnitRanges(
            catalog,
            "u1",
            CatalogWeaponIds.MvpDefault);

        Assert.That(sensor, Is.EqualTo(CatalogEnvelopeRangeResolver.DefaultSensorRangeNm));
        var expectedWeapon = CatalogWeaponDefaults.MvpEnvelope.MaxRangeMeters / 1852.0;
        Assert.That(weapon, Is.EqualTo(expectedWeapon).Within(1e-9));
    }

    [Test]
    public void ResolveSelectedUnitRanges_unknown_weapon_falls_back_to_default_weapon()
    {
        var catalog = InMemoryCatalogReader.BalticPatrolFixture();
        var (sensor, weapon) = CatalogEnvelopeRangeResolver.ResolveSelectedUnitRanges(
            catalog,
            unitId: null,
            weaponId: "missing-weapon");

        Assert.That(sensor, Is.EqualTo(40.0));
        Assert.That(weapon, Is.EqualTo(20.0));
    }

    [Test]
    public void ResolveSelectedUnitRanges_kill_chain_long_range()
    {
        var catalog = InMemoryCatalogReader.BalticPatrolFixture();
        var (_, weapon) = CatalogEnvelopeRangeResolver.ResolveSelectedUnitRanges(
            catalog,
            "any",
            CatalogWeaponIds.KillChainLongRange);

        var expected = CatalogWeaponDefaults.KillChainLongRangeEnvelope.MaxRangeMeters / 1852.0;
        Assert.That(weapon, Is.EqualTo(expected).Within(1e-9));
    }
}
