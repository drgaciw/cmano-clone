namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// One air-asset readiness row for CMD-24 Air Operations panel (presentation only).
/// Phase A fields first; Phase N lifecycle fields are additive with safe defaults.
/// </summary>
/// <param name="UnitId">Stable unit identifier.</param>
/// <param name="PlatformTypeLabel">Display label for the platform type.</param>
/// <param name="HostLabel">Display label for the host.</param>
/// <param name="ReadyForLaunch">Whether the asset is ready to launch.</param>
/// <param name="StatusLine">Presentation status text.</param>
/// <param name="RefusalCode">Optional stable launch-refusal code.</param>
/// <param name="PhaseLabel">LOG-08 phase label (for example, OnGround, Prepping, or Airborne).</param>
/// <param name="TimeToReadyTicks">Remaining ticks in the current phase timer; zero when idle or airborne.</param>
/// <param name="CanLaunch">Whether the Launch action should be enabled for this row.</param>
/// <param name="CanAbort">Whether the Abort Launch action should be enabled for this row.</param>
/// <param name="LaunchDisabledReason">Stable reason when <paramref name="CanLaunch"/> is false; null when launchable.</param>
public sealed record AirOpsEntry(
    string UnitId,
    string PlatformTypeLabel,
    string HostLabel,
    bool ReadyForLaunch,
    string StatusLine,
    string? RefusalCode,
    string PhaseLabel = "OnGround",
    int TimeToReadyTicks = 0,
    bool CanLaunch = false,
    bool CanAbort = false,
    string? LaunchDisabledReason = null);
