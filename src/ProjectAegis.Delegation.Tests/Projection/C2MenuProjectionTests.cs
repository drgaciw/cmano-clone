using ProjectAegis.Delegation.C2Nodes;
using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

public sealed class C2MenuProjectionTests
{
    [Test]
    public void ProjectDefault_includes_view_tool_shortcuts()
    {
        var menu = C2MenuProjection.ProjectDefault();

        AssertShortcut(menu, "view-zoom-in", "+ / =");
        AssertShortcut(menu, "view-zoom-out", "-");
        AssertShortcut(menu, "tools-measure", "M");
        AssertShortcut(menu, "view-next-unit", "]");
        AssertShortcut(menu, "view-prev-unit", "[");
        AssertShortcut(menu, "view-2d-3d-toggle", "F9");
    }

    [Test]
    public void ProjectDefault_includes_layers_entry_and_layer_rows()
    {
        var menu = C2MenuProjection.ProjectDefault();

        var layersPanel = menu.Single(i => i.Id == "layers-panel");
        Assert.That(layersPanel.Label, Is.EqualTo("Layers…"));
        Assert.That(layersPanel.Category, Is.EqualTo(C2MenuCategory.Layers));

        var layerIds = menu
            .Where(i => i.Id.StartsWith("layer-", StringComparison.Ordinal))
            .Select(i => i.Id)
            .ToList();
        Assert.That(layerIds, Does.Contain("layer-Satellite"));
        Assert.That(layerIds, Does.Contain("layer-DayNight"));
        Assert.That(layerIds.Count, Is.EqualTo(8));
    }

    [Test]
    public void ProjectDefault_2d_3d_toggle_is_view_category_and_enabled()
    {
        var item = C2MenuProjection.ProjectDefault().Single(i => i.Id == "view-2d-3d-toggle");
        Assert.That(item.Category, Is.EqualTo(C2MenuCategory.View));
        Assert.That(item.IsEnabled, Is.True);
        Assert.That(item.DisabledReason, Is.Null);
        Assert.That(item.Label, Does.Contain("2D").And.Contain("3D"));
    }

    [Test]
    public void Empty_bookmarks_are_enabled_with_status_not_disabled()
    {
        var menu = C2MenuProjection.ProjectDefault(bookmarkCount: 0);
        var bookmarks = menu.Single(i => i.Id == "window-bookmarks");

        Assert.That(bookmarks.IsEnabled, Is.True);
        Assert.That(bookmarks.DisabledReason, Is.Null);
        Assert.That(bookmarks.StatusNote, Is.EqualTo(C2MenuProjection.EmptyBookmarksStatusNote));
        Assert.That(bookmarks.StatusNote, Does.Contain("No bookmarks"));
        Assert.That(bookmarks.Category, Is.EqualTo(C2MenuCategory.Window));
    }

    [Test]
    public void ProjectBookmarksItem_with_count_drops_empty_status()
    {
        var item = C2MenuProjection.ProjectBookmarksItem(bookmarkCount: 3);
        Assert.That(item.IsEnabled, Is.True);
        Assert.That(item.StatusNote, Is.Null);
        Assert.That(item.Label, Is.EqualTo("Bookmarks (3)"));
    }

    [Test]
    public void ProjectDefault_every_item_has_shortcut_label()
    {
        var menu = C2MenuProjection.ProjectDefault();
        Assert.That(menu, Is.Not.Empty);
        foreach (var item in menu)
        {
            Assert.That(item.ShortcutLabel, Is.Not.Null.And.Not.Empty, item.Id);
            Assert.That(item.Id, Is.Not.Null.And.Not.Empty);
            Assert.That(item.Label, Is.Not.Null.And.Not.Empty);
        }
    }

    [Test]
    public void ProjectDefault_categories_cover_view_layers_tools_window()
    {
        var categories = C2MenuProjection.ProjectDefault()
            .Select(i => i.Category)
            .Distinct()
            .OrderBy(c => c)
            .ToList();

        Assert.That(categories, Is.EqualTo(new[]
        {
            C2MenuCategory.View,
            C2MenuCategory.Layers,
            C2MenuCategory.Tools,
            C2MenuCategory.Window,
            C2MenuCategory.C2,
        }));
    }

    [Test]
    public void ProjectDefault_null_packages_adds_enabled_c2_nodes_empty_state()
    {
        var menu = C2MenuProjection.ProjectDefault();
        AssertEmptyC2NodesItem(menu);
    }

    [Test]
    public void ProjectDefault_empty_snapshot_adds_enabled_c2_nodes_empty_state()
    {
        var menu = C2MenuProjection.ProjectDefault(packages: MissionPackageSnapshot.Empty);
        AssertEmptyC2NodesItem(menu);
        Assert.That(menu.Any(i => i.Id.StartsWith("c2-node-", StringComparison.Ordinal)), Is.False);
    }

    [Test]
    public void ProjectDefault_with_baltic_package_adds_read_only_row_per_element()
    {
        var snapshot = MissionPackageProjection.Project(new[] { BalticAsuwPackage() });
        Assert.That(snapshot.Elements, Has.Count.EqualTo(4));

        var menu = C2MenuProjection.ProjectDefault(packages: snapshot);
        var nodeRows = menu
            .Where(i => i.Id.StartsWith("c2-node-", StringComparison.Ordinal))
            .ToList();

        Assert.That(menu.Any(i => i.Id == "c2-nodes"), Is.False);
        Assert.That(nodeRows, Has.Count.EqualTo(snapshot.Elements.Count));

        foreach (var element in snapshot.Elements)
        {
            var item = menu.Single(i => i.Id == $"c2-node-{element.ElementId}");
            Assert.That(item.Label, Does.Contain(element.Role.ToString()));
            Assert.That(item.Label, Does.Contain(element.PlatformUnitId));
            Assert.That(item.Label, Does.Contain(element.Availability.ToString()));
            Assert.That(item.Label, Is.EqualTo($"{element.Role} {element.PlatformUnitId} ({element.Availability})"));
            Assert.That(item.Category, Is.EqualTo(C2MenuCategory.C2));
            Assert.That(item.ShortcutLabel, Is.Not.Null.And.Not.Empty);
            Assert.That(item.IsEnabled, Is.False);
            Assert.That(item.DisabledReason, Is.EqualTo("Read-only"));
            Assert.That(item.StatusNote, Is.Null);
        }

        var c2 = menu.Single(i => i.Id == "c2-node-elem-c2-1");
        Assert.That(c2.Label, Is.EqualTo("C2 u1 (Available)"));
    }

    [Test]
    public void ProjectDefault_with_custom_stack_reflects_visibility()
    {
        var stack = MapLayerStackProjection.DefaultStack().SetVisible(MapLayerId.Borders, false);
        var menu = C2MenuProjection.ProjectDefault(bookmarkCount: 0, layerStack: stack);
        var borders = menu.Single(i => i.Id == "layer-Borders");
        Assert.That(borders.Label, Does.Contain("Off"));
    }

    private static void AssertShortcut(
        IReadOnlyList<C2MenuItemEntry> menu,
        string id,
        string expectedShortcut)
    {
        var item = menu.Single(i => i.Id == id);
        Assert.That(item.ShortcutLabel, Is.EqualTo(expectedShortcut));
        Assert.That(item.IsEnabled, Is.True);
    }

    private static void AssertEmptyC2NodesItem(IReadOnlyList<C2MenuItemEntry> menu)
    {
        var item = menu.Single(i => i.Id == "c2-nodes");
        Assert.That(item.IsEnabled, Is.True);
        Assert.That(item.DisabledReason, Is.Null);
        Assert.That(item.StatusNote, Is.EqualTo(C2MenuProjection.EmptyC2NodesStatusNote));
        Assert.That(item.StatusNote, Does.Contain("No C2 nodes"));
        Assert.That(item.Category, Is.EqualTo(C2MenuCategory.C2));
        Assert.That(item.ShortcutLabel, Is.Not.Null.And.Not.Empty);
        Assert.That(item.Label, Is.Not.Null.And.Not.Empty);
    }

    /// <summary>Same Baltic ASuW composition as <c>MissionPackageProjectionTests</c>.</summary>
    private static PackageDefinition BalticAsuwPackage() =>
        new(
            "pkg-asuw-1",
            "Baltic ASuW Package",
            new[]
            {
                new PackageElementDefinition("elem-sensor-1", "u1", C2NodeRole.Sensor, "package-track-feed"),
                new PackageElementDefinition("elem-shooter-1", "u2", C2NodeRole.Shooter, "package-engage"),
                new PackageElementDefinition("elem-relay-1", "u3", C2NodeRole.Relay, "package-relay"),
                new PackageElementDefinition("elem-c2-1", "u1", C2NodeRole.C2, "organic-c2"),
            });
}
