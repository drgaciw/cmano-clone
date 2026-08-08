namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// Unit-to-unit datalink/comms edge for map network projection (CMD-32).
/// Status: "Up" | "Degraded" | "Down".
/// </summary>
public sealed record DatalinkEdgeEntry(
    string FromUnitId,
    string ToUnitId,
    string LinkType,
    string Status);
