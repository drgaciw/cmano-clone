namespace ProjectAegis.Delegation.UnityAdapter.Bridge;

using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Projection;

/// <summary>
/// Headless/Unity facade: project order log to HUD message lines.
/// Read-only projection path (ADR-010 §2–3, ADR-007, ADR-001) — never mutates sim authority.
/// </summary>
public static class MessageLogBridge
{
    /// <summary>
    /// Full AAR message log (all projected order-log categories) for presentation bind.
    /// Consumes <see cref="DecisionLog"/> only — no live ECS / session write handles.
    /// </summary>
    /// <param name="log">Decision / order log (message projection source).</param>
    /// <returns>Immutable <see cref="IReadOnlyList{T}"/> of <see cref="MessageLogLine"/> rows.</returns>
    /// <exception cref="ArgumentNullException">When log is null.</exception>
    public static IReadOnlyList<MessageLogLine> ProjectFrom(DecisionLog log)
    {
        // netstandard2.1 (Unity plugins): no ArgumentNullException.ThrowIfNull
        if (log is null)
        {
            throw new ArgumentNullException(nameof(log));
        }

        return MessageLogProjection.Project(log);
    }

    /// <summary>
    /// Compact combat strip (bottom HUD subset: kills, intercepts, hits, misses, magazine).
    /// </summary>
    /// <param name="log">Decision / order log (message projection source).</param>
    /// <returns>Immutable combat-category subset of message lines.</returns>
    /// <exception cref="ArgumentNullException">When log is null.</exception>
    public static IReadOnlyList<MessageLogLine> ProjectCombatMessages(DecisionLog log)
    {
        if (log is null)
        {
            throw new ArgumentNullException(nameof(log));
        }

        return ProjectFrom(log)
            .Where(m => m.Category is "KILL_CONFIRMED" or "INTERCEPT_SUCCESS" or "HIT" or "MISS" or "MAGAZINE")
            .ToArray();
    }
}
