namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// Logged intent DTO for in-play mission activate/deactivate (req 20 §Mission and Editor Entry;
/// doc 11 runtime actions ActivateMission / DeactivateMission). ADR-010: UI never mutates sim —
/// host maps <see cref="IntentKind"/> onto the bridge/order-log path.
/// </summary>
public sealed record MissionRuntimeCommand(
    string IntentKind,
    string MissionId,
    string Detail = "");

/// <summary>
/// Pure eligibility + intent factory for mission activate/deactivate over
/// <see cref="MissionListProjection"/> rows. Replay is always blocked (req 20 §Replay suppression).
/// </summary>
public static class MissionRuntimeCommandResolver
{
    /// <summary>Stable intent kind for activate (AME-5.4 ActivateMission surface).</summary>
    public const string ActivateIntentKind = "mission_activate";

    /// <summary>Stable intent kind for deactivate (AME-5.4 DeactivateMission surface).</summary>
    public const string DeactivateIntentKind = "mission_deactivate";

    public const string FailureReplay = "REPLAY_BLOCKED";
    public const string FailureMissingMissionId = "MISSING_MISSION_ID";
    public const string FailureUnknownMission = "UNKNOWN_MISSION";
    public const string FailureAlreadyActive = "ALREADY_ACTIVE";
    public const string FailureNotActive = "NOT_ACTIVE";

    /// <summary>
    /// Builds a <see cref="ActivateIntentKind"/> command when <paramref name="missionId"/> is present
    /// in the projected mission list, not already active, and not in replay. Otherwise returns
    /// <c>null</c> with a stable <paramref name="failureReason"/> code.
    /// </summary>
    public static MissionRuntimeCommand? TryActivate(
        string? missionId,
        IReadOnlyList<MissionListEntry> missions,
        bool isReplay,
        bool isCurrentlyActive,
        out string? failureReason)
    {
        if (!TryValidateMissionTarget(missionId, missions, isReplay, out var resolvedId, out failureReason))
        {
            return null;
        }

        if (isCurrentlyActive)
        {
            failureReason = FailureAlreadyActive;
            return null;
        }

        failureReason = null;
        return new MissionRuntimeCommand(ActivateIntentKind, resolvedId);
    }

    /// <summary>
    /// Builds a <see cref="DeactivateIntentKind"/> command when <paramref name="missionId"/> is present
    /// in the projected mission list, currently active, and not in replay.
    /// </summary>
    public static MissionRuntimeCommand? TryDeactivate(
        string? missionId,
        IReadOnlyList<MissionListEntry> missions,
        bool isReplay,
        bool isCurrentlyActive,
        out string? failureReason)
    {
        if (!TryValidateMissionTarget(missionId, missions, isReplay, out var resolvedId, out failureReason))
        {
            return null;
        }

        if (!isCurrentlyActive)
        {
            failureReason = FailureNotActive;
            return null;
        }

        failureReason = null;
        return new MissionRuntimeCommand(DeactivateIntentKind, resolvedId);
    }

    private static bool TryValidateMissionTarget(
        string? missionId,
        IReadOnlyList<MissionListEntry> missions,
        bool isReplay,
        out string resolvedId,
        out string? failureReason)
    {
        resolvedId = string.Empty;

        if (isReplay)
        {
            failureReason = FailureReplay;
            return false;
        }

        if (string.IsNullOrWhiteSpace(missionId))
        {
            failureReason = FailureMissingMissionId;
            return false;
        }

        missions ??= Array.Empty<MissionListEntry>();
        var match = missions.FirstOrDefault(m =>
            string.Equals(m.EventId, missionId, StringComparison.Ordinal));
        if (match is null)
        {
            failureReason = FailureUnknownMission;
            return false;
        }

        resolvedId = match.EventId;
        failureReason = null;
        return true;
    }
}
