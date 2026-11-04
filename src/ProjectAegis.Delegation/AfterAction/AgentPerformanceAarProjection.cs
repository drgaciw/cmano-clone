namespace ProjectAegis.Delegation.AfterAction;

using System.Globalization;
using System.Text;
using ProjectAegis.Delegation.Decision;

/// <summary>One per-agent row of the structured agent-performance AAR section (DRG-254 / INF-01).</summary>
public sealed record AgentPerformanceRow(
    string AgentId,
    int IntentCount,
    int PolicyDenialCount,
    int OverrideCount)
{
    /// <summary>Human overrides divided by intents; 0 when the agent logged no intents.</summary>
    public double OverrideRate => IntentCount == 0 ? 0d : (double)OverrideCount / IntentCount;
}

/// <summary>Agent-performance AAR section; rows are ordered by agent id (ordinal).</summary>
public sealed record AgentPerformanceSection(IReadOnlyList<AgentPerformanceRow> Rows)
{
    public static readonly AgentPerformanceSection Empty = new(Array.Empty<AgentPerformanceRow>());
}

/// <summary>
/// DRG-254 (INF-01, REQ-07 / INF-2.3): per-agent intents, policy denials and override rate
/// from a completed run. Read-only over <see cref="IReadOnlyOrderLog"/> (ADR-019 / INF-2.1):
/// never appends, so the replay fingerprint cannot change. Deterministic for the same log.
/// </summary>
public static class AgentPerformanceAarProjection
{
    /// <summary>Controller kind written when a human takes a unit back from an agent.</summary>
    public const string HumanControllerKind = "Human";

    public static AgentPerformanceSection Project(IReadOnlyOrderLog? log)
    {
        if (log is null)
        {
            return AgentPerformanceSection.Empty;
        }

        var intents = new Dictionary<string, int>(StringComparer.Ordinal);
        var denials = new Dictionary<string, int>(StringComparer.Ordinal);
        var overrides = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var record in log.Records)
        {
            Increment(intents, record.AgentId.Value);
        }

        foreach (var denial in log.PolicyDenials)
        {
            Increment(denials, denial.AgentId.Value);
        }

        foreach (var change in log.ControllerChanges)
        {
            if (change.AgentId is { } agent
                && string.Equals(change.NewKind, HumanControllerKind, StringComparison.Ordinal)
                && !string.Equals(change.PreviousKind, HumanControllerKind, StringComparison.Ordinal))
            {
                Increment(overrides, agent.Value);
            }
        }

        var ids = new SortedSet<string>(StringComparer.Ordinal);
        ids.UnionWith(intents.Keys);
        ids.UnionWith(denials.Keys);
        ids.UnionWith(overrides.Keys);
        if (ids.Count == 0)
        {
            return AgentPerformanceSection.Empty;
        }

        var rows = new List<AgentPerformanceRow>(ids.Count);
        foreach (var id in ids)
        {
            rows.Add(new AgentPerformanceRow(id, Get(intents, id), Get(denials, id), Get(overrides, id)));
        }

        return new AgentPerformanceSection(rows);
    }

    /// <summary>Invariant-culture CSV dump (header + one line per agent) for headless/CLI use.</summary>
    public static string ToCsv(AgentPerformanceSection section)
    {
        var sb = new StringBuilder("agentId,intents,policyDenials,overrides,overrideRate\n");
        foreach (var row in section.Rows)
        {
            sb.Append(row.AgentId).Append(',')
                .Append(row.IntentCount.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(row.PolicyDenialCount.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(row.OverrideCount.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(row.OverrideRate.ToString("0.####", CultureInfo.InvariantCulture)).Append('\n');
        }

        return sb.ToString();
    }

    private static void Increment(Dictionary<string, int> map, string key) =>
        map[key] = map.TryGetValue(key, out var n) ? n + 1 : 1;

    private static int Get(Dictionary<string, int> map, string key) =>
        map.TryGetValue(key, out var n) ? n : 0;
}
