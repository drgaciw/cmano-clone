namespace ProjectAegis.Delegation.Projection;

/// <summary>Minimal sensor C2 HUD state: contacts plus EMCON / track / engagement indicators.</summary>
public sealed record SensorC2Snapshot(
    IReadOnlyList<ContactPictureEntry> Contacts,
    int ActiveContactCount,
    bool ObserverRadarEmconActive,
    bool HasFireControlTrackOnPrimary,
    string? PrimaryTargetId,
    int ActiveEngagementCount);