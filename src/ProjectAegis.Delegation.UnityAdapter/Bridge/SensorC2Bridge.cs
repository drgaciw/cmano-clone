namespace ProjectAegis.Delegation.UnityAdapter.Bridge;

using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Projection;

/// <summary>Unity/sim bridge for sensor C2 HUD view models.</summary>
public static class SensorC2Bridge
{
    public static SensorC2Snapshot Build(ISimWorldSnapshot snapshot, DecisionLog log) =>
        SensorC2Projection.Build(log, new SnapshotIndicators(snapshot));

    private sealed class SnapshotIndicators(ISimWorldSnapshot snapshot) : SensorC2Projection.ISensorC2WorldIndicators
    {
        public bool ObserverRadarEmconActive => snapshot.ObserverRadarEmconActive;

        public bool HasFireControlTrackOnPrimaryContact => snapshot.HasFireControlTrackOnPrimaryContact;

        public string? PrimaryHostileTargetId => snapshot.PrimaryHostileContactId?.Value;

        public int ActiveEngagementCount => snapshot.ActiveEngagementCount;
    }
}