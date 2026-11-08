namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// One basemap layer row for the CMD-28.2 layer stack checklist (UI-local presentation).
/// </summary>
/// <param name="Id">Stable layer id.</param>
/// <param name="Label">Operator-facing display name.</param>
/// <param name="IsVisible">Whether the layer is currently drawn.</param>
/// <param name="ShortcutHint">Keyboard letter or <c>"none"</c> when no binding.</param>
public sealed record MapLayerEntry(
    MapLayerId Id,
    string Label,
    bool IsVisible,
    string ShortcutHint);
