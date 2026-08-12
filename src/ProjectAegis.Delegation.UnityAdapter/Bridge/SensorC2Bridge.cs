namespace ProjectAegis.Delegation.UnityAdapter.Bridge;

using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Projection;

/// <summary>
/// Headless/Unity facade: sensor C2 HUD snapshot + panel bind from world snapshot + decision log.
/// Read-only projection path (ADR-010 §2–3, ADR-007, ADR-001) — never mutates sim authority.
/// </summary>
public static class SensorC2Bridge
{
    /// <summary>Default panel bridge for Unity hosts (Spirit1 G1 traceability seam).</summary>
    public static ISensorC2PanelBridge PanelBridge { get; set; } = SensorC2PanelBridge.Default;

    /// <summary>
    /// Builds a sensor C2 snapshot for presentation bind (contacts, EMCON, fire-control indicators).
    /// Consumes <see cref="ISimWorldSnapshot"/> indicators and a <see cref="DecisionLog"/> only —
    /// no live ECS / session write handles.
    /// </summary>
    /// <param name="snapshot">Read-only world snapshot (EMCON / FC / engagement indicators).</param>
    /// <param name="log">Decision / order log (contact lifecycle projection source).</param>
    /// <returns>Immutable <see cref="SensorC2Snapshot"/> for panel bind.</returns>
    /// <exception cref="ArgumentNullException">When snapshot or log is null.</exception>
    public static SensorC2Snapshot Build(ISimWorldSnapshot snapshot, DecisionLog log)
    {
        // netstandard2.1 (Unity plugins): no ArgumentNullException.ThrowIfNull
        if (snapshot is null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        if (log is null)
        {
            throw new ArgumentNullException(nameof(log));
        }

        return SensorC2Projection.Build(log, new SnapshotIndicators(snapshot));
    }

    /// <summary>
    /// Maps a sensor C2 snapshot to UI Toolkit panel state via the adapter seam (not a direct binder).
    /// </summary>
    /// <param name="snapshot">Already-projected sensor C2 snapshot.</param>
    /// <returns>Panel state rows/labels for host bind.</returns>
    /// <exception cref="ArgumentNullException">When snapshot is null.</exception>
    public static SensorC2PanelState BindPanel(SensorC2Snapshot snapshot)
    {
        if (snapshot is null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        return PanelBridge.BindPanel(snapshot);
    }

    private sealed class SnapshotIndicators(ISimWorldSnapshot snapshot) : SensorC2Projection.ISensorC2WorldIndicators
    {
        public bool ObserverRadarEmconActive => snapshot.ObserverRadarEmconActive;

        public bool HasFireControlTrackOnPrimaryContact => snapshot.HasFireControlTrackOnPrimaryContact;

        public string? PrimaryHostileTargetId => snapshot.PrimaryHostileContactId?.Value;

        public int ActiveEngagementCount => snapshot.ActiveEngagementCount;
    }
}
