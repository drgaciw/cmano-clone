namespace ProjectAegis.Delegation.Projection;

using ProjectAegis.Delegation.Attention;
using ProjectAegis.Delegation.Controllers;

/// <summary>
/// DRG-67: projects per-agent attention load into UI-ready state for the C2 chrome.
/// Reads from <see cref="AgentController"/> budget + live <see cref="AttentionEvaluation"/>
/// snapshots; does not modify any state.
/// </summary>
public static class AgentAttentionProjection
{
    /// <summary>
    /// Projects a single agent's attention evaluation into a display row.
    /// <paramref name="evaluation"/> may be null when the agent has not ticked yet.
    /// </summary>
    public static AgentAttentionRow Project(
        AgentController agent,
        AttentionEvaluation? evaluation)
    {
        if (agent is null)
        {
            return AgentAttentionRow.Unknown;
        }

        var budget = agent.AttentionBudget;
        var load = evaluation?.Load ?? 0.0;
        var isOverloaded = evaluation?.IsOverloaded ?? false;
        var degradation = evaluation?.Degradation;

        return new AgentAttentionRow(
            AgentId: agent.Id.Value,
            Budget: budget,
            Load: load,
            IsOverloaded: isOverloaded,
            StatusLabel: FormatStatus(load, budget, degradation),
            LoadBadge: FormatLoadBadge(load, budget));
    }

    /// <summary>
    /// Projects a collection of (agent, evaluation) pairs into a summary state.
    /// </summary>
    public static AgentAttentionSummary Summarize(
        IReadOnlyList<(AgentController Agent, AttentionEvaluation? Evaluation)>? pairs)
    {
        if (pairs is null || pairs.Count == 0)
        {
            return AgentAttentionSummary.Empty;
        }

        var overloadedCount = 0;
        var rows = new List<AgentAttentionRow>(pairs.Count);
        foreach (var (agent, eval) in pairs)
        {
            var row = Project(agent, eval);
            rows.Add(row);
            if (row.IsOverloaded)
            {
                overloadedCount++;
            }
        }

        var summaryLine = overloadedCount > 0
            ? $"ATTENTION: {overloadedCount}/{pairs.Count} overloaded"
            : $"ATTENTION: {pairs.Count} agent(s) nominal";

        return new AgentAttentionSummary(rows, summaryLine, overloadedCount);
    }

    private static string FormatStatus(double load, double budget, AttentionDegradation? deg)
    {
        if (deg is null)
        {
            return "ATT: —";
        }

        if (deg.SimplerDecisions)
        {
            return "ATT: CRITICAL";
        }

        if (deg.NarrowedFocus)
        {
            return "ATT: HIGH";
        }

        if (deg.SlowerReactions)
        {
            return "ATT: ELEVATED";
        }

        return budget <= 0 ? "ATT: —" : $"ATT: {load / budget * 100:0}%";
    }

    private static string FormatLoadBadge(double load, double budget)
    {
        if (budget <= 0)
        {
            return "LOAD: —";
        }

        return $"LOAD: {load:0.0}/{budget:0.0}";
    }
}

/// <summary>Display row for a single agent's attention state.</summary>
public sealed record AgentAttentionRow(
    string AgentId,
    double Budget,
    double Load,
    bool IsOverloaded,
    string StatusLabel,
    string LoadBadge)
{
    public static AgentAttentionRow Unknown { get; } =
        new(string.Empty, 0, 0, false, "ATT: —", "LOAD: —");
}

/// <summary>Aggregate summary across all agents.</summary>
public sealed record AgentAttentionSummary(
    IReadOnlyList<AgentAttentionRow> Rows,
    string SummaryLine,
    int OverloadedCount)
{
    public static AgentAttentionSummary Empty { get; } =
        new(Array.Empty<AgentAttentionRow>(), "ATTENTION: no agents", 0);
}
