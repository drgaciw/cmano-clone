namespace ProjectAegis.Delegation.UnityAdapter.PlayEntry;

using Core;
using Orchestration;

/// <summary>
/// Side the player commands (or observes in AgentVsAgent). Maps onto
/// <see cref="SimulationModeProfile.PlayerControlsFriendlySide"/>.
/// </summary>
public enum PlaySide
{
    Friendly,
    Opposing,
}

/// <summary>Stable play-entry error / gate reason codes (S123 DRG-243 / DRG-246).</summary>
public static class PlayEntryErrorCodes
{
    public const string NoPackage = "NO_PACKAGE";
    public const string PackageUnavailable = "PACKAGE_UNAVAILABLE";
    public const string FileUnreadable = "FILE_UNREADABLE";
    public const string SchemaError = "SCHEMA_ERROR";
    public const string PolicyUnresolved = "POLICY_UNRESOLVED";
    public const string SeedUnsupported = "SEED_UNSUPPORTED";
    public const string BridgeBuildFailed = "BRIDGE_BUILD_FAILED";
    public const string NotPlanning = "NOT_PLANNING";
    public const string ModeRequired = "MODE_REQUIRED";
    public const string SideRequired = "SIDE_REQUIRED";
    public const string SideNotOffered = "SIDE_NOT_OFFERED";
}

/// <summary>Outcome of a play-entry command. Failed commands never mutate session state.</summary>
public sealed record PlayEntryResult(bool Succeeded, string? ErrorCode, string Message)
{
    public static PlayEntryResult Ok(string message) => new(true, null, message);

    public static PlayEntryResult Fail(string errorCode, string message) => new(false, errorCode, message);
}

/// <summary>
/// Immutable view of the play-entry session for presentation binders.
/// <see cref="Phase"/> is read from the active bridge; null when no package is loaded.
/// <see cref="SessionGeneration"/> increments on each successful load or reset so hosts can rebind.
/// </summary>
public sealed record PlayEntryState(
    string? ScenarioId,
    string? PolicyId,
    string? SourcePath,
    SimulationPhase? Phase,
    SimulationModeKind? Mode,
    PlaySide? Side,
    int SessionGeneration)
{
    public static PlayEntryState Empty { get; } = new(null, null, null, null, null, null, 0);

    public bool HasPackage => ScenarioId != null;

    public bool IsPlanning => HasPackage && Phase == SimulationPhase.Planning;
}

/// <summary>Begin Execution gate verdict (S123-06 W3-MODE-01): blocked reasons in display order.</summary>
public sealed record BeginExecutionGate(IReadOnlyList<string> BlockedReasons)
{
    public bool CanBegin => BlockedReasons.Count == 0;
}
