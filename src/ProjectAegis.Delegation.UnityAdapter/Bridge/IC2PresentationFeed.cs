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
}