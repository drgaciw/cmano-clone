namespace ProjectAegis.Delegation.Projection;

using ProjectAegis.Delegation.Attention;
using ProjectAegis.Delegation.Decision;

/// <summary>
/// S109-05 / AGD-20: decision-time attention as an attributable explain reason.
/// Sources load/budget from <see cref="DecisionRecord"/> (values written when the decision
/// was made) — never from live UI state.
/// </summary>
public static class AttentionExplainProjection
{
    public const string NoRecordLabel = "ATTENTION: (no decision-time sample)";

    public static AttentionExplainSnippet Project(DecisionRecord? record)
    {
        if (record is null)
        {
            return AttentionExplainSnippet.Empty;
        }

        var row = AgentAttentionProjection.ProjectFromLoadBudget(
            record.AgentId.Value,
            record.AttentionLoad,
            record.AttentionBudget);

        var affected = row.Tier != AttentionTierName.Nominal;
        var reason = affected
            ? $"Decision-time attention was {row.TierLabel} " +
              $"(load {row.Load:0.0} / budget {row.Budget:0.0}); degradation applied."
            : $"Decision-time attention was Nominal " +
              $"(load {row.Load:0.0} / budget {row.Budget:0.0}).";

        var statusLine = $"ATTENTION @ decision: {row.TierLabel} · {row.LoadBadge}";

        return new AttentionExplainSnippet(
            AgentId: record.AgentId.Value,
            SimTime: record.SimTime,
            Load: row.Load,
            Budget: row.Budget,
            Tier: row.Tier,
            TierLabel: row.TierLabel,
            AffectedBehavior: affected,
            StatusLine: statusLine,
            ReasonPlain: reason,
            AccessibleLabel: row.AccessibleLabel +
                (affected ? " This attention state affected the decision." : string.Empty));
    }

    /// <summary>
    /// Append attention reason to an existing explain block when attention affected behavior.
    /// </summary>
    public static string CombineWithRationale(string? existingRationale, AttentionExplainSnippet snippet)
    {
        if (!snippet.HasSample)
        {
            return existingRationale ?? string.Empty;
        }

        if (string.IsNullOrWhiteSpace(existingRationale))
        {
            return snippet.ReasonPlain;
        }

        if (!snippet.AffectedBehavior)
        {
            return existingRationale;
        }

        return $"{existingRationale} | {snippet.ReasonPlain}";
    }
}

/// <summary>Explain snippet for decision-time attention (S109-05).</summary>
public sealed record AttentionExplainSnippet(
    string AgentId,
    double SimTime,
    double Load,
    double Budget,
    AttentionTierName Tier,
    string TierLabel,
    bool AffectedBehavior,
    string StatusLine,
    string ReasonPlain,
    string AccessibleLabel)
{
    public bool HasSample => !string.IsNullOrEmpty(AgentId) || Load > 0 || Budget > 0;

    public static AttentionExplainSnippet Empty { get; } =
        new(
            AgentId: string.Empty,
            SimTime: 0,
            Load: 0,
            Budget: 0,
            Tier: AttentionTierName.Nominal,
            TierLabel: AttentionTierNaming.UnknownDisplay,
            AffectedBehavior: false,
            StatusLine: AttentionExplainProjection.NoRecordLabel,
            ReasonPlain: "No decision-time attention sample is available.",
            AccessibleLabel: "No decision-time attention sample is available.");
}
