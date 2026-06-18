using ProjectAegis.Data.Catalog;
using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

[TestFixture]
public sealed class PlatformCatalogFilterProjectionTests
{
    private static IReadOnlyList<CatalogPlatformBrowseRow> BalticRows()
    {
        var platforms = CatalogValidationDefaults.BalticPlatforms();
        var bindings = platforms
            .Select(p => new CatalogSensorBinding(p.PlatformId, "radar-1", 1.0, $"baltic-fixture-{p.PlatformId}"))
            .ToArray();
        var reader = new InMemoryCatalogReader(bindings, "p0-baltic-fixture", platforms);
        return CatalogPlatformBrowseProjection.FromReader(reader);
    }

    [Test]
    public void Filter_narrows_list_preserving_stable_order()
    {
        var all = BalticRows();
        Assert.That(all.Count, Is.GreaterThanOrEqualTo(3));

        var filtered = PlatformCatalogFilterProjection.Apply(all, "hostile");

        Assert.That(
            filtered.Select(r => r.PlatformId).ToArray(),
            Is.EqualTo(new[] { "hostile-1", "hostile-far" }));
    }

    [Test]
    public void Empty_filter_shows_all_rows()
    {
        var all = BalticRows();

        Assert.That(PlatformCatalogFilterProjection.Apply(all, string.Empty), Is.EqualTo(all));
        Assert.That(PlatformCatalogFilterProjection.Apply(all, "   "), Is.EqualTo(all));
        Assert.That(PlatformCatalogFilterProjection.Apply(all, null), Is.EqualTo(all));
    }

    [Test]
    public void No_match_filter_shows_empty()
    {
        var all = BalticRows();

        Assert.That(PlatformCatalogFilterProjection.Apply(all, "no-such-platform-xyz"), Is.Empty);
    }

    [Test]
    public void Filter_is_case_insensitive_on_platform_id()
    {
        var all = BalticRows();

        var filtered = PlatformCatalogFilterProjection.Apply(all, "HOSTILE-1");

        Assert.That(filtered.Select(r => r.PlatformId).ToArray(), Is.EqualTo(new[] { "hostile-1" }));
    }
}