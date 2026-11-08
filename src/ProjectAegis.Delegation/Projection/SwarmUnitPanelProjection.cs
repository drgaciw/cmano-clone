namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// SWARM-09 / SWARM-14: unit panel integrity + Phase B mode/host/link/CEC readout.
/// Selection remains a single unit id (SWARM-05). Presentation only.
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
    /// SWARM-14 Phase B: project full panel including mode, host, linkState, cecMeshState.
    /// Missing telemetry uses explicit unknown-with-reason (CMD-17 pattern).
    /// </summary>
    public static SwarmUnitPanelState ProjectPhaseB(SwarmPanelSnapshot snapshot)
    {
        if (snapshot is null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        if (string.IsNullOrWhiteSpace(snapshot.UnitId))
        {
            throw new ArgumentException("Unit id is required.", nameof(snapshot));
        }

        var integrity = new SwarmIntegrityReadout(
            snapshot.UnitId.Trim(),
            snapshot.DroneCount,
            snapshot.MaxDrones,
            snapshot.IsDestroyed);

        return new SwarmUnitPanelState(
            SelectedUnitId: integrity.UnitId,
            IntegrityLine: FormatIntegrityLine(integrity),
            DensityLabel: "DENSITY: swarm (aggregate)",
            IsSwarm: true,
            IsDestroyed: integrity.IsDestroyed || integrity.DroneCount <= 0,
            ModeLine: FormatField("MODE", snapshot.Mode, snapshot.ModeUnknownReason),
            HostLine: FormatField("HOST", snapshot.HostId, snapshot.HostUnknownReason),
            LinkStateLine: FormatField("LINK", snapshot.LinkState, snapshot.LinkUnknownReason),
            CecMeshStateLine: FormatField("CEC", snapshot.CecMeshState, snapshot.CecUnknownReason));
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

    private static string FormatField(string label, string? value, string? unknownReason)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            return $"{label}: {value.Trim()}";
        }

        var reason = string.IsNullOrWhiteSpace(unknownReason)
            ? "unknown"
            : unknownReason.Trim();
        return $"{label}: unknown ({reason})";
    }
}

/// <summary>Headless C2 panel state for a selected swarm unit (Phase A + B fields).</summary>
public sealed record SwarmUnitPanelState(
    string SelectedUnitId,
    string IntegrityLine,
    string DensityLabel,
    bool IsSwarm,
    bool IsDestroyed,
    string? ModeLine = null,
    string? HostLine = null,
    string? LinkStateLine = null,
    string? CecMeshStateLine = null);
