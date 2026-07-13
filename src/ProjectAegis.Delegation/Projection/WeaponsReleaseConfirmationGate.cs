namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// Pure confirm/cancel decision for the weapons-release confirmation gate (req 20 §Order lifecycle,
/// TR-c2-006; GDD command-and-control-ui.md — "Weapons-release intents pass a confirmation gate when
/// ROE/doctrine requires positive control (policy projection flag); Enter confirms, Esc cancels — cancel
/// emits no intent." — see also c2-command-post.md UX interaction map).
/// </summary>
/// <remarks>
/// ADR-010: this function only decides WHETHER to emit; it never calls the bridge itself. The Unity
/// host wires Enter/Esc key input to <see cref="GateAction"/> and, on a <c>true</c> result, calls the
/// existing bridge command API (e.g. <c>DelegationBridge.TryEnqueueAttackOption</c>) as a logged
/// intent — the same path an unconfirmed fire order would already use. Cancelling never touches the
/// bridge, so no intent is ever logged for a cancelled gate.
/// </remarks>
public static class WeaponsReleaseConfirmationGate
{
    public enum GateAction
    {
        Confirm,
        Cancel,
    }

    /// <summary>
    /// True when the pending weapons-release intent should be emitted (i.e. the host should call the
    /// bridge). When <paramref name="positiveControlRequired"/> is false the gate does not apply and
    /// the intent always emits regardless of <paramref name="action"/>. When true, only
    /// <see cref="GateAction.Confirm"/> emits — <see cref="GateAction.Cancel"/> always returns false.
    /// </summary>
    public static bool ShouldEmit(bool positiveControlRequired, GateAction action) =>
        !positiveControlRequired || action == GateAction.Confirm;
}
