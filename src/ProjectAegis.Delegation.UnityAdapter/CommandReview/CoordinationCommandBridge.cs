namespace ProjectAegis.Delegation.UnityAdapter.CommandReview;

using Controllers;
using Core;
using MissionIntent;
using Targets;
using Bridge;

/// <summary>Closed set of deliberate human task-group decisions exposed by command review.</summary>
public enum CoordinationDecision
{
    Hold = 0,
    Withdraw = 1,
    Reattack = 2,
}

/// <summary>Readable command submission outcome and the exact affected unit scope.</summary>
public sealed record CoordinationDecisionResult(
    bool Accepted,
    string GroupId,
    CoordinationDecision Decision,
    IReadOnlyList<string> AffectedUnitIds,
    string? FailureReason,
    CoordinationApprovedScope? ApprovedScope);

/// <summary>Exact player-approved group scope used to authorize downstream unit expansion.</summary>
public sealed record CoordinationApprovedScope(
    string ScopeId,
    ulong PlayerOrderSequenceId,
    EntityKey GroupEntity,
    string GroupId,
    OrderKind Kind,
    double SimTime,
    ulong ExecuteSimTick,
    IReadOnlyList<string> CapturedMemberIds);

/// <summary>
/// Submits an explicit human task-group decision through the existing player command facade.
/// Recommendations never call this type and therefore cannot issue orders.
/// </summary>
public static class CoordinationCommandBridge
{
    public const string ReasonReplayAttached = "REPLAY_ATTACHED";
    public const string ReasonUnknownGroup = "UNKNOWN_GROUP";
    public const string ReasonEmptyGroup = "EMPTY_GROUP";
    public const string ReasonMissingMember = "MISSING_OR_LOST_MEMBER";
    public const string ReasonNotHumanControl = "NOT_HUMAN_CONTROL";
    public const string ReasonAuthorityWithheld = "AUTHORITY_WITHHELD";
    public const string ReasonIntentConstraint = "MISSION_INTENT_CONSTRAINT";
    public const string ReasonRetattackAdvisoryOnly = "REATTACK_ADVISORY_ONLY";
    public const string ReasonEnqueueFailed = "ENQUEUE_FAILED";
    public const string ReasonInvalidSimTime = "INVALID_SIM_TIME";
    public const string ReasonGroupDecisionPending = "GROUP_DECISION_PENDING";
    public const string ReasonIntentScopeMismatch = "MISSION_INTENT_SCOPE_MISMATCH";

    /// <summary>Validate the complete group scope, then atomically enqueue one group decision.</summary>
    public static CoordinationDecisionResult Submit(
        DelegationBridge? bridge,
        ISimWorldSnapshot? snapshot,
        string groupId,
        CoordinationDecision decision,
        MissionIntentSnapshot? reviewedIntent = null,
        IReadOnlyList<CoordinationApprovedScope>? pendingScopes = null)
    {
        if (bridge is null || snapshot is null)
        {
            return Reject(groupId, decision, ReasonUnknownGroup);
        }

        if (bridge.AttachReplayViewer)
        {
            return Reject(groupId, decision, ReasonReplayAttached);
        }

        if (!double.IsFinite(snapshot.SimTime) || snapshot.SimTime < 0)
        {
            return Reject(groupId, decision, ReasonInvalidSimTime);
        }
        if (pendingScopes?.Any(s => string.Equals(s.GroupId, groupId, StringComparison.Ordinal)) == true)
        {
            return Reject(groupId, decision, ReasonGroupDecisionPending);
        }

        var groupBinding = bridge.Registry.Bindings.FirstOrDefault(b =>
            b.Target is GroupTarget && string.Equals(b.TargetId.Value, groupId, StringComparison.Ordinal));
        if (groupBinding?.Target is not GroupTarget group)
        {
            return Reject(groupId, decision, ReasonUnknownGroup);
        }

        if (reviewedIntent is not null
            && !string.IsNullOrWhiteSpace(reviewedIntent.GroupId)
            && !string.Equals(reviewedIntent.GroupId, groupId, StringComparison.Ordinal))
        {
            return Reject(groupId, decision, ReasonIntentScopeMismatch);
        }

        if (group.Slot.Active is not HumanController)
        {
            return Reject(groupId, decision, ReasonNotHumanControl);
        }

        var memberIds = group.Members.Select(id => id.Value).OrderBy(id => id, StringComparer.Ordinal).ToArray();
        if (memberIds.Length == 0)
        {
            return Reject(groupId, decision, ReasonEmptyGroup);
        }

        for (var i = 0; i < group.Members.Count; i++)
        {
            var memberId = group.Members[i];
            if (!bridge.Registry.TryGetBinding(memberId, out var memberBinding)
                || memberBinding.Target is not UnitTarget unit
                || unit.IsDetachedFromGroup
                || !snapshot.IsMemberAlive(memberId))
            {
                return Reject(groupId, decision, ReasonMissingMember);
            }
        }

        var currentIntent = CoordinationBridge.Build(bridge, snapshot).Groups
            .FirstOrDefault(g => string.Equals(g.Coordination.GroupId, groupId, StringComparison.Ordinal))?.Intent;
        var effectiveIntent = currentIntent is not null
            && (!string.IsNullOrWhiteSpace(currentIntent.GroupId) || !string.IsNullOrWhiteSpace(currentIntent.UnitId))
                ? currentIntent
                : reviewedIntent;
        if (IsConstrained(decision, effectiveIntent))
        {
            return Reject(groupId, decision, ReasonIntentConstraint);
        }

        if (decision == CoordinationDecision.Reattack)
        {
            return Reject(groupId, decision, ReasonRetattackAdvisoryOnly);
        }

        var commandId = decision switch
        {
            CoordinationDecision.Hold => "hold",
            CoordinationDecision.Withdraw => "rtb",
            _ => string.Empty,
        };
        if (!C2PlayerCommandBridge.TryIssue(
                bridge,
                groupBinding.Entity,
                commandId,
                snapshot.SimTime,
                out var failureReason))
        {
            return Reject(groupId, decision, failureReason ?? ReasonEnqueueFailed);
        }

        var kind = decision == CoordinationDecision.Hold ? OrderKind.Hold : OrderKind.ReturnToBase;
        var playerOrderSequenceId = bridge.Orchestrator.DecisionLog.PlayerOrders[^1].SequenceId;
        var playerOrder = bridge.Orchestrator.DecisionLog.PlayerOrders[^1];
        var approvedScope = new CoordinationApprovedScope(
            $"coord-scope:{playerOrderSequenceId}",
            playerOrderSequenceId,
            groupBinding.Entity,
            groupId,
            kind,
            snapshot.SimTime,
            playerOrder.ResolvedExecuteSimTick,
            memberIds);
        return new CoordinationDecisionResult(true, groupId, decision, memberIds, FailureReason: null, approvedScope);
    }

    private static bool IsConstrained(CoordinationDecision decision, MissionIntentSnapshot? intent)
    {
        if (intent is null)
        {
            return false;
        }

        var constraints = intent.Constraints;
        if (decision == CoordinationDecision.Reattack)
        {
            return constraints.Contains(MissionIntentConstraintCode.NoStrike, StringComparer.Ordinal)
                || constraints.Contains(MissionIntentConstraintCode.RoeWithhold, StringComparer.Ordinal)
                || constraints.Contains(MissionIntentConstraintCode.Hold, StringComparer.Ordinal);
        }

        return decision == CoordinationDecision.Withdraw
            && constraints.Contains(MissionIntentConstraintCode.Hold, StringComparer.Ordinal);
    }

    private static CoordinationDecisionResult Reject(
        string groupId,
        CoordinationDecision decision,
        string reason) =>
        new(false, groupId ?? string.Empty, decision, Array.Empty<string>(), reason, ApprovedScope: null);
}
