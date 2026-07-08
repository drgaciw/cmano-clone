namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// A presentation-layer COMMAND intent (ADR-010: UI binds projections only, UI never mutates sim
/// directly) requesting that a Critical-tier alert push a reason onto the sim's pause-reason stack
/// (req 20 §Alerting and Interruption, TR-c2-007 default matrix: Critical pauses; Notable flashes;
/// Routine log-only).
/// </summary>
/// <remarks>
/// <para>
/// <b>BLOCKER (Track T3, req 20 rev 2, 2026-07-08):</b> this codebase has no pause-reason-stack API
/// reachable from the presentation layer — or anywhere. A repo-wide search of
/// <c>src/ProjectAegis.Sim</c>, <c>src/ProjectAegis.Delegation*</c>, and
/// <c>unity/ProjectAegis/Assets</c> for "pause" / "multitasker" found only this Phase-0 doc comment and
/// the <c>AlertSeverity</c> contract itself. <see cref="ProjectAegis.Sim.Time.SimClock"/> exposes only
/// <c>AdvanceOneTick</c>/<c>Reset</c> (no pause concept at all), and
/// <see cref="ProjectAegis.Delegation.Orchestration.SimulationSession"/> has no pause hook of any kind.
/// </para>
/// <para>
/// Per the track's ground rules this stops at producing the COMMAND *value* — a pure, testable decision
/// of "does this alert batch want auto-pause" — via <see cref="AutoPausePolicy.Evaluate"/>. It is
/// intentionally NOT wired to any bridge/sim call: <c>DelegationBridge.cs</c> is unchanged (zero diff,
/// Baltic hash unaffected). Dispatching this command onto an actual pause-reason stack is blocked
/// pending a shared pause/interrupt contract from a future track or ADR; see the T3 report to
/// producer / ui-experience-lead / determinism-engineer for follow-up ownership.
/// </para>
/// </remarks>
public sealed record AutoPauseCommand(string Reason, ulong TriggerSequenceId, string? SourceUnitId);

/// <summary>
/// Pure policy: given a batch of newly observed alerts, should the presentation layer request
/// auto-pause? (req 20 default matrix — see <see cref="AutoPauseCommand"/> remarks for the
/// pause-reason-stack reachability blocker.)
/// </summary>
public static class AutoPausePolicy
{
    /// <summary>Stable reason id for a Critical-tier C2 alert auto-pause request.</summary>
    public const string CriticalAlertReason = "c2.alert.critical";

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
