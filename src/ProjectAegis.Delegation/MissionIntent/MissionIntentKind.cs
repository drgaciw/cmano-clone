namespace ProjectAegis.Delegation.MissionIntent;

/// <summary>
/// DRG-229: advisory classification for mission-command intent output.
/// Only <see cref="AdvisoryIntent"/> is emitted — never orders, retask enqueue, or catalog writes.
/// </summary>
public enum MissionIntentKind
{
    /// <summary>Headless mission intent for UI review — not a C2 order.</summary>
    AdvisoryIntent = 0,
}
