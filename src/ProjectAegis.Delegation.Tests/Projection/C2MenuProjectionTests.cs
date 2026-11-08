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
        }));
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
}
