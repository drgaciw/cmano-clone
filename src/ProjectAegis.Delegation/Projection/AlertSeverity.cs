namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// Severity tier for a C2 alert/message (req 20 §Alerting and Interruption; GDD TR-c2-007).
/// Routing per req 20: <see cref="Critical"/> → transient toast + log + optional auto-pause;
/// <see cref="Notable"/> → log highlight; <see cref="Routine"/> → log only.
/// </summary>
/// <remarks>
/// Tier is never encoded by colour alone in the UI (a11y §5) — the ToastStack/log pair the tier
/// with an icon + label. The category → severity mapping lives in <see cref="AlertSeverityMap"/>.
/// </remarks>
public enum AlertSeverity
{
    /// <summary>Weapons inbound, unit lost, or ROE breach — demands immediate attention.</summary>
    Critical,

    /// <summary>New/reclassified contact, mission or mode change — the operator should register it.</summary>
    Notable,

    /// <summary>Nav, fuel, and other log-only events.</summary>
    Routine,
}
