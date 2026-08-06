namespace ProjectAegis.Delegation.Projection;

/// <summary>Top-level menu categories (CMD-28.7 organisation).</summary>
public enum C2MenuCategory
{
    View = 0,
    Layers = 1,
    Tools = 2,
    Window = 3,
}

/// <summary>
/// One C2 menu item with inline shortcut discovery (CMD-28.1).
/// View-state items are ADR-010 exempt (CMD-28.3); they do not go through DecisionLog.
/// </summary>
/// <param name="Id">Stable item id.</param>
/// <param name="Label">Operator-facing label.</param>
/// <param name="ShortcutLabel">Binding advertised inline (e.g. <c>+ / =</c>, <c>F9</c>).</param>
/// <param name="Category">Menu grouping.</param>
/// <param name="IsEnabled">Whether the item is actionable.</param>
/// <param name="DisabledReason">Why disabled, when not enabled; null when enabled.</param>
/// <param name="StatusNote">
/// Optional status for empty-but-enabled states (CMD-28.11 bookmarks). Not a disabled reason.
/// </param>
public sealed record C2MenuItemEntry(
    string Id,
    string Label,
    string ShortcutLabel,
    C2MenuCategory Category,
    bool IsEnabled,
    string? DisabledReason = null,
    string? StatusNote = null);
