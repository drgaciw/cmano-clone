namespace ProjectAegis.Delegation.Input;

using ProjectAegis.Delegation.Core;

/// <summary>Agent directive action kinds (CMD-37).</summary>
public enum AgentDirectiveAction
{
    /// <summary>Player takes direct control (mode change).</summary>
    TakeControl,

    /// <summary>Return control to the suspended agent (mode change).</summary>
    ReturnToAgent,

    /// <summary>Hold order (reuses human enqueue path).</summary>
    Hold,

    /// <summary>Return-to-base order (reuses human enqueue path).</summary>
    Rtb,
}

/// <summary>
/// Structured directive request produced by pure validation — hosts execute mode changes
/// or enqueue <see cref="OrderKind"/> via the bridge.
/// </summary>
public sealed record AgentDirectiveRequest(
    string DirectiveId,
    AgentDirectiveAction Action,
    bool IsModeChange,
    OrderKind? OrderKind);

/// <summary>Result of pure directive validation.</summary>
public readonly record struct AgentDirectiveResult(
    bool Ok,
    string? FailureReason,
    AgentDirectiveRequest? Request);

/// <summary>
/// Pure validation + directive-id mapping for agent roster issuance (CMD-37).
/// No sim / bridge side effects — hosts call this then apply via mode change or enqueue.
/// </summary>
public static class AgentDirectiveIssuance
{
    public const string ReasonNoSelection = "NO_SELECTION";
    public const string ReasonNoAgent = "NO_AGENT";
    public const string ReasonUnknownDirective = "UNKNOWN_DIRECTIVE";

    public const string TakeControlId = "take_control";
    public const string ReturnToAgentId = "return_to_agent";
    public const string HoldId = "hold";
    public const string RtbId = "rtb";

    /// <summary>
    /// Map a directive id string to a structured request (no selection/agent checks).
    /// Unknown or empty ids fail with <see cref="ReasonUnknownDirective"/>.
    /// </summary>
    public static bool TryResolve(string? directiveId, out AgentDirectiveRequest request, out string? reason)
    {
        request = null!;
        reason = null;

        if (string.IsNullOrWhiteSpace(directiveId))
        {
            reason = ReasonUnknownDirective;
            return false;
        }

        var id = directiveId.Trim().ToLowerInvariant();
        switch (id)
        {
            case TakeControlId:
                request = new AgentDirectiveRequest(TakeControlId, AgentDirectiveAction.TakeControl, IsModeChange: true, OrderKind: null);
                return true;
            case ReturnToAgentId:
                request = new AgentDirectiveRequest(ReturnToAgentId, AgentDirectiveAction.ReturnToAgent, IsModeChange: true, OrderKind: null);
                return true;
            case HoldId:
                request = new AgentDirectiveRequest(HoldId, AgentDirectiveAction.Hold, IsModeChange: false, OrderKind.Hold);
                return true;
            case RtbId:
                request = new AgentDirectiveRequest(RtbId, AgentDirectiveAction.Rtb, IsModeChange: false, OrderKind.ReturnToBase);
                return true;
            default:
                reason = ReasonUnknownDirective;
                return false;
        }
    }

    /// <summary>
    /// Full pre-issue validation: selection required, then directive resolution, then agent presence for mode changes.
    /// </summary>
    /// <param name="directiveId">Directive id (take_control | return_to_agent | hold | rtb).</param>
    /// <param name="hasSelection">True when a unit is selected.</param>
    /// <param name="hasAgent">
    /// True when the selection has a related agent (active or suspended).
    /// Required for mode-change directives; order directives only need selection.
    /// </param>
    public static AgentDirectiveResult Validate(string? directiveId, bool hasSelection, bool hasAgent)
    {
        if (!hasSelection)
        {
            return new AgentDirectiveResult(false, ReasonNoSelection, null);
        }

        if (!TryResolve(directiveId, out var request, out var reason))
        {
            return new AgentDirectiveResult(false, reason ?? ReasonUnknownDirective, null);
        }

        if (request.IsModeChange && !hasAgent)
        {
            return new AgentDirectiveResult(false, ReasonNoAgent, null);
        }

        return new AgentDirectiveResult(true, null, request);
    }
}
