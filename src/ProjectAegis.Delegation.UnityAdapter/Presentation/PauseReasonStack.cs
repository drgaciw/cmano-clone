using ProjectAegis.Delegation.Projection;

namespace ProjectAegis.Delegation.UnityAdapter.Presentation;

/// <summary>
/// Ordered, de-duplicated set of active pause reasons with multitasker semantics (req 20 rev 2
/// §Alerting and Interruption, TR-c2-007). This is SESSION/UI control state, NOT sim state (ADR-010):
/// the host consults <see cref="IsPaused"/> before advancing the sim tick, so a paused sim simply does
/// not receive <c>Tick()</c> calls. Deterministic replay/order-log state is never touched — the Baltic
/// hash is unaffected because no sim mutation occurs here.
/// </summary>
/// <remarks>
/// Reasons stack: the sim stays paused until every reason that pushed it is removed (e.g. an auto-pause
/// reason and a manual pause reason are independent). <see cref="ApplyAutoPause"/> is the bridge from
/// Track T3's <see cref="AutoPauseCommand"/> (already replay-suppressed by
/// <see cref="AutoPausePolicy.Evaluate"/>, which returns null in replay) onto this stack.
/// </remarks>
public sealed class PauseReasonStack
{
    private readonly List<string> _reasons = new();

    /// <summary>Active pause reasons in push order.</summary>
    public IReadOnlyList<string> Reasons => _reasons;

    /// <summary>True while at least one reason holds the sim paused.</summary>
    public bool IsPaused => _reasons.Count > 0;

    /// <summary>True if <paramref name="reason"/> is currently holding a pause.</summary>
    public bool Contains(string? reason) => !string.IsNullOrEmpty(reason) && _reasons.Contains(reason!);

    /// <summary>Push a pause reason. No-op if already present or null/empty. Returns true if added.</summary>
    public bool Push(string? reason)
    {
        if (string.IsNullOrEmpty(reason) || _reasons.Contains(reason!))
        {
            return false;
        }

        _reasons.Add(reason!);
        return true;
    }

    /// <summary>Remove a pause reason (resume when it was the last). Returns true if it was present.</summary>
    public bool Remove(string? reason) => !string.IsNullOrEmpty(reason) && _reasons.Remove(reason!);

    /// <summary>Clear every pause reason (force resume).</summary>
    public void Clear() => _reasons.Clear();

    /// <summary>
    /// Apply an <see cref="AutoPauseCommand"/> from the alerting layer by pushing the canonical
    /// <see cref="PauseReasonIds.AutoPauseSeverity"/> reason (aligned with
    /// <see cref="AutoPausePolicy.CriticalAlertReason"/>). A null command (no Critical alert, or
    /// replay-suppressed) is a no-op. Returns true if a new pause was added.
    /// </summary>
    public bool ApplyAutoPause(AutoPauseCommand? command) =>
        command != null && Push(PauseReasonIds.AutoPauseSeverity);
}
