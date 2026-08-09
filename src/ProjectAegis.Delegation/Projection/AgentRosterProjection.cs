namespace ProjectAegis.Delegation.Projection;

using ProjectAegis.Delegation.Core;

/// <summary>
/// Pure projection of controller bindings into agent roster rows (CMD-37).
/// No Unity / orchestrator dependency — callers supply simple tuples or lookup funcs.
/// </summary>
public static class AgentRosterProjection
{
    public const string DefaultAttentionLabel = "ATTENTION: —";
    public const string MissingAgentId = "—";

    /// <summary>
    /// Project roster rows from simple controller-binding tuples.
    /// <paramref name="rows"/> items: unitId, controllerKind (Agent|Human|AgentSuspended|None), agentId, autonomy label.
    /// </summary>
    public static IReadOnlyList<AgentRosterEntry> Project(
        IReadOnlyList<(string unitId, string controllerKind, string? agentId, string autonomy)> rows)
    {
        if (rows is null || rows.Count == 0)
        {
            return Array.Empty<AgentRosterEntry>();
        }

        var result = new List<AgentRosterEntry>(rows.Count);
        foreach (var row in rows.OrderBy(r => r.unitId, StringComparer.Ordinal))
        {
            result.Add(ToEntry(row.unitId, row.controllerKind, row.agentId, row.autonomy, attention: null));
        }

        return result;
    }

    /// <summary>
    /// Project with optional attention labels (null/empty → <see cref="DefaultAttentionLabel"/>).
    /// </summary>
    public static IReadOnlyList<AgentRosterEntry> Project(
        IReadOnlyList<(string unitId, string controllerKind, string? agentId, string autonomy, string? attention)> rows)
    {
        if (rows is null || rows.Count == 0)
        {
            return Array.Empty<AgentRosterEntry>();
        }

        var result = new List<AgentRosterEntry>(rows.Count);
        foreach (var row in rows.OrderBy(r => r.unitId, StringComparer.Ordinal))
        {
            result.Add(ToEntry(row.unitId, row.controllerKind, row.agentId, row.autonomy, row.attention));
        }

        return result;
    }

    /// <summary>
    /// Walk unit ids with Func lookups (easy host / test wiring without full orchestrator).
    /// </summary>
    public static IReadOnlyList<AgentRosterEntry> Project(
        IEnumerable<string> unitIds,
        Func<string, string> controllerKind,
        Func<string, string?> agentId,
        Func<string, string> autonomy,
        Func<string, string?>? attention = null)
    {
        if (unitIds is null)
        {
            return Array.Empty<AgentRosterEntry>();
        }

        var list = unitIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToList();

        if (list.Count == 0)
        {
            return Array.Empty<AgentRosterEntry>();
        }

        var result = new List<AgentRosterEntry>(list.Count);
        foreach (var id in list)
        {
            result.Add(ToEntry(
                id,
                controllerKind(id) ?? "None",
                agentId(id),
                autonomy(id) ?? string.Empty,
                attention?.Invoke(id)));
        }

        return result;
    }

    /// <summary>
    /// Walk <see cref="TargetId"/> list with Func lookups.
    /// </summary>
    public static IReadOnlyList<AgentRosterEntry> Project(
        IEnumerable<TargetId> unitIds,
        Func<TargetId, string> controllerKind,
        Func<TargetId, string?> agentId,
        Func<TargetId, string> autonomy,
        Func<TargetId, string?>? attention = null)
    {
        if (unitIds is null)
        {
            return Array.Empty<AgentRosterEntry>();
        }

        var list = unitIds
            .Select(id => id.Value)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(id => id, StringComparer.Ordinal)
            .Select(id => new TargetId(id))
            .ToList();

        if (list.Count == 0)
        {
            return Array.Empty<AgentRosterEntry>();
        }

        var result = new List<AgentRosterEntry>(list.Count);
        foreach (var id in list)
        {
            result.Add(ToEntry(
                id.Value,
                controllerKind(id) ?? "None",
                agentId(id),
                autonomy(id) ?? string.Empty,
                attention?.Invoke(id)));
        }

        return result;
    }

    /// <summary>Map <see cref="AutonomyLevel"/> to a stable label string.</summary>
    public static string FormatAutonomyLabel(AutonomyLevel level) => level.ToString();

    private static AgentRosterEntry ToEntry(
        string unitId,
        string controllerKind,
        string? agentId,
        string autonomy,
        string? attention)
    {
        var kind = string.IsNullOrWhiteSpace(controllerKind) ? "None" : controllerKind.Trim();
        var resolvedAgent = string.IsNullOrWhiteSpace(agentId) ? MissingAgentId : agentId!.Trim();
        var autonomyLabel = string.IsNullOrWhiteSpace(autonomy) ? "—" : autonomy.Trim();
        var attentionLabel = string.IsNullOrWhiteSpace(attention)
            ? DefaultAttentionLabel
            : attention!.Trim().StartsWith("ATTENTION:", StringComparison.OrdinalIgnoreCase)
                ? attention.Trim()
                : $"ATTENTION: {attention.Trim()}";

        var (status, mode) = MapStatusAndMode(kind, hasAgent: resolvedAgent != MissingAgentId, attentionLabel);
        return new AgentRosterEntry(
            AgentId: resolvedAgent,
            UnitId: unitId,
            AutonomyLabel: autonomyLabel,
            StatusLabel: status,
            AttentionLabel: attentionLabel,
            ModeLabel: mode);
    }

    private static (string Status, string Mode) MapStatusAndMode(
        string controllerKind,
        bool hasAgent,
        string attentionLabel)
    {
        // Explicit attention status when load exceeds budget (or host marked Attention).
        if (attentionLabel.IndexOf("Overloaded", StringComparison.OrdinalIgnoreCase) >= 0
            || string.Equals(controllerKind, "Attention", StringComparison.OrdinalIgnoreCase))
        {
            return ("Attention", NormalizeMode(controllerKind));
        }

        return controllerKind.ToLowerInvariant() switch
        {
            "agent" => ("Active", "Agent"),
            "human" => (hasAgent ? "Suspended" : "Active", "Human"),
            "agentsuspended" => ("Suspended", "AgentSuspended"),
            "none" => ("—", "None"),
            _ => (hasAgent ? "Active" : "—", NormalizeMode(controllerKind)),
        };
    }

    private static string NormalizeMode(string controllerKind)
    {
        if (string.IsNullOrWhiteSpace(controllerKind))
        {
            return "None";
        }

        // Preserve known casing; otherwise passthrough trimmed.
        return controllerKind.Trim() switch
        {
            var k when k.Equals("agent", StringComparison.OrdinalIgnoreCase) => "Agent",
            var k when k.Equals("human", StringComparison.OrdinalIgnoreCase) => "Human",
            var k when k.Equals("agentsuspended", StringComparison.OrdinalIgnoreCase) => "AgentSuspended",
            var k when k.Equals("none", StringComparison.OrdinalIgnoreCase) => "None",
            _ => controllerKind.Trim(),
        };
    }
}
