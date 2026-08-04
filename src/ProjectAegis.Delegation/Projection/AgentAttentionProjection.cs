namespace ProjectAegis.Delegation.Projection;

using ProjectAegis.Delegation.Attention;
using ProjectAegis.Delegation.Controllers;

/// <summary>
/// S109-01 / DRG-67: projects decision-time <see cref="AttentionEvaluation"/> into
/// a stable UI contract. Unity hosts bind labels only — they never re-derive load or tier.
/// </summary>
public static class AgentAttentionProjection
{
    public const string NoSampleStatus = "ATT: —";
    public const string NoSampleLoadBadge = "LOAD: —";

    /// <summary>
    /// Project a single agent's attention evaluation into a display row.
    /// <paramref name="evaluation"/> is the exact decision-time sample (or null when
    /// the agent has not yet ticked).
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
        var hasSample = evaluation is not null;
        var load = evaluation?.Load ?? 0.0;
        // Prefer evaluation budget when present (decision-time truth); fall back to controller.
        var sampleBudget = evaluation?.Budget ?? budget;
        var tier = hasSample
            ? AttentionTierNaming.FromEvaluation(evaluation)
            : AttentionTierName.Nominal;
        var isOverloaded = evaluation?.IsOverloaded ?? false;

        return new AgentAttentionRow(
            AgentId: agent.Id.Value,
            Budget: sampleBudget,
            Load: load,
            IsOverloaded: isOverloaded,
            HasSample: hasSample,
            Tier: tier,
            TierLabel: hasSample ? AttentionTierNaming.DisplayName(tier) : AttentionTierNaming.UnknownDisplay,
            StatusLabel: FormatStatus(hasSample, load, sampleBudget, tier),
            LoadBadge: FormatLoadBadge(hasSample, load, sampleBudget),
            AccessibleLabel: hasSample
                ? AttentionTierNaming.AccessibleLabel(tier, load, sampleBudget)
                : "Attention sample unavailable");
    }

    /// <summary>
    /// Project from a raw decision-time evaluation without an <see cref="AgentController"/>
    /// (explain / historic paths).
    /// </summary>
    public static AgentAttentionRow ProjectDecisionTime(
        string agentId,
        AttentionEvaluation evaluation)
    {
        if (evaluation is null)
        {
            return AgentAttentionRow.Unknown with { AgentId = agentId ?? string.Empty };
        }

        var tier = AttentionTierNaming.FromEvaluation(evaluation);
        return new AgentAttentionRow(
            AgentId: agentId ?? string.Empty,
            Budget: evaluation.Budget,
            Load: evaluation.Load,
            IsOverloaded: evaluation.IsOverloaded,
            HasSample: true,
            Tier: tier,
            TierLabel: AttentionTierNaming.DisplayName(tier),
            StatusLabel: FormatStatus(true, evaluation.Load, evaluation.Budget, tier),
            LoadBadge: FormatLoadBadge(true, evaluation.Load, evaluation.Budget),
            AccessibleLabel: AttentionTierNaming.AccessibleLabel(tier, evaluation.Load, evaluation.Budget));
    }

    /// <summary>
    /// Project from recorded load/budget only (DecisionRecord path) using calculator thresholds.
    /// </summary>
    public static AgentAttentionRow ProjectFromLoadBudget(
        string agentId,
        double load,
        double budget)
    {
        var tier = AttentionTierNaming.FromLoadBudget(load, budget);
        var isOverloaded = budget > 0 && load > budget;
        return new AgentAttentionRow(
            AgentId: agentId ?? string.Empty,
            Budget: budget,
            Load: load,
            IsOverloaded: isOverloaded,
            HasSample: true,
            Tier: tier,
            TierLabel: AttentionTierNaming.DisplayName(tier),
            StatusLabel: FormatStatus(true, load, budget, tier),
            LoadBadge: FormatLoadBadge(true, load, budget),
            AccessibleLabel: AttentionTierNaming.AccessibleLabel(tier, load, budget));
    }

    /// <summary>
    /// Projects a collection of (agent, evaluation) pairs into a summary state.
    /// Ordering is deterministic by agent id (ordinal).
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
        foreach (var (agent, eval) in pairs.OrderBy(p => p.Agent.Id.Value, StringComparer.Ordinal))
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

    private static string FormatStatus(bool hasSample, double load, double budget, AttentionTierName tier)
    {
        if (!hasSample)
        {
            return NoSampleStatus;
        }

        return tier switch
        {
            AttentionTierName.SimplerDecisions => "ATT: SimplerDecisions",
            AttentionTierName.NarrowedFocus => "ATT: NarrowedFocus",
            AttentionTierName.SlowerReactions => "ATT: SlowerReactions",
            _ when budget <= 0 => NoSampleStatus,
            _ => $"ATT: {load / budget * 100:0}% · {AttentionTierNaming.NominalDisplay}",
        };
    }

    private static string FormatLoadBadge(bool hasSample, double load, double budget)
    {
        if (!hasSample || budget <= 0)
        {
            return NoSampleLoadBadge;
        }

        return $"LOAD: {load:0.0}/{budget:0.0}";
    }
}

/// <summary>Display row for a single agent's decision-time attention state (S109-01 contract).</summary>
public sealed record AgentAttentionRow(
    string AgentId,
    double Budget,
    double Load,
    bool IsOverloaded,
    bool HasSample,
    AttentionTierName Tier,
    string TierLabel,
    string StatusLabel,
    string LoadBadge,
    string AccessibleLabel)
{
    public static AgentAttentionRow Unknown { get; } =
        new(
            string.Empty,
            0,
            0,
            false,
            HasSample: false,
            AttentionTierName.Nominal,
            AttentionTierNaming.UnknownDisplay,
            AgentAttentionProjection.NoSampleStatus,
            AgentAttentionProjection.NoSampleLoadBadge,
            "Attention sample unavailable");
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
