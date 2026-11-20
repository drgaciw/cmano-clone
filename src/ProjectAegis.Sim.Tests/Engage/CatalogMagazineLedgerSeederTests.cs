using ProjectAegis.Data.Catalog;
using ProjectAegis.Sim.Engage;
using NUnit.Framework;

namespace ProjectAegis.Sim.Tests.Engage;

public sealed class CatalogMagazineLedgerSeederTests
{
    [Test]
    public void TrySeedInitialRounds_prefers_catalog_over_fallback()
    {
        var ledger = new MagazineLedger();
        var catalog = InMemoryCatalogReader.BalticMagazineFixture(magazineQuantity: 2);

        var usedCatalog = CatalogMagazineLedgerSeeder.TrySeedInitialRounds(
            ledger,
            catalog,
            "u1",
            shooterUnitId: 42,
            mountId: 0,
            fallbackRounds: 99,
            out var seeded);

        Assert.That(usedCatalog, Is.True);
        Assert.That(seeded, Is.EqualTo(2));
        Assert.That(ledger.GetRounds(42, 0), Is.EqualTo(2));
    }

    [Test]
    public void TrySeedInitialRounds_falls_back_when_catalog_missing_rows()
    {
        var ledger = new MagazineLedger();
        var catalog = InMemoryCatalogReader.BalticPatrolFixture();

        var usedCatalog = CatalogMagazineLedgerSeeder.TrySeedInitialRounds(
            ledger,
            catalog,
            "u1",
            shooterUnitId: 7,
            mountId: 0,
            fallbackRounds: 4,
            out var seeded);

        Assert.That(usedCatalog, Is.False);
        Assert.That(seeded, Is.EqualTo(4));
        Assert.That(ledger.GetRounds(7, 0), Is.EqualTo(4));
    }

    /// <summary>Wave 2 adversarial: catalog-resolved zero must not silently apply fallback (LOG-02).</summary>
    [Test]
    public void TrySeedInitialRounds_catalog_resolved_zero_does_not_apply_fallback_and_leaves_ledger_empty()
    {
        var ledger = new MagazineLedger();
        var catalog = InMemoryCatalogReader.BalticMagazineFixture(magazineQuantity: 0);

        var usedCatalog = CatalogMagazineLedgerSeeder.TrySeedInitialRounds(
            ledger,
            catalog,
            platformId: "u1",
            shooterUnitId: 1,
            mountId: 0,
            fallbackRounds: 99,
            out var seeded);

        Assert.That(usedCatalog, Is.True);
        Assert.That(seeded, Is.EqualTo(0));
        Assert.That(ledger.GetRounds(1, 0), Is.EqualTo(0));
    }

    [Test]
    public void Resolver_aborts_second_salvo_when_catalog_seeded_magazine_depleted()
    {
        var world = new DictionaryEngageWorldQuery();
        var ledger = new MagazineLedger();
        var catalog = InMemoryCatalogReader.BalticMagazineFixture(magazineQuantity: 1);
        CatalogMagazineLedgerSeeder.TrySeedInitialRounds(
            ledger,
            catalog,
            "u1",
            shooterUnitId: 1,
            mountId: 0,
            fallbackRounds: 99,
            out _);

        var resolver = new MvpEngagementResolver(world, ledger);
        var ctx = new EngageContext(
            50_000,
            new WeaponEnvelope(1_000, 100_000),
            RoundsRemaining: 0,
            HasFireControlTrack: true);
        var request = new EngageRequest(1, 2, MountId: 0, SimTick: 0);
        world.Set(request, ctx);

        Assert.That(resolver.Resolve(request).Launched, Is.True);
        Assert.That(ledger.GetRounds(1, 0), Is.EqualTo(0));
        Assert.That(resolver.Resolve(request).Launched, Is.False);
        Assert.That(resolver.Resolve(request).AbortReason, Is.EqualTo(EngagementAbortReason.WinchesterOrdnance));
    }
}
