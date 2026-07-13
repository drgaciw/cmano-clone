namespace ProjectAegis.Delegation.UnityAdapter.Bridge;

using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Projection;

/// <summary>Unity/sim bridge for sensor C2 HUD view models.</summary>
public static class SensorC2Bridge
{
    /// <summary>Default panel bridge for Unity hosts (Spirit1 G1 traceability seam).</summary>
    public static ISensorC2PanelBridge PanelBridge { get; set; } = SensorC2PanelBridge.Default;

    public static SensorC2Snapshot Build(ISimWorldSnapshot snapshot, DecisionLog log) =>
        SensorC2Projection.Build(log, new SnapshotIndicators(snapshot));

    /// <summary>Maps snapshot to UI Toolkit panel state via adapter seam (not direct binder).</summary>
    public static SensorC2PanelState BindPanel(SensorC2Snapshot snapshot) =>
        PanelBridge.BindPanel(snapshot);

    private sealed class SnapshotIndicators(ISimWorldSnapshot snapshot) : SensorC2Projection.ISensorC2WorldIndicators
    {
        public bool ObserverRadarEmconActive => snapshot.ObserverRadarEmconActive;

        public bool HasFireControlTrackOnPrimaryContact => snapshot.HasFireControlTrackOnPrimaryContact;

        public string? PrimaryHostileTargetId => snapshot.PrimaryHostileContactId?.Value;

        public int ActiveEngagementCount => snapshot.ActiveEngagementCount;
    }
}