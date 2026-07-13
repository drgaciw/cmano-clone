namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// A presentation-layer COMMAND intent (ADR-010: UI binds projections only, UI never mutates sim
/// directly) requesting that a Critical-tier alert push a reason onto the sim's pause-reason stack
/// (req 20 §Alerting and Interruption, TR-c2-007 default matrix: Critical pauses; Notable flashes;
/// Routine log-only).
/// </summary>
/// <remarks>
/// Dispatch path (T5 / Phase 2b): hosts feed this command into
/// <c>PauseReasonStack.ApplyAutoPause</c>, which pushes the canonical
/// <c>PauseReasonIds.AutoPauseSeverity</c> reason. <see cref="CriticalAlertReason"/> is string-aligned
/// with that id so command.Reason and the stack reason stay identical. Never mutates sim tables —
/// the host consults stack <c>IsPaused</c> before advancing ticks (ADR-010).
/// </remarks>
public sealed record AutoPauseCommand(string Reason, ulong TriggerSequenceId, string? SourceUnitId);

/// <summary>
/// Pure policy: given a batch of newly observed alerts, should the presentation layer request
/// auto-pause? (req 20 default matrix). Dispatch onto <c>PauseReasonStack.ApplyAutoPause</c>.
/// </summary>
public static class AutoPausePolicy
{
    /// <summary>
    /// Stable reason id for a Critical-tier C2 alert auto-pause request.
    /// Aligned with <c>PauseReasonIds.AutoPauseSeverity</c> (<c>auto_pause_severity</c>) so
    /// <c>ApplyAutoPause</c> and command.Reason share one canonical stack key.
    /// </summary>
    public const string CriticalAlertReason = "auto_pause_severity";

    /// <summary>
    /// Returns an <see cref="AutoPauseCommand"/> for the first Critical-tier alert in
    /// <paramref name="newAlerts"/> (in list order), or <c>null</c> if there is none. Always returns
    /// <c>null</c> when <paramref name="isReplay"/> is true — replay is read-only and must never issue an
    /// interrupt/pause command (req 20 §Replay suppression), regardless of severity.
    /// </summary>
    public static AutoPauseCommand? Evaluate(IReadOnlyList<AlertItem> newAlerts, bool isReplay)
    {
        if (isReplay)
        {
            return null;
        }

        foreach (var alert in newAlerts)
        {
            if (alert.Severity == AlertSeverity.Critical)
            {
                return new AutoPauseCommand(CriticalAlertReason, alert.SequenceId, alert.UnitId);
            }
        }

        return null;
    }
}
