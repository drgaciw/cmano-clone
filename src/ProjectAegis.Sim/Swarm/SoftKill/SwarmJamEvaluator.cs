namespace ProjectAegis.Sim.Swarm.SoftKill;

/// <summary>
/// Pure deterministic jam soft-kill rules (SWARM-18 / DRG-107).
/// Maps severity → C2 <see cref="SwarmLinkState"/>; does not touch CEC mesh.
/// </summary>
public static class SwarmJamEvaluator
{
    public const string ReasonDegraded = "soft-kill-jam-degraded";
    public const string ReasonLost = "soft-kill-jam-lost";
    public const string ReasonClear = "soft-kill-jam-clear";

    /// <summary>Map jam severity to linkState (None → Connected for recovery).</summary>
    public static SwarmLinkState LinkStateForSeverity(SwarmJamSeverity severity) =>
        severity switch
        {
            SwarmJamSeverity.Lost => SwarmLinkState.Lost,
            SwarmJamSeverity.Degraded => SwarmLinkState.Degraded,
            _ => SwarmLinkState.Connected,
        };

    /// <summary>Explicit reason string for the severity (empty for None).</summary>
    public static string ReasonForSeverity(SwarmJamSeverity severity) =>
        severity switch
        {
            SwarmJamSeverity.Lost => ReasonLost,
            SwarmJamSeverity.Degraded => ReasonDegraded,
            _ => ReasonClear,
        };
}
