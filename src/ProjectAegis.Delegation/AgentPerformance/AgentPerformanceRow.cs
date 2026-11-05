namespace ProjectAegis.Delegation.AgentPerformance;

using Core;

/// <summary>Immutable metrics attributed to one agent in a completed run's canonical order log.</summary>
/// <param name="AgentId">The explicit agent identity recorded by the log.</param>
/// <param name="IntentCount">Number of chosen intent events, including intents later denied by policy.</param>
/// <param name="PolicyDenialCount">Number of policy denial events attributed to this agent.</param>
/// <param name="OverrideCount">Number of explicitly attributed Agent-to-Human controller changes.</param>
public sealed record AgentPerformanceRow(
    AgentId AgentId,
    int IntentCount,
    int PolicyDenialCount,
    int OverrideCount)
{
    /// <summary>
    /// Human interventions per chosen intent: OverrideCount / IntentCount; null when no intent was chosen.
    /// This frequency may exceed one: controller changes are not linked to individual intents in the log.
    /// </summary>
    public double? OverrideRate => IntentCount == 0 ? null : (double)OverrideCount / IntentCount;
}
