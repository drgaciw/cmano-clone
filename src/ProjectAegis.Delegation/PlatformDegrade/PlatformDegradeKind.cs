namespace ProjectAegis.Delegation.PlatformDegrade;

/// <summary>
/// DRG-227: advisory classification for own-unit platform degrade output.
/// Only <see cref="AdvisoryDamageControl"/> is emitted — never retask, detach, or replan.
/// </summary>
public enum PlatformDegradeKind
{
    /// <summary>Headless damage-control posture for UI review — not a C2 order.</summary>
    AdvisoryDamageControl = 0,
}
