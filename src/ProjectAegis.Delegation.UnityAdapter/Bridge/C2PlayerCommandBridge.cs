namespace ProjectAegis.Delegation.UnityAdapter.Bridge;

using ProjectAegis.Delegation.Controllers;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Input;

/// <summary>
/// Player command issuance facade over <see cref="DelegationBridge.TryEnqueueHumanOrder"/> (CMD-31).
/// Returns structured failure reasons for UI tooltips / status labels.
/// </summary>
public static class C2PlayerCommandBridge
{
    public const string ReasonReplayAttached = "REPLAY_ATTACHED";
    public const string ReasonNotHumanControl = "NOT_HUMAN_CONTROL";
    public const string ReasonUnknownUnit = "UNKNOWN_UNIT";
    public const string ReasonEnqueueFailed = "ENQUEUE_FAILED";

    /// <summary>
    /// Resolve <paramref name="commandId"/> and enqueue a human order for <paramref name="entity"/>.
    /// Does not mutate control slots — unit must already be under <see cref="HumanController"/>.
    /// </summary>
    public static bool TryIssue(
        DelegationBridge bridge,
        EntityKey entity,
        string commandId,
        double simTime,
        out string? failureReason)
    {
        failureReason = null;

        if (bridge is null)
        {
            failureReason = ReasonUnknownUnit;
            return false;
        }

        if (!C2CommandIssuance.TryResolve(commandId, out var kind, out var resolveReason))
        {
            failureReason = resolveReason ?? C2CommandIssuance.ReasonUnknownCommand;
            return false;
        }

        if (bridge.AttachReplayViewer)
        {
            failureReason = ReasonReplayAttached;
            return false;
        }

        if (!bridge.Registry.TryGetBinding(entity, out var binding))
        {
            failureReason = ReasonUnknownUnit;
            return false;
        }

        if (binding.Target.Slot.Active is not HumanController)
        {
            failureReason = ReasonNotHumanControl;
            return false;
        }

        if (!bridge.TryEnqueueHumanOrder(entity, kind, simTime))
        {
            failureReason = ReasonEnqueueFailed;
            return false;
        }

        return true;
    }
}
