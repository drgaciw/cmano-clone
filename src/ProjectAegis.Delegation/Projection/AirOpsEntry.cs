namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// One air-asset readiness row for CMD-24 Air Operations panel (presentation only).
/// Phase A fields first; Phase N lifecycle fields are additive with safe defaults.
/// </summary>
public sealed record AirOpsEntry(
    string UnitId,
    string PlatformTypeLabel,
    string HostLabel,
    bool ReadyForLaunch,
    string StatusLine,
    string? RefusalCode,
    /// <summary>LOG-08 phase label (e.g. OnGround, Prepping, Airborne).</summary>
    string PhaseLabel = "OnGround",
    /// <summary>Remaining ticks in the current phase timer (0 when idle / airborne).</summary>
    int TimeToReadyTicks = 0,
    /// <summary>True when Launch action should be enabled for this row.</summary>
    bool CanLaunch = false,
    /// <summary>True when Abort Launch action should be enabled for this row.</summary>
    bool CanAbort = false,
    /// <summary>Stable reason when <see cref="CanLaunch"/> is false (null when launchable).</summary>
    string? LaunchDisabledReason = null);
