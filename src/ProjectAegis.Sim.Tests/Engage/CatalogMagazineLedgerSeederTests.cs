using ProjectAegis.Data.Catalog;
using ProjectAegis.Sim.Engage;
using Xunit;

namespace ProjectAegis.Sim.Tests.Engage;

public sealed class CatalogMagazineLedgerSeederTests
{
    [Fact]
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

        Assert.True(usedCatalog);
        Assert.Equal(2, seeded);
        Assert.Equal(2, ledger.GetRounds(42, 0));
    }

    [Fact]
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

        Assert.False(usedCatalog);
        Assert.Equal(4, seeded);
        Assert.Equal(4, ledger.GetRounds(7, 0));
    }

    [Fact]
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

        Assert.True(resolver.Resolve(request).Launched);
        Assert.Equal(0, ledger.GetRounds(1, 0));
        Assert.False(resolver.Resolve(request).Launched);
        Assert.Equal(EngagementAbortReason.MagazineEmpty, resolver.Resolve(request).AbortReason);
    }
}