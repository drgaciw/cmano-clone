using ProjectAegis.Data.Catalog;
using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

/// <summary>PE-UX-W5: searchable / focus-selected graph lines beyond display cap.</summary>
[TestFixture]
public sealed class PlatformCatalogGraphProjectionTests
{
    private static CatalogDependencyEdge[] SampleEdges() =>
    [
        new("u1", MountId: "m1"),
        new("u1", MountId: "m1", WeaponId: "w1"),
        new("u1", SensorId: "radar-1"),
        new("u1", LinkId: "LINK_NATO", CommsFittingId: "c1"),
        new("hostile-1", MountId: "hm1"),
        new("hostile-1", SensorId: "radar-h"),
    ];

    [Test]
    public void FormatLines_caps_unfocused_list_at_default()
    {
        var many = Enumerable.Range(0, 30)
            .Select(i => new CatalogDependencyEdge($"p{i:D2}", MountId: $"m{i}"))
            .ToArray();

        var lines = PlatformCatalogGraphProjection.FormatLines(many);

        Assert.That(lines, Has.Count.EqualTo(PlatformCatalogGraphProjection.DefaultDisplayCap));
    }

    [Test]
    public void FormatLines_focus_platform_returns_all_matching_edges()
    {
        var lines = PlatformCatalogGraphProjection.FormatLines(SampleEdges(), focusPlatformId: "u1");

        Assert.That(lines, Has.Count.EqualTo(4));
        Assert.That(lines.All(l => l.Contains("u1", StringComparison.Ordinal)), Is.True);
    }

    [Test]
    public void FormatLines_search_filters_case_insensitive()
    {
        var lines = PlatformCatalogGraphProjection.FormatLines(SampleEdges(), search: "link_nato");

        Assert.That(lines, Has.Count.EqualTo(1));
        Assert.That(lines[0], Does.Contain("LINK_NATO"));
    }

    [Test]
    public void FormatLines_focus_with_no_edges_returns_placeholder()
    {
        var lines = PlatformCatalogGraphProjection.FormatLines(SampleEdges(), focusPlatformId: "missing");

        Assert.That(lines, Has.Count.EqualTo(1));
        Assert.That(lines[0], Does.Contain("no graph edges"));
        Assert.That(lines[0], Does.Contain("missing"));
    }

    [Test]
    public void FormatEdgeLine_includes_kind_and_ids()
    {
        var edge = new CatalogDependencyEdge("u1", LinkId: "LINK_NATO", CommsFittingId: "c1");
        var line = PlatformCatalogGraphProjection.FormatEdgeLine(edge);

        Assert.That(line, Does.Contain("PlatformToLink"));
        Assert.That(line, Does.Contain("u1"));
        Assert.That(line, Does.Contain("LINK_NATO"));
    }
}
