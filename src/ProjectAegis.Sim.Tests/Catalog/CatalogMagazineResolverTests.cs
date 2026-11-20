using ProjectAegis.Data.Catalog;
using ProjectAegis.Sim.Catalog;
using ProjectAegis.Sim.Policy;
using NUnit.Framework;

namespace ProjectAegis.Sim.Tests.Catalog;

public sealed class CatalogMagazineResolverTests
{
    [Test]
    public void EvaluateInitialMagazine_sums_default_loadout_rows_for_platform()
    {
        var catalog = InMemoryCatalogReader.BalticMagazineFixture(magazineQuantity: 3);

        var readiness = CatalogMagazineResolver.EvaluateInitialMagazine("u1", catalog);

        Assert.That(readiness.CatalogResolved, Is.True);
        Assert.That(readiness.LoadoutId, Is.EqualTo("asuw-default"));
        Assert.That(readiness.TotalRounds, Is.EqualTo(3));
    }

    [Test]
    public void Readiness_evaluator_exposes_catalog_magazine_counts()
    {
        var catalog = InMemoryCatalogReader.BalticMagazineFixture(magazineQuantity: 5);

        var readiness = ReadinessPolicyEvaluator.EvaluateMagazine("u1", catalog);

        Assert.That(readiness.CatalogResolved, Is.True);
        Assert.That(readiness.TotalRounds, Is.EqualTo(5));
    }

    [Test]
    public void Missing_loadout_rows_leave_catalog_unresolved()
    {
        var catalog = new InMemoryCatalogReader(
            [new CatalogSensorBinding("u1", "radar-1", 1.0)],
            magazines: [new CatalogMagazineEntry("u1", "ghost", "m1", "w1", 4)]);

        var readiness = CatalogMagazineResolver.EvaluateInitialMagazine("u1", catalog);

        Assert.That(readiness.CatalogResolved, Is.False);
        Assert.That(readiness.TotalRounds, Is.EqualTo(0));
    }

    [Test]
    public void Zero_quantity_rows_resolve_empty_magazine_from_catalog()
    {
        var catalog = InMemoryCatalogReader.BalticMagazineFixture(magazineQuantity: 0);

        var readiness = CatalogMagazineResolver.EvaluateInitialMagazine("u1", catalog);

        Assert.That(readiness.CatalogResolved, Is.True);
        Assert.That(readiness.TotalRounds, Is.EqualTo(0));
    }
}