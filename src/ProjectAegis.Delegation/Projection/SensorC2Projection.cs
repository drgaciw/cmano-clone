namespace ProjectAegis.Delegation.Projection;

using ProjectAegis.Delegation.Decision;

/// <summary>Combines order-log contact picture with per-tick sim snapshot fields for C2 presentation.</summary>
public static class SensorC2Projection
{
    public static SensorC2Snapshot Build(DecisionLog log, ISensorC2WorldIndicators indicators) =>
        Build(ContactPictureProjection.Project(log), indicators);

    public static SensorC2Snapshot Build(
        IReadOnlyList<ContactPictureEntry> contacts,
        ISensorC2WorldIndicators indicators) =>
        new(
            contacts,
            contacts.Count,
            indicators.ObserverRadarEmconActive,
            indicators.HasFireControlTrackOnPrimaryContact,
            indicators.PrimaryHostileTargetId,
            indicators.ActiveEngagementCount);

    /// <summary>Per-tick world indicators supplied by sim / harness (Unity implements via <c>ISimWorldSnapshot</c>).</summary>
    public interface ISensorC2WorldIndicators
    {
        bool ObserverRadarEmconActive { get; }

        bool HasFireControlTrackOnPrimaryContact { get; }

        string? PrimaryHostileTargetId { get; }

        int ActiveEngagementCount { get; }
    }
}