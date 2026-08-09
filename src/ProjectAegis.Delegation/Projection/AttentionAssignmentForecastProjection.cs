namespace ProjectAegis.Delegation.Projection;

using ProjectAegis.Delegation.Attention;
using ProjectAegis.Delegation.Sim;

/// <summary>
/// S109-04 / AGD-14: advisory forecast of post-assignment attention before commit.
/// Uses <see cref="AttentionCalculator.Evaluate"/> with a hypothetical member count —
/// does not mutate session state and is not a command.
/// </summary>
public static class AttentionAssignmentForecastProjection
{
    public const string AdvisoryPrefix = "FORECAST (advisory)";

    /// <summary>
    /// Forecast attention after assigning <paramref name="additionalMembers"/> units
    /// to an agent with the given budget and current observed state.
    /// </summary>
    public static AttentionAssignmentForecast Forecast(
        string agentId,
        double attentionBudget,
        int currentMemberCount,
        int additionalMembers,
        ObservedState? state)
    {
        if (string.IsNullOrWhiteSpace(agentId))
        {
            return AttentionAssignmentForecast.Unavailable("No agent selected.");
        }

        if (state is null)
        {
            return AttentionAssignmentForecast.Unavailable("Observed state unavailable for forecast.");
        }

        if (additionalMembers < 0)
        {
            return AttentionAssignmentForecast.Unavailable("Assignment delta cannot be negative.");
        }

        var currentEval = AttentionCalculator.Evaluate(attentionBudget, currentMemberCount, state);
        var projectedMembers = currentMemberCount + additionalMembers;
        var projectedEval = AttentionCalculator.Evaluate(attentionBudget, projectedMembers, state);

        var currentRow = AgentAttentionProjection.ProjectDecisionTime(agentId, currentEval);
        var projectedRow = AgentAttentionProjection.ProjectDecisionTime(agentId, projectedEval);
        var tierCrosses = currentRow.Tier != projectedRow.Tier;

        var statusLine =
            $"{AdvisoryPrefix}: {projectedRow.LoadBadge} · tier {projectedRow.TierLabel}" +
            (tierCrosses ? $" (was {currentRow.TierLabel})" : string.Empty);

        var accessible =
            $"Advisory attention forecast for agent {agentId}: " +
            $"projected load {projectedRow.Load:0.0} of budget {projectedRow.Budget:0.0}, " +
            $"tier {projectedRow.TierLabel}" +
            (tierCrosses ? $", up from {currentRow.TierLabel}" : string.Empty) +
            ". Not committed until command is issued.";

        return new AttentionAssignmentForecast(
            AgentId: agentId,
            IsAvailable: true,
            IsAdvisory: true,
            CurrentMemberCount: currentMemberCount,
            ProjectedMemberCount: projectedMembers,
            AdditionalMembers: additionalMembers,
            Current: currentRow,
            Projected: projectedRow,
            TierCrosses: tierCrosses,
            StatusLine: statusLine,
            AccessibleLabel: accessible,
            FailureReason: null);
    }
}

/// <summary>Advisory post-assignment attention forecast (S109-04).</summary>
public sealed record AttentionAssignmentForecast(
    string AgentId,
    bool IsAvailable,
    bool IsAdvisory,
    int CurrentMemberCount,
    int ProjectedMemberCount,
    int AdditionalMembers,
    AgentAttentionRow? Current,
    AgentAttentionRow? Projected,
    bool TierCrosses,
    string StatusLine,
    string AccessibleLabel,
    string? FailureReason)
{
    public static AttentionAssignmentForecast Unavailable(string reason) =>
        new(
            AgentId: string.Empty,
            IsAvailable: false,
            IsAdvisory: true,
            CurrentMemberCount: 0,
            ProjectedMemberCount: 0,
            AdditionalMembers: 0,
            Current: null,
            Projected: null,
            TierCrosses: false,
            StatusLine: $"{AttentionAssignmentForecastProjection.AdvisoryPrefix}: unavailable — {reason}",
            AccessibleLabel: $"Attention forecast unavailable. {reason}",
            FailureReason: reason);
}
