namespace ProjectAegis.Delegation.Projection;

/// <summary>Who currently controls a unit, as shown on the OOB row / map symbol badge (DEL-01).</summary>
public enum ControllerBadgeKind
{
    /// <summary>No controller bound.</summary>
    None = 0,

    /// <summary>Player has direct control.</summary>
    Human = 1,

    /// <summary>An agent controller is active.</summary>
    Agent = 2,

    /// <summary>Agent is bound but suspended with no active controller.</summary>
    AgentSuspended = 3,
}

/// <summary>
/// Presentation-only controller badge. <see cref="Shape"/> and <see cref="ShortText"/> carry the
/// state independently of <see cref="ColorToken"/> so colour is never the only channel.
/// </summary>
public sealed record ControllerBadge(
    string UnitId,
    ControllerBadgeKind Kind,
    string Shape,
    string ShortText,
    string AccessibleLabel,
    string ColorToken,
    string? AgentId,
    bool HasHeldAgent);

/// <summary>
/// Pure projection of <see cref="AgentRosterEntry"/> rows into controller badges (DEL-01).
/// Reads roster output only; never touches controller slots or sim state (ADR-010 §2–3).
/// </summary>
public static class ControllerBadgeProjection
{
    /// <summary>Project one badge per roster row, preserving roster order.</summary>
    public static IReadOnlyList<ControllerBadge> Project(IReadOnlyList<AgentRosterEntry>? roster)
    {
        if (roster is null || roster.Count == 0)
        {
            return Array.Empty<ControllerBadge>();
        }

        var result = new ControllerBadge[roster.Count];
        for (var i = 0; i < roster.Count; i++)
        {
            result[i] = FromRosterEntry(roster[i]);
        }

        return result;
    }

    /// <summary>Bind a single roster row to its controller badge.</summary>
    public static ControllerBadge FromRosterEntry(AgentRosterEntry entry)
    {
        if (entry is null)
        {
            throw new ArgumentNullException(nameof(entry));
        }

        var kind = ParseKind(entry.ModeLabel);
        var agentId = string.IsNullOrWhiteSpace(entry.AgentId) || entry.AgentId == AgentRosterProjection.MissingAgentId
            ? null
            : entry.AgentId;
        var heldAgent = kind == ControllerBadgeKind.Human && agentId is not null;

        return kind switch
        {
            ControllerBadgeKind.Human => new ControllerBadge(
                entry.UnitId,
                kind,
                Shape: "square",
                ShortText: "HUM",
                AccessibleLabel: heldAgent ? $"Human control (agent {agentId} suspended)" : "Human control",
                ColorToken: "badge-human",
                agentId,
                heldAgent),
            ControllerBadgeKind.Agent => new ControllerBadge(
                entry.UnitId,
                kind,
                Shape: "diamond",
                ShortText: "AGT",
                AccessibleLabel: $"Agent control ({agentId ?? AgentRosterProjection.MissingAgentId})",
                ColorToken: "badge-agent",
                agentId,
                HasHeldAgent: false),
            ControllerBadgeKind.AgentSuspended => new ControllerBadge(
                entry.UnitId,
                kind,
                Shape: "diamond-dashed",
                ShortText: "AGT-SUSP",
                AccessibleLabel: $"Agent suspended ({agentId ?? AgentRosterProjection.MissingAgentId})",
                ColorToken: "badge-agent-suspended",
                agentId,
                HasHeldAgent: false),
            _ => new ControllerBadge(
                entry.UnitId,
                ControllerBadgeKind.None,
                Shape: "none",
                ShortText: AgentRosterProjection.MissingAgentId,
                AccessibleLabel: "No controller",
                ColorToken: "badge-none",
                agentId,
                HasHeldAgent: false),
        };
    }

    /// <summary>Parse an <see cref="AgentRosterEntry.ModeLabel"/>; unknown values map to <see cref="ControllerBadgeKind.None"/>.</summary>
    public static ControllerBadgeKind ParseKind(string? modeLabel)
    {
        var mode = modeLabel?.Trim();
        if (string.Equals(mode, "Human", StringComparison.OrdinalIgnoreCase))
        {
            return ControllerBadgeKind.Human;
        }

        if (string.Equals(mode, "Agent", StringComparison.OrdinalIgnoreCase))
        {
            return ControllerBadgeKind.Agent;
        }

        if (string.Equals(mode, "AgentSuspended", StringComparison.OrdinalIgnoreCase))
        {
            return ControllerBadgeKind.AgentSuspended;
        }

        return ControllerBadgeKind.None;
    }

    /// <summary>Roster mode label for a badge kind (inverse of <see cref="ParseKind"/>).</summary>
    public static string FormatKind(ControllerBadgeKind kind) => kind switch
    {
        ControllerBadgeKind.Human => "Human",
        ControllerBadgeKind.Agent => "Agent",
        ControllerBadgeKind.AgentSuspended => "AgentSuspended",
        _ => "None",
    };
}
