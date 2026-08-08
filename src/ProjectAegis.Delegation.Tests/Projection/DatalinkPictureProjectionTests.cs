using ProjectAegis.Data.Catalog;
using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

public sealed class DatalinkPictureProjectionTests
{
    [Test]
    public void Project_from_tuples_maps_edges_and_defaults_status()
    {
        var edges = DatalinkPictureProjection.Project(
        [
            ("u2", "u1", CatalogLinkTypes.Tactical, ""),
            ("u1", "u3", CatalogLinkTypes.Satcom, DatalinkPictureProjection.StatusDegraded),
        ]);

        Assert.That(edges, Has.Count.EqualTo(2));
        Assert.That(edges[0].FromUnitId, Is.EqualTo("u1"));
        Assert.That(edges[0].ToUnitId, Is.EqualTo("u3"));
        Assert.That(edges[0].LinkType, Is.EqualTo(CatalogLinkTypes.Satcom));
        Assert.That(edges[0].Status, Is.EqualTo(DatalinkPictureProjection.StatusDegraded));

        Assert.That(edges[1].FromUnitId, Is.EqualTo("u2"));
        Assert.That(edges[1].Status, Is.EqualTo(DatalinkPictureProjection.StatusUp));
    }

    [Test]
    public void Project_skips_blank_endpoints()
    {
        var edges = DatalinkPictureProjection.Project(
        [
            ("", "u1", CatalogLinkTypes.Tactical, DatalinkPictureProjection.StatusUp),
            ("u1", "  ", CatalogLinkTypes.Tactical, DatalinkPictureProjection.StatusUp),
            ("u1", "u2", CatalogLinkTypes.Voice, DatalinkPictureProjection.StatusDown),
        ]);

        Assert.That(edges, Has.Count.EqualTo(1));
        Assert.That(edges[0].LinkType, Is.EqualTo(CatalogLinkTypes.Voice));
        Assert.That(edges[0].Status, Is.EqualTo(DatalinkPictureProjection.StatusDown));
    }

    [Test]
    public void Project_empty_returns_empty()
    {
        Assert.That(
            DatalinkPictureProjection.Project(
                Array.Empty<(string, string, string, string)>()),
            Is.Empty);
    }

    [Test]
    public void Project_with_catalog_resolves_link_type_from_link_id()
    {
        var catalog = new[]
        {
            new CatalogLinkEntry("NATO_TADIL_J", "NATO Link 16", CatalogLinkTypes.Tactical, 50),
            new CatalogLinkEntry("SATCOM_B", "SATCOM Wideband", CatalogLinkTypes.Satcom, 250),
        };

        var edges = DatalinkPictureProjection.Project(
            [
                ("cvn", "ddg", "NATO_TADIL_J"),
                ("ddg", "helo", "SATCOM_B"),
                ("helo", "uav", "UNKNOWN_LINK"),
            ],
            catalog);

        Assert.That(edges, Has.Count.EqualTo(3));
        var tadil = edges.Single(e => e.FromUnitId == "cvn");
        Assert.That(tadil.LinkType, Is.EqualTo(CatalogLinkTypes.Tactical));
        Assert.That(tadil.Status, Is.EqualTo(DatalinkPictureProjection.StatusUp));

        var satcom = edges.Single(e => e.FromUnitId == "ddg");
        Assert.That(satcom.LinkType, Is.EqualTo(CatalogLinkTypes.Satcom));

        var unknown = edges.Single(e => e.FromUnitId == "helo");
        Assert.That(unknown.LinkType, Is.EqualTo("UNKNOWN_LINK"));
    }

    [Test]
    public void Project_with_catalog_honors_status_override()
    {
        var catalog = new[]
        {
            new CatalogLinkEntry("L1", "Link", CatalogLinkTypes.Strategic, 10),
        };

        var edges = DatalinkPictureProjection.Project(
            [("a", "b", "L1")],
            catalog,
            status: DatalinkPictureProjection.StatusDegraded);

        Assert.That(edges[0].Status, Is.EqualTo(DatalinkPictureProjection.StatusDegraded));
        Assert.That(edges[0].LinkType, Is.EqualTo(CatalogLinkTypes.Strategic));
    }

    [Test]
    public void Project_null_links_throws()
    {
        IReadOnlyList<(string, string, string, string)>? links = null;
        try
        {
            DatalinkPictureProjection.Project(links!);
            Assert.Fail("Expected ArgumentNullException");
        }
        catch (ArgumentNullException ex)
        {
            Assert.That(ex.ParamName, Is.EqualTo("links"));
        }
    }

    [Test]
    public void Project_null_catalog_throws()
    {
        IReadOnlyList<(string, string, string)> unitLinks = [("a", "b", "L1")];
        try
        {
            DatalinkPictureProjection.Project(unitLinks, null!);
            Assert.Fail("Expected ArgumentNullException");
        }
        catch (ArgumentNullException ex)
        {
            Assert.That(ex.ParamName, Is.EqualTo("catalogLinks"));
        }
    }

    [TestCase("   ")]
    [TestCase("\t")]
    [TestCase("  \r\n  ")]
    public void NormalizeStatus_whitespace_defaults_to_Up(string whitespace)
    {
        var edges = DatalinkPictureProjection.Project(
        [
            ("a", "b", CatalogLinkTypes.Tactical, whitespace),
        ]);

        Assert.That(edges, Has.Count.EqualTo(1));
        Assert.That(edges[0].Status, Is.EqualTo(DatalinkPictureProjection.StatusUp));
    }

    [TestCase("  Up  ", DatalinkPictureProjection.StatusUp)]
    [TestCase("  Degraded  ", DatalinkPictureProjection.StatusDegraded)]
    [TestCase("  Down  ", DatalinkPictureProjection.StatusDown)]
    [TestCase("  Custom  ", "Custom")]
    public void NormalizeStatus_trims_surrounding_whitespace(string padded, string expected)
    {
        var edges = DatalinkPictureProjection.Project(
        [
            ("a", "b", CatalogLinkTypes.Tactical, padded),
        ]);

        Assert.That(edges, Has.Count.EqualTo(1));
        Assert.That(edges[0].Status, Is.EqualTo(expected));
    }
}
