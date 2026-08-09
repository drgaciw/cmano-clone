namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// SWARM-09: unit panel integrity readout without deep inspector.
/// Selection remains a single unit id (SWARM-05).
/// </summary>
public static class SwarmUnitPanelProjection
{
    public static string FormatIntegrityLine(SwarmIntegrityReadout? readout) =>
        readout is null ? "INTEGRITY: —" : readout.PanelLine;

    /// <summary>
    /// Builds a compact panel view model: one selection id + integrity text (+ density note).
    /// </summary>
    public static SwarmUnitPanelState Project(SwarmIntegrityReadout readout)
    {
        if (readout is null)
        {
            throw new ArgumentNullException(nameof(readout));
        }

        return new SwarmUnitPanelState(
            readout.UnitId,
            FormatIntegrityLine(readout),
            DensityLabel: "DENSITY: swarm (aggregate)",
            IsSwarm: true,
            IsDestroyed: readout.IsDestroyed || readout.DroneCount <= 0);
    }

    /// <summary>
    /// Selection for a swarm is always exactly one unit id (no per-drone nodes).
    /// </summary>
    public static string SelectSwarmUnit(string unitId)
    {
        if (string.IsNullOrWhiteSpace(unitId))
        {
            throw new ArgumentException("Swarm selection requires a unit id.", nameof(unitId));
        }

        return unitId.Trim();
    }
}

/// <summary>Headless C2 panel state for a selected swarm unit.</summary>
public sealed record SwarmUnitPanelState(
    string SelectedUnitId,
    string IntegrityLine,
    string DensityLabel,
    bool IsSwarm,
    bool IsDestroyed);
