using ProjectAegis.Data.Catalog;
using ProjectAegis.Sim.Catalog;
using ProjectAegis.Sim.Policy;
using Xunit;

namespace ProjectAegis.Sim.Tests.Catalog;

public sealed class CatalogMagazineResolverTests
{
    [Fact]
    public void EvaluateInitialMagazine_sums_default_loadout_rows_for_platform()
    {
        var catalog = InMemoryCatalogReader.BalticMagazineFixture(magazineQuantity: 3);

        var readiness = CatalogMagazineResolver.EvaluateInitialMagazine("u1", catalog);

        Assert.True(readiness.CatalogResolved);
        Assert.Equal("asuw-default", readiness.LoadoutId);
        Assert.Equal(3, readiness.TotalRounds);
    }

    [Fact]
    public void Readiness_evaluator_exposes_catalog_magazine_counts()
    {
        var catalog = InMemoryCatalogReader.BalticMagazineFixture(magazineQuantity: 5);

        var readiness = ReadinessPolicyEvaluator.EvaluateMagazine("u1", catalog);

        Assert.True(readiness.CatalogResolved);
        Assert.Equal(5, readiness.TotalRounds);
    }

    [Fact]
    public void Missing_loadout_rows_leave_catalog_unresolved()
    {
        var catalog = new InMemoryCatalogReader(
            [new CatalogSensorBinding("u1", "radar-1", 1.0)],
            magazines: [new CatalogMagazineEntry("u1", "ghost", "m1", "w1", 4)]);

        var readiness = CatalogMagazineResolver.EvaluateInitialMagazine("u1", catalog);

        Assert.False(readiness.CatalogResolved);
        Assert.Equal(0, readiness.TotalRounds);
    }

    [Fact]
    public void Zero_quantity_rows_resolve_empty_magazine_from_catalog()
    {
        var catalog = InMemoryCatalogReader.BalticMagazineFixture(magazineQuantity: 0);

        var readiness = CatalogMagazineResolver.EvaluateInitialMagazine("u1", catalog);

        Assert.True(readiness.CatalogResolved);
        Assert.Equal(0, readiness.TotalRounds);
    }
}