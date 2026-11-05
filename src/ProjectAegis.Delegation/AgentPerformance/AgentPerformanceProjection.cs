namespace ProjectAegis.Delegation.AgentPerformance;

using Core;
using Decision;

/// <summary>Builds structured AAR metrics without mutation, using the ADR-019 read-only order-log boundary.</summary>
public sealed class AgentPerformanceProjection
{
    /// <summary>Projects agent metrics from a completed run's canonical chronological entries.</summary>
    /// <remarks>
    /// Each AgentDecision payload contributes one chosen intent, not one event per scored candidate.
    /// Policy denials remain separate events and do not subtract from chosen intent counts.
    /// Only exact Agent-to-Human transitions are interventions. Releases, assignments and player orders
    /// are excluded. Explicit controller-change AgentId is authoritative; missing attribution is preserved
    /// in the report, never inferred from the target's earlier decisions or controller ownership.
    /// Agents appear only when they have an attributed intent, denial or intervention in this log.
    /// </remarks>
    /// <param name="log">Completed run log; no live simulation state is needed.</param>
    /// <returns>Detached immutable rows with explicit attribution and an unattributed override count.</returns>
    /// <exception cref="ArgumentNullException">The log is null.</exception>
    public AgentPerformanceReport Project(IReadOnlyOrderLog log)
    {
        if (log is null)
        {
            throw new ArgumentNullException(nameof(log));
        }

        var counts = new Dictionary<AgentId, AgentCounts>();
        var unattributedOverrides = 0;
        foreach (var entry in log.ChronologicalEntries())
        {
            switch (entry.Kind)
            {
                case OrderLogEntryKind.AgentDecision when entry.Payload is AgentDecisionPayload decision:
                    GetCounts(decision.AgentId).Intents++;
                    break;
                case OrderLogEntryKind.PolicyDenial when entry.Payload is PolicyDenialRecord denial:
                    GetCounts(denial.AgentId).Denials++;
                    break;
                case OrderLogEntryKind.ControllerChange when entry.Payload is ControllerChangeRecord change
                    && string.Equals(change.PreviousKind, "Agent", StringComparison.Ordinal)
                    && string.Equals(change.NewKind, "Human", StringComparison.Ordinal):
                    if (change.AgentId is { } agentId && !string.IsNullOrWhiteSpace(agentId.Value))
                    {
                        GetCounts(agentId).Overrides++;
                    }
                    else
                    {
                        unattributedOverrides++;
                    }

                    break;
            }
        }

        var rows = counts.OrderBy(pair => pair.Key.Value, StringComparer.Ordinal)
            .Select(pair => new AgentPerformanceRow(pair.Key, pair.Value.Intents, pair.Value.Denials, pair.Value.Overrides));
        return new AgentPerformanceReport(rows, unattributedOverrides);

        AgentCounts GetCounts(AgentId id)
        {
            if (!counts.TryGetValue(id, out var value))
            {
                value = new AgentCounts();
                counts.Add(id, value);
            }

            return value;
        }
    }

    private sealed class AgentCounts
    {
        public int Intents;
        public int Denials;
        public int Overrides;
    }
}
