namespace ProjectAegis.Ai.Host.Authoring;

/// <summary>
/// ADR-015 transparency labels derived from provenance degree (not a hand-set binary).
/// Propose-lane drafts that await human Submit are <see cref="AgentAssisted"/>.
/// </summary>
public static class AuthoringTransparencyLabel
{
    /// <summary>Human-driven session with agent proposals accepted on the Propose lane.</summary>
    public const string AgentAssisted = "agent-assisted";

    /// <summary>Predominantly agent-drafted content (reserved for future bulk-author flows).</summary>
    public const string AgentAuthored = "agent-authored";

    /// <summary>Maps Propose-lane acceptance to the ADR-015 assisted label.</summary>
    public static string ForProposeLane() => AgentAssisted;
}
