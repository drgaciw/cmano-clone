namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// Single source of truth mapping <see cref="MessageLogProjection"/> category strings to an
/// <see cref="AlertSeverity"/> tier (req 20 §Alerting and Interruption; GDD TR-c2-007). The mapping
/// lives with the projection layer, not scattered in UI code (GDD §Alerting projection).
/// </summary>
/// <remarks>
/// req 20 tiers: Critical = weapons inbound / unit lost / ROE breach; Notable = new or reclassified
/// contact, mission/mode change; Routine = nav, fuel, log-only. The category identifiers below mirror
/// the strings emitted by <see cref="MessageLogProjection"/> (<c>MessageLogLine.Category</c>).
/// Centralising those identifiers as shared constants is a candidate follow-up, out of scope for the
/// Phase 0 contract.
/// </remarks>
public static class AlertSeverityMap
{
    /// <summary>
    /// Severity tier for a message-log category. Unknown or null categories default to
    /// <see cref="AlertSeverity.Routine"/> (log-only) so a new category never silently escalates.
    /// </summary>
    public static AlertSeverity ForCategory(string? category) => category switch
    {
        // Critical — unit lost / ROE breach.
        "KILL_CONFIRMED" => AlertSeverity.Critical,
        "POLICY_DENIAL" => AlertSeverity.Critical,

        // Notable — situational change the operator should register (log highlight, no auto-pause).
        "CONTACT" => AlertSeverity.Notable,
        "MODE" => AlertSeverity.Notable,
        "MISSION" => AlertSeverity.Notable,
        "ENGAGE_ABORT" => AlertSeverity.Notable,

        // Routine — log-only. WEAPON_LAUNCH is Routine by decision (2026-07-08): the category fires on
        // friendly launches too, and inbound-threat criticality is carried by KILL_CONFIRMED /
        // POLICY_DENIAL. Split-by-direction remains a possible future refinement.
        "WEAPON_LAUNCH" => AlertSeverity.Routine,
        "MAGAZINE" => AlertSeverity.Routine,
        "PLAYER_ORDER" => AlertSeverity.Routine,
        "COMMS" => AlertSeverity.Routine,
        "FUEL" => AlertSeverity.Routine,
        "INTERCEPT_SUCCESS" => AlertSeverity.Routine,
        "HIT" => AlertSeverity.Routine,
        "MISS" => AlertSeverity.Routine,
        "COMBAT" => AlertSeverity.Routine,

        _ => AlertSeverity.Routine,
    };
}
