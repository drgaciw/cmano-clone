namespace ProjectAegis.Delegation.Projection;

using ProjectAegis.Delegation.C2Nodes;

/// <summary>
/// Projects default C2 menu items with shortcut labels (CMD-28.1 / .4 / .5 / .8 / .10 / .11).
/// Pure presentation — no DecisionLog, no order issuance.
/// </summary>
public static class C2MenuProjection
{
    public const string EmptyBookmarksStatusNote =
        "No bookmarks — press Ctrl+1 to save one";

    public const string EmptyC2NodesStatusNote =
        "No C2 nodes — no mission package bound";

    public const string ReadOnlyDisabledReason = "Read-only";

    /// <summary>
    /// Default menu catalogue. Bookmark list is empty by default and remains enabled
    /// with a status note (CMD-28.11) rather than disabled-with-no-reason.
    /// Null/empty <paramref name="packages"/> is an honest empty C2-node list (DRG-189),
    /// not a fake live Baltic package.
    /// </summary>
    /// <param name="bookmarkCount">Number of saved camera bookmarks (0 → empty-state note).</param>
    /// <param name="layerStack">Optional layer stack for Layers submenu rows; null uses defaults.</param>
    /// <param name="packages">Optional DRG-213 snapshot; null/empty → enabled <c>c2-nodes</c> status.</param>
    public static IReadOnlyList<C2MenuItemEntry> ProjectDefault(
        int bookmarkCount = 0,
        MapLayerStackState? layerStack = null,
        MissionPackageSnapshot? packages = null)
    {
        var items = new List<C2MenuItemEntry>();
        AppendC2NodeItems(items, packages);

        // View — camera / mode (CMD-28.8, CMD-28.10)
        items.Add(Item("view-zoom-in", "Zoom In", "+ / =", C2MenuCategory.View));
        items.Add(Item("view-zoom-out", "Zoom Out", "-", C2MenuCategory.View));
        items.Add(Item("view-2d-3d-toggle", "2D / 3D Toggle", "F9", C2MenuCategory.View));
        items.Add(Item("view-next-unit", "Next Unit", "]", C2MenuCategory.View));
        items.Add(Item("view-prev-unit", "Previous Unit", "[", C2MenuCategory.View));

        // Tools (CMD-28.4)
        items.Add(Item("tools-measure", "Measure", "M", C2MenuCategory.Tools));

        // Layers — entry point + per-layer toggles (CMD-28.2)
        items.Add(Item("layers-panel", "Layers…", "none", C2MenuCategory.Layers));

        var stack = layerStack ?? MapLayerStackProjection.DefaultStack();
        foreach (var layer in stack.Layers)
        {
            if (layer is null)
            {
                continue;
            }

            var mark = layer.IsVisible ? "On" : "Off";
            items.Add(Item(
                id: $"layer-{layer.Id}",
                label: $"{layer.Label} ({mark})",
                shortcut: layer.ShortcutHint ?? "none",
                category: C2MenuCategory.Layers));
        }

        // Window — bookmarks empty-state (CMD-28.11): enabled with status, not disabled
        items.Add(ProjectBookmarksItem(bookmarkCount));

        return items;
    }

    private static void AppendC2NodeItems(
        List<C2MenuItemEntry> items,
        MissionPackageSnapshot? packages)
    {
        var elements = packages?.Elements;
        if (elements is { Count: > 0 })
        {
            var added = 0;
            foreach (var element in elements)
            {
                if (element is null || string.IsNullOrEmpty(element.ElementId))
                {
                    continue;
                }

                items.Add(new C2MenuItemEntry(
                    Id: C2NodeMenuFormat.ItemId(element.ElementId),
                    Label: C2NodeMenuFormat.FormatLabel(element),
                    ShortcutLabel: "none",
                    Category: C2MenuCategory.C2,
                    IsEnabled: false,
                    DisabledReason: ReadOnlyDisabledReason,
                    StatusNote: null));
                added++;
            }

            if (added > 0)
            {
                return;
            }
        }

        items.Add(new C2MenuItemEntry(
            Id: "c2-nodes",
            Label: "C2 Nodes",
            ShortcutLabel: "none",
            Category: C2MenuCategory.C2,
            IsEnabled: true,
            DisabledReason: null,
            StatusNote: EmptyC2NodesStatusNote));
    }

    /// <summary>
    /// Bookmarks menu row. Empty list stays enabled with status note so the feature
    /// does not look broken (CMD-28.11).
    /// </summary>
    public static C2MenuItemEntry ProjectBookmarksItem(int bookmarkCount)
    {
        if (bookmarkCount <= 0)
        {
            return new C2MenuItemEntry(
                Id: "window-bookmarks",
                Label: "Bookmarks",
                ShortcutLabel: "Ctrl+1…9",
                Category: C2MenuCategory.Window,
                IsEnabled: true,
                DisabledReason: null,
                StatusNote: EmptyBookmarksStatusNote);
        }

        return new C2MenuItemEntry(
            Id: "window-bookmarks",
            Label: $"Bookmarks ({bookmarkCount})",
            ShortcutLabel: "Ctrl+1…9",
            Category: C2MenuCategory.Window,
            IsEnabled: true,
            DisabledReason: null,
            StatusNote: null);
    }

    private static C2MenuItemEntry Item(
        string id,
        string label,
        string shortcut,
        C2MenuCategory category) =>
        new(
            Id: id,
            Label: label,
            ShortcutLabel: shortcut,
            Category: category,
            IsEnabled: true,
            DisabledReason: null,
            StatusNote: null);
}
