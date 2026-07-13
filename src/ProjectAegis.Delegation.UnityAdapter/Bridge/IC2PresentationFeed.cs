using ProjectAegis.Delegation.Projection;

namespace ProjectAegis.Delegation.UnityAdapter.Bridge;

/// <summary>
/// Read-only C2 presentation feed for Unity panel hosts (ADR-010 adapter seam).
/// Implemented by <c>DelegationBridgeHost</c> so GitNexus can trace host → adapter edges.
/// </summary>
public interface IC2PresentationFeed
{
    string? SelectedUnitId { get; }

    string? SelectedContactId { get; }

    IReadOnlyList<OobTreeEntry> LastOobTree { get; }

    IReadOnlyList<MapSymbolEntry> LastMapSymbols { get; }

    SensorC2Snapshot LastSensorC2 { get; }

    C2TopBarState LastTopBar { get; }

    UnitDetailEntry? LastUnitDetail { get; }

    void SelectUnit(string unitId);

    void SelectContact(string contactId);

    // req20-rev2 Track T1 (TR-c2-005): multi-select surface for drag-box + shift-click on the map.
    /// <summary>Full ordered multi-select (anchor first). Single-select is a set of one.</summary>
    IReadOnlyList<string> SelectedUnitIds { get; }

    /// <summary>Replace the whole selection (drag-box marquee, no modifier).</summary>
    void SelectUnits(IReadOnlyList<string> unitIds);

    /// <summary>Union into the current selection without deselecting anything (shift+drag-box).</summary>
    void AddUnits(IReadOnlyList<string> unitIds);

    /// <summary>Shift-click add-or-remove a single unit from the current selection.</summary>
    void ToggleUnit(string unitId);

    // S37-04: graph surfacing extensions (viewer/panel/highlights/bind) — read-only projections
    IReadOnlyList<string> LastGraphHighlightIds { get; }
    string? LastGraphLinkChainDisplay { get; }
}