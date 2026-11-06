namespace ProjectAegis.Delegation.EmconPosture;

/// <summary>
/// DRG-221: advisory classification for emissions posture output.
/// Only <see cref="AdvisoryEmissionsPosture"/> is emitted — never emitter toggles or weapons release.
/// </summary>
public enum EmconPostureKind
{
    /// <summary>Headless EMCON/emissions posture for UI review — not emitter control.</summary>
    AdvisoryEmissionsPosture = 0,
}
