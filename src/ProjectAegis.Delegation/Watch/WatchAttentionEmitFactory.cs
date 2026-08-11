namespace ProjectAegis.Delegation.Watch;

using ProjectAegis.Sim.Scenario;
using ProjectAegis.Sim.Sensors;

/// <summary>
/// S116: pure fact → <see cref="WatchAttentionEvent"/> factories.
/// Stable EventIds make queue enqueue idempotent. No Bridge, no RNG, no clock ownership.
/// </summary>
public static class WatchAttentionEmitFactory
{
    public const string ContactEventIdPrefix = "watch:contact:";
    public const string LossEventIdPrefix = "watch:loss:";

    /// <summary>
    /// First detection of a non-own-side contact (hostile or unknown).
    /// Emits only on <see cref="ContactLifecycleState.Unknown"/> → Detected/Classified/Identified.
    /// EventId is stable per <see cref="ContactTransition.TargetId"/>.
    /// </summary>
    public static bool TryFromFirstHostileOrUnknownContact(
        in ContactTransition transition,
        out WatchAttentionEvent? evt)
    {
        evt = null;

        if (transition.PreviousState != ContactLifecycleState.Unknown)
        {
            return false;
        }

        if (transition.NewState is not (
                ContactLifecycleState.Detected
                or ContactLifecycleState.Classified
                or ContactLifecycleState.Identified))
        {
            return false;
        }

        var subject = transition.TargetId;
        if (string.IsNullOrWhiteSpace(subject))
        {
            return false;
        }

        // Own-side tracks are not "hostile/unknown contact" pause-class.
        if (IsOwnSideUnit(subject))
        {
            return false;
        }

        // Hostile (catalog red / synthetic) or unclassified non-blue → pause-class.
        var isHostile = HostileContactFilter.IsEngageableHostileTarget(subject);
        var isUnknownTrack = !isHostile; // non-own, non-hostile engageable ⇒ unknown/neutral track

        var priority = isHostile
            ? WatchAttentionPriority.Critical
            : WatchAttentionPriority.High;

        var eventId = ContactEventIdPrefix + subject;
        var detail = isHostile
            ? $"hostile {transition.PreviousState}->{transition.NewState}"
            : $"unknown {transition.PreviousState}->{transition.NewState}";

        evt = new WatchAttentionEvent(
            eventId,
            WatchAttentionKind.HostileOrUnknownContact,
            priority,
            transition.SimTick,
            subject,
            GroupingKey: string.IsNullOrWhiteSpace(transition.ContactId) ? null : transition.ContactId,
            ReasonDetail: detail);
        return true;
    }

    /// <summary>
    /// Own-side unit loss / battle-damage. EventId stable per unit id.
    /// </summary>
    public static bool TryFromOwnSideLoss(
        string unitId,
        ulong triggerTick,
        string? reasonDetail,
        out WatchAttentionEvent? evt)
    {
        evt = null;

        if (string.IsNullOrWhiteSpace(unitId))
        {
            return false;
        }

        if (!IsOwnSideUnit(unitId))
        {
            return false;
        }

        evt = new WatchAttentionEvent(
            LossEventIdPrefix + unitId,
            WatchAttentionKind.OwnSideLossOrDamage,
            WatchAttentionPriority.Critical,
            triggerTick,
            unitId,
            GroupingKey: null,
            ReasonDetail: reasonDetail);
        return true;
    }

    /// <summary>
    /// Own-side contact lifecycle transition to <see cref="ContactLifecycleState.Lost"/>.
    /// </summary>
    public static bool TryFromOwnSideLostTransition(
        in ContactTransition transition,
        out WatchAttentionEvent? evt)
    {
        evt = null;

        if (transition.NewState != ContactLifecycleState.Lost)
        {
            return false;
        }

        return TryFromOwnSideLoss(
            transition.TargetId,
            transition.SimTick,
            "lifecycle:Lost",
            out evt);
    }

    /// <summary>
    /// Own-side: catalog blue, or legacy primary blue id "u1".
    /// </summary>
    public static bool IsOwnSideUnit(string unitId)
    {
        if (string.IsNullOrWhiteSpace(unitId))
        {
            return false;
        }

        if (string.Equals(unitId, "u1", StringComparison.Ordinal))
        {
            return true;
        }

        return BalticV3SideRegistry.IsBlueForceUnit(unitId);
    }
}
