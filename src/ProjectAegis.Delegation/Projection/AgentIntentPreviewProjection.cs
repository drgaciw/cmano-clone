namespace ProjectAegis.Delegation.Projection;

using ProjectAegis.Delegation.Core;

/// <summary>
/// Req 20 P0: Assisted-mode intent-preview ghost. Pure projection — UI draws a ghost course/engage
/// affordance before commit; no sim mutation (ADR-010).
/// </summary>
public sealed record AgentIntentPreview(
    string UnitId,
    bool ShowGhost,
    string Detail);

/// <summary>Builds assisted intent-preview visibility from delegation state + optional engage preview.</summary>
public static class AgentIntentPreviewProjection
{
    /// <summary>
    /// Ghost is shown when the unit is agent-owned (or mixed), autonomy is
    /// <see cref="AutonomyLevel.Assisted"/>, and the agent is not paused.
    /// </summary>
    public static AgentIntentPreview Project(
        DelegationStateProjection state,
        EngagePreview? engagePreview = null)
    {
        if (state is null)
        {
            throw new ArgumentNullException(nameof(state));
        }

        var agentOwned = state.Owner is DelegationOwnerKind.Agent or DelegationOwnerKind.Mixed;
        var assisted = state.AutonomyLevel == AutonomyLevel.Assisted;
        var show = agentOwned && assisted && !state.Paused;

        string detail;
        if (!show)
        {
            detail = string.Empty;
        }
        else if (engagePreview is null)
        {
            detail = "ASSISTED_INTENT";
        }
        else if (!engagePreview.CanFire)
        {
            detail = engagePreview.AbortPreviewCode ?? "ASSISTED_DENY";
        }
        else
        {
            detail = engagePreview.DlzLabel;
        }

        return new AgentIntentPreview(state.UnitId, show, detail);
    }
}
