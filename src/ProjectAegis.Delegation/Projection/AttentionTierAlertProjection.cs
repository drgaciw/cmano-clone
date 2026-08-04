namespace ProjectAegis.Delegation.Projection;

using ProjectAegis.Delegation.Attention;

/// <summary>
/// S109-03 / AGD-13: emit attributable alerts only when an agent's named attention tier
/// changes. Consumes projected rows — never recomputes attention.
/// </summary>
public static class AttentionTierAlertProjection
{
    public const string Category = "AttentionTier";

    /// <summary>
    /// Diff previous vs current projected rows keyed by agent id.
    /// Emits one alert per agent whose <see cref="AgentAttentionRow.Tier"/> changed
    /// (including first sample after none). Unchanged tiers produce no alert.
    /// Ordering is deterministic by agent id.
    /// </summary>
    public static IReadOnlyList<AttentionTierAlert> Diff(
        IReadOnlyDictionary<string, AgentAttentionRow>? previous,
        IReadOnlyList<AgentAttentionRow>? current)
    {
        if (current is null || current.Count == 0)
        {
            return Array.Empty<AttentionTierAlert>();
        }

        previous ??= new Dictionary<string, AgentAttentionRow>(StringComparer.Ordinal);
        var alerts = new List<AttentionTierAlert>();

        foreach (var row in current.OrderBy(r => r.AgentId, StringComparer.Ordinal))
        {
            if (string.IsNullOrEmpty(row.AgentId) || !row.HasSample)
            {
                continue;
            }

            previous.TryGetValue(row.AgentId, out var prior);
            var priorTier = prior is { HasSample: true }
                ? prior.Tier
                : (AttentionTierName?)null;

            if (priorTier == row.Tier)
            {
                continue; // no transition — suppress spam
            }

            // First observation of Nominal is not a "crossing" worth alerting.
            if (priorTier is null && row.Tier == AttentionTierName.Nominal)
            {
                continue;
            }

            alerts.Add(BuildAlert(row, priorTier));
        }

        return alerts;
    }

    public static AttentionTierAlert BuildAlert(AgentAttentionRow current, AttentionTierName? priorTier)
    {
        var priorLabel = priorTier is null
            ? AttentionTierNaming.UnknownDisplay
            : AttentionTierNaming.DisplayName(priorTier.Value);
        var newLabel = AttentionTierNaming.DisplayName(current.Tier);
        var severity = current.Tier switch
        {
            AttentionTierName.SimplerDecisions => AlertSeverity.Critical,
            AttentionTierName.NarrowedFocus => AlertSeverity.Notable,
            AttentionTierName.SlowerReactions => AlertSeverity.Notable,
            _ => AlertSeverity.Routine,
        };

        var text =
            $"ATTENTION tier {priorLabel} → {newLabel} on agent {current.AgentId} " +
            $"({current.Load:0.0}/{current.Budget:0.0})";

        var accessible =
            $"Attention tier changed for agent {current.AgentId} from {priorLabel} to {newLabel}. " +
            $"Load {current.Load:0.0} of budget {current.Budget:0.0}.";

        return new AttentionTierAlert(
            AgentId: current.AgentId,
            PriorTier: priorTier,
            NewTier: current.Tier,
            Load: current.Load,
            Budget: current.Budget,
            Text: text,
            AccessibleText: accessible,
            Severity: severity,
            SeverityLabel: severity.ToString());
    }

    public static MessageLogLine ToMessageLogLine(AttentionTierAlert alert, ulong sequenceId, double simTime) =>
        new(
            SequenceId: sequenceId,
            SimTime: simTime,
            Category: Category,
            Text: alert.Text,
            UnitId: alert.AgentId);
}

/// <summary>One attributable attention-tier transition alert.</summary>
public sealed record AttentionTierAlert(
    string AgentId,
    AttentionTierName? PriorTier,
    AttentionTierName NewTier,
    double Load,
    double Budget,
    string Text,
    string AccessibleText,
    AlertSeverity Severity,
    string SeverityLabel);
