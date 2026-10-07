namespace ProjectAegis.Delegation.UnityAdapter.Presentation;

using Bridge;
using Input;

/// <summary>
/// Observer / read-only chrome for <see cref="DelegationBridge.AttachReplayViewer"/> (DRG-249, MODE-03).
/// Presentation-only (ADR-010 §2–3, ADR-007): reads the viewer flag and routes order attempts through
/// <see cref="C2PlayerCommandBridge.TryIssue"/>; never sets the flag or touches controller slots.
/// </summary>
/// <param name="IsObserver">True while a replay viewer is attached.</param>
/// <param name="ModeLabel">Top-bar mode text (<see cref="ObserverLabel"/> or <see cref="CommandLabel"/>).</param>
/// <param name="CssClass">Top-bar mode USS class.</param>
/// <param name="CanIssueOrders">Whether order buttons are enabled.</param>
/// <param name="CanDelegate">Whether assign / take-control / return-to-agent are enabled.</param>
/// <param name="DisabledReasonCode">Stable reason code when commands are disabled; otherwise null.</param>
/// <param name="DisabledReasonText">Player-visible reason when commands are disabled; otherwise null.</param>
public sealed record ObserverModeChromePresentation(
    bool IsObserver,
    string ModeLabel,
    string CssClass,
    bool CanIssueOrders,
    bool CanDelegate,
    string? DisabledReasonCode,
    string? DisabledReasonText)
{
    /// <summary>Mode label while a replay viewer is attached.</summary>
    public const string ObserverLabel = "OBSERVER · READ-ONLY";

    /// <summary>Mode label when the player can command.</summary>
    public const string CommandLabel = "COMMAND";

    /// <summary>USS class for observer chrome.</summary>
    public const string ObserverCssClass = "c2-topbar-item--mode-observer";

    /// <summary>USS class for command chrome.</summary>
    public const string CommandCssClass = "c2-topbar-item--mode-command";

    /// <summary>Visible reason for <see cref="C2PlayerCommandBridge.ReasonReplayAttached"/>.</summary>
    public const string ReplayAttachedReasonText = "Observer mode: replay viewer attached — orders and delegation are disabled.";

    private static readonly ObserverModeChromePresentation Observer = new(
        true,
        ObserverLabel,
        ObserverCssClass,
        CanIssueOrders: false,
        CanDelegate: false,
        C2PlayerCommandBridge.ReasonReplayAttached,
        ReplayAttachedReasonText);

    private static readonly ObserverModeChromePresentation Command = new(
        false,
        CommandLabel,
        CommandCssClass,
        CanIssueOrders: true,
        CanDelegate: true,
        DisabledReasonCode: null,
        DisabledReasonText: null);

    /// <summary>Project chrome from the bridge's <see cref="DelegationBridge.AttachReplayViewer"/> flag.</summary>
    /// <exception cref="ArgumentNullException">When bridge is null.</exception>
    public static ObserverModeChromePresentation Project(DelegationBridge bridge)
    {
        if (bridge is null)
        {
            throw new ArgumentNullException(nameof(bridge));
        }

        return Project(bridge.AttachReplayViewer);
    }

    /// <summary>Project chrome from a viewer flag.</summary>
    public static ObserverModeChromePresentation Project(bool attachReplayViewer) =>
        attachReplayViewer ? Observer : Command;

    /// <summary>
    /// Attempt a player order via <see cref="C2PlayerCommandBridge.TryIssue"/> and return a visible outcome.
    /// While observing, the attempt is refused with <see cref="C2PlayerCommandBridge.ReasonReplayAttached"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException">When bridge is null.</exception>
    public static ObserverOrderAttempt TryIssueOrder(
        DelegationBridge bridge,
        EntityKey entity,
        string commandId,
        double simTime)
    {
        if (bridge is null)
        {
            throw new ArgumentNullException(nameof(bridge));
        }

        if (C2PlayerCommandBridge.TryIssue(bridge, entity, commandId, simTime, out var reason))
        {
            return ObserverOrderAttempt.Ok;
        }

        var code = reason ?? C2PlayerCommandBridge.ReasonEnqueueFailed;
        return new ObserverOrderAttempt(false, code, DescribeReason(code));
    }

    /// <summary>Player-visible text for an order refusal reason code.</summary>
    public static string DescribeReason(string reasonCode) =>
        reasonCode switch
        {
            C2PlayerCommandBridge.ReasonReplayAttached => ReplayAttachedReasonText,
            C2PlayerCommandBridge.ReasonNotHumanControl => "Order refused: unit is not under direct control.",
            C2PlayerCommandBridge.ReasonUnknownUnit => "Order refused: unit not found.",
            C2PlayerCommandBridge.ReasonEnqueueFailed => "Order refused: could not queue order.",
            C2CommandIssuance.ReasonUnknownCommand => "Order refused: unknown command.",
            _ => $"Order refused: {reasonCode}",
        };
}

/// <summary>Outcome of an order attempt with a player-visible reason (DRG-249).</summary>
/// <param name="Accepted">True when the order was queued.</param>
/// <param name="ReasonCode">Refusal reason code; null when accepted.</param>
/// <param name="ReasonText">Player-visible refusal text; null when accepted.</param>
public sealed record ObserverOrderAttempt(bool Accepted, string? ReasonCode, string? ReasonText)
{
    /// <summary>Accepted attempt.</summary>
    public static ObserverOrderAttempt Ok { get; } = new(true, null, null);
}
