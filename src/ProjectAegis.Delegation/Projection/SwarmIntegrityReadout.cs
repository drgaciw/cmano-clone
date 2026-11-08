namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// SWARM-09 / DRG-90: presentation-only aggregate integrity for a single swarm unit node.
/// Textual count is required so color is not the only channel (CMD-12).
/// </summary>
public sealed record SwarmIntegrityReadout(
    string UnitId,
    int DroneCount,
    int MaxDrones,
    bool IsDestroyed = false)
{
    public double IntegrityFraction =>
        MaxDrones <= 0 ? 0 : Math.Clamp(DroneCount / (double)MaxDrones, 0, 1);

    /// <summary>Non-color integrity channel, e.g. "24/40".</summary>
    public string CountLabel => MaxDrones <= 0 ? "0/0" : $"{DroneCount}/{MaxDrones}";

    /// <summary>Panel line including destroyed state.</summary>
    public string PanelLine =>
        IsDestroyed || DroneCount <= 0
            ? $"INTEGRITY: {CountLabel} (DESTROYED)"
            : $"INTEGRITY: {CountLabel}";

    /// <summary>Short map label suffix.</summary>
    public string MapLabelSuffix => $"[{CountLabel}]";
}
