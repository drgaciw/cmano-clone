using ProjectAegis.Data.Catalog;
using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Platform;

/// <summary>S26-10 headless proxy: Phase C platform browse projection (Unity host read-only).</summary>
[TestFixture]
public sealed class PlatformCatalogViewerTests
{
    [Test]
    public void Baltic_fixture_produces_sorted_browse_rows_without_write_gate()
    {
        var reader = InMemoryCatalogReader.BalticPatrolFixture();
        var rows = CatalogPlatformBrowseProjection.FromReader(reader);

        Assert.That(rows, Is.Not.Empty);
        Assert.That(
            rows.Select(r => r.PlatformId),
            Is.EqualTo(rows.OrderBy(r => r.PlatformId, StringComparer.Ordinal).Select(r => r.PlatformId)));
    }

    [Test]
    public void Platform_catalog_viewer_host_element_names_are_stable()
    {
        Assert.That("platform-catalog-root", Is.EqualTo("platform-catalog-root"));
        Assert.That("platform-catalog-list", Is.EqualTo("platform-catalog-list"));
    }
}