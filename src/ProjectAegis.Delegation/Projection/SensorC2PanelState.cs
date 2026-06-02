namespace ProjectAegis.Delegation.Projection;

/// <summary>Display-only sensor C2 panel fields (no Unity dependency).</summary>
public sealed record SensorC2PanelState(
    string EmconLabel,
    string TrackLabel,
    string ContactCountLabel,
    IReadOnlyList<SensorC2ContactRow> ContactRows);