namespace ProjectAegis.Delegation.AfterAction;

using System.Globalization;
using System.Text;
using Decision;

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
/// Infrastructure denial ids (<c>comms-grid</c>, <c>comms-guard</c>) are not rows; those
/// denials are credited to the agent that owned the unit when the denial was logged.
/// </summary>
public static class AgentPerformanceAarProjection
{
    /// <summary>Controller kind written when a human takes a unit back from an agent.</summary>
    public const string HumanControllerKind = "Human";

    /// <summary>Synthetic agent id used by <c>FilterOffGridHumanOrders</c> (C3-01).</summary>
    public const string CommsGridAgentId = "comms-grid";

    /// <summary>Synthetic agent id used when the engage-phase communications guard blocks an order.</summary>
    public const string CommsGuardAgentId = "comms-guard";

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
            var agentId = ResolveDenialAgentId(log, denial);
            if (agentId is null)
            {
                continue;
            }

            Increment(denials, agentId);
        }

        foreach (var change in log.ControllerChanges)
        {
            if (change.AgentId is { } agent
                && string.Equals(change.NewKind, HumanControllerKind, StringComparison.Ordinal)
                && !string.Equals(change.PreviousKind, HumanControllerKind, StringComparison.Ordinal)
                && !IsInfrastructureAgent(agent.Value))
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

    /// <summary>
    /// Maps a denial onto a real agent. Infrastructure ids never become rows of their own;
    /// when the denial was logged under one of them, the unit's owning agent at that tick
    /// receives the credit (latest intent for the unit at or before the denial tick, else
    /// the last non-human controller change). Human-only off-grid denials are dropped.
    /// </summary>
    private static string? ResolveDenialAgentId(IReadOnlyOrderLog log, PolicyDenialRecord denial)
    {
        var raw = denial.AgentId.Value;
        if (!IsInfrastructureAgent(raw))
        {
            return raw;
        }

        string? fromIntent = null;
        foreach (var record in log.Records)
        {
            if (record.SimTick <= denial.SimTick
                && string.Equals(record.TargetId.Value, denial.TargetId.Value, StringComparison.Ordinal)
                && !IsInfrastructureAgent(record.AgentId.Value))
            {
                fromIntent = record.AgentId.Value;
            }
        }

        if (fromIntent is not null)
        {
            return fromIntent;
        }

        string? fromController = null;
        foreach (var change in log.ControllerChanges)
        {
            if (change.SimTime > denial.SimTime
                || !string.Equals(change.TargetId.Value, denial.TargetId.Value, StringComparison.Ordinal)
                || change.AgentId is not { } controllerAgent
                || IsInfrastructureAgent(controllerAgent.Value))
            {
                continue;
            }

            if (string.Equals(change.NewKind, HumanControllerKind, StringComparison.Ordinal))
            {
                fromController = null;
                continue;
            }

            fromController = controllerAgent.Value;
        }

        return fromController;
    }

    private static bool IsInfrastructureAgent(string agentId) =>
        string.Equals(agentId, CommsGridAgentId, StringComparison.Ordinal)
        || string.Equals(agentId, CommsGuardAgentId, StringComparison.Ordinal);

    private static void Increment(Dictionary<string, int> map, string key) =>
        map[key] = map.TryGetValue(key, out var n) ? n + 1 : 1;

    private static int Get(Dictionary<string, int> map, string key) =>
        map.TryGetValue(key, out var n) ? n : 0;
}
