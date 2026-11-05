namespace ProjectAegis.Delegation.AgentPerformance;

/// <summary>Detached, read-only agent performance section for a completed run's structured AAR.</summary>
public sealed record AgentPerformanceReport
{
    internal AgentPerformanceReport(IEnumerable<AgentPerformanceRow> agents, int unattributedOverrideCount)
    {
        Agents = Array.AsReadOnly(agents.ToArray());
        UnattributedOverrideCount = unattributedOverrideCount;
    }

    /// <summary>Metrics sorted by agent identity using ordinal comparison.</summary>
    public IReadOnlyList<AgentPerformanceRow> Agents { get; }

    /// <summary>
    /// Agent-to-Human changes with absent or blank agent attribution; these are never assigned using unit history.
    /// </summary>
    public int UnattributedOverrideCount { get; }
}
