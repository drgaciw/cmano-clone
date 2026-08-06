using ProjectAegis.Data.Catalog;
using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

public sealed class DatalinkUnitPairFeedTests
{
    private static IReadOnlyList<CatalogLinkEntry> BalticLinks() =>
    [
        new CatalogLinkEntry("NATO_TADIL_J", "NATO Link 16", CatalogLinkTypes.Tactical, 50),
        new CatalogLinkEntry("SATCOM_B", "SATCOM Wideband", CatalogLinkTypes.Satcom, 250),
    ];

    [Test]
    public void BuildMesh_empty_units_returns_empty()
    {
        var mesh = DatalinkUnitPairFeed.BuildMesh(Array.Empty<string>(), BalticLinks());
        Assert.That(mesh, Is.Empty);
    }

    [Test]
    public void BuildMesh_empty_catalog_links_returns_empty()
    {
        var mesh = DatalinkUnitPairFeed.BuildMesh(["u1", "u2"], Array.Empty<CatalogLinkEntry>());
        Assert.That(mesh, Is.Empty);
    }

    [Test]
    public void BuildMesh_single_unit_returns_empty()
    {
        var mesh = DatalinkUnitPairFeed.BuildMesh(["u1"], BalticLinks());
        Assert.That(mesh, Is.Empty);
    }

    [Test]
    public void BuildMesh_creates_adjacent_pairs_along_sorted_ids()
    {
        // Unsorted input → sorted u1,u2,u3 → pairs u1-u2, u2-u3
        var mesh = DatalinkUnitPairFeed.BuildMesh(["u3", "u1", "u2"], BalticLinks());

        Assert.That(mesh, Has.Count.EqualTo(2));
        Assert.That(mesh[0].FromUnitId, Is.EqualTo("u1"));
        Assert.That(mesh[0].ToUnitId, Is.EqualTo("u2"));
        Assert.That(mesh[1].FromUnitId, Is.EqualTo("u2"));
        Assert.That(mesh[1].ToUnitId, Is.EqualTo("u3"));
        // Primary tactical link (first tactical in catalog order)
        Assert.That(mesh[0].LinkId, Is.EqualTo("NATO_TADIL_J"));
        Assert.That(mesh[1].LinkId, Is.EqualTo("NATO_TADIL_J"));
    }

    [Test]
    public void BuildMesh_prefers_preferred_link_id_when_present()
    {
        var mesh = DatalinkUnitPairFeed.BuildMesh(
            ["a", "b"],
            BalticLinks(),
            preferredLinkId: "SATCOM_B");

        Assert.That(mesh, Has.Count.EqualTo(1));
        Assert.That(mesh[0].LinkId, Is.EqualTo("SATCOM_B"));
    }

    [Test]
    public void BuildMesh_falls_back_to_first_link_when_no_tactical()
    {
        var links = new[]
        {
            new CatalogLinkEntry("VOICE_1", "Voice", CatalogLinkTypes.Voice, 10),
            new CatalogLinkEntry("SAT_1", "Sat", CatalogLinkTypes.Satcom, 200),
        };
        var mesh = DatalinkUnitPairFeed.BuildMesh(["x", "y"], links);
        Assert.That(mesh, Has.Count.EqualTo(1));
        Assert.That(mesh[0].LinkId, Is.EqualTo("VOICE_1"));
    }

    [Test]
    public void BuildMesh_skips_blank_and_dedupes()
    {
        var mesh = DatalinkUnitPairFeed.BuildMesh(["u2", "", "u1", "u1", "  "], BalticLinks());
        Assert.That(mesh, Has.Count.EqualTo(1));
        Assert.That(mesh[0].FromUnitId, Is.EqualTo("u1"));
        Assert.That(mesh[0].ToUnitId, Is.EqualTo("u2"));
    }

    [Test]
    public void ProjectEdges_status_up_and_resolves_link_type()
    {
        var edges = DatalinkUnitPairFeed.ProjectEdges(["u2", "u1", "u3"], BalticLinks());

        Assert.That(edges, Has.Count.EqualTo(2));
        Assert.That(edges.All(e => e.Status == DatalinkPictureProjection.StatusUp), Is.True);
        Assert.That(edges.All(e => e.LinkType == CatalogLinkTypes.Tactical), Is.True);
        // Deterministic sort by from/to
        Assert.That(edges[0].FromUnitId, Is.EqualTo("u1"));
        Assert.That(edges[0].ToUnitId, Is.EqualTo("u2"));
        Assert.That(edges[1].FromUnitId, Is.EqualTo("u2"));
        Assert.That(edges[1].ToUnitId, Is.EqualTo("u3"));
    }

    [Test]
    public void ProjectEdges_empty_units_returns_empty()
    {
        Assert.That(DatalinkUnitPairFeed.ProjectEdges(Array.Empty<string>(), BalticLinks()), Is.Empty);
    }

    [Test]
    public void BuildMesh_null_units_throws()
    {
        try
        {
            DatalinkUnitPairFeed.BuildMesh(null!, BalticLinks());
            Assert.Fail("Expected ArgumentNullException");
        }
        catch (ArgumentNullException ex)
        {
            Assert.That(ex.ParamName, Is.EqualTo("friendlyUnitIds"));
        }
    }

    [Test]
    public void BuildMesh_null_links_throws()
    {
        try
        {
            DatalinkUnitPairFeed.BuildMesh(["u1", "u2"], null!);
            Assert.Fail("Expected ArgumentNullException");
        }
        catch (ArgumentNullException ex)
        {
            Assert.That(ex.ParamName, Is.EqualTo("catalogLinks"));
        }
    }

    [Test]
    public void ProjectEdges_baltic_fixture_catalog_non_empty_for_two_units()
    {
        var catalog = InMemoryCatalogReader.BalticPatrolFixture();
        var edges = DatalinkUnitPairFeed.ProjectEdges(["blue-1", "blue-2"], catalog.GetSortedLinks());
        Assert.That(edges, Has.Count.EqualTo(1));
        Assert.That(edges[0].Status, Is.EqualTo(DatalinkPictureProjection.StatusUp));
    }
}
