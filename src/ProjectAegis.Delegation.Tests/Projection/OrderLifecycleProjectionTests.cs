using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Sim.Policy;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

public sealed class OrderLifecycleProjectionTests
{
    [Test]
    public void Player_order_with_no_comms_delay_is_Accepted()
    {
        var log = new DecisionLog();
        log.AppendPlayerOrder(new PlayerOrderRecord(
            0, 10.0, 10, new TargetId("u1"), OrderKind.Move, ExecuteSimTick: 10));

        var states = OrderLifecycleProjection.Project(log);

        var key = new OrderLifecycleProjection.OrderKey("u1", 1);
        Assert.That(states[key], Is.EqualTo(OrderLifecycleState.Accepted));
    }

    [Test]
    public void Player_order_with_resolved_execute_tick_in_the_future_is_Queued()
    {
        // req 20 AC-8: a comms-delayed order is already scheduled at logging time — the UI must
        // show it as Queued, not merely Accepted.
        var log = new DecisionLog();
        log.AppendPlayerOrder(new PlayerOrderRecord(
            0, 10.0, 10, new TargetId("u1"), OrderKind.Move, ExecuteSimTick: 25));

        var states = OrderLifecycleProjection.Project(log);

        var key = new OrderLifecycleProjection.OrderKey("u1", 1);
        Assert.That(states[key], Is.EqualTo(OrderLifecycleState.Queued));
    }

    [Test]
    public void Launched_engagement_moves_matching_order_to_Executing()
    {
        var log = new DecisionLog();
        log.AppendPlayerOrder(new PlayerOrderRecord(
            0, 10.0, 10, new TargetId("u1"), OrderKind.Engage, ExecuteSimTick: 10));
        log.AppendEngagement(new EngagementRecord(
            0, 11.0, 11, new TargetId("u1"), EngagementId: 501, Launched: true));

        var states = OrderLifecycleProjection.Project(log);

        var key = new OrderLifecycleProjection.OrderKey("u1", 1);
        Assert.That(states[key], Is.EqualTo(OrderLifecycleState.Executing));
    }

    [Test]
    public void Engagement_outcome_moves_matching_order_to_Completed()
    {
        var log = new DecisionLog();
        log.AppendPlayerOrder(new PlayerOrderRecord(
            0, 10.0, 10, new TargetId("u1"), OrderKind.Engage, ExecuteSimTick: 10));
        log.AppendEngagement(new EngagementRecord(
            0, 11.0, 11, new TargetId("u1"), EngagementId: 501, Launched: true));
        log.AppendEngagementOutcome(new EngagementOutcomeRecord(
            0, 12.0, 12, new TargetId("u1"), new TargetId("hostile-1"), EngagementId: 501,
            OutcomeCode: "KILL", PkDraw: 0.1));

        var states = OrderLifecycleProjection.Project(log);

        var key = new OrderLifecycleProjection.OrderKey("u1", 1);
        Assert.That(states[key], Is.EqualTo(OrderLifecycleState.Completed));
    }

    [Test]
    public void Policy_denial_moves_matching_order_to_Denied()
    {
        var log = new DecisionLog();
        log.AppendPlayerOrder(new PlayerOrderRecord(
            0, 10.0, 10, new TargetId("u1"), OrderKind.Engage, ExecuteSimTick: 10));
        log.AppendPolicyDenial(new PolicyDenialRecord(
            0, 11.0, 11, new AgentId("a1"), new TargetId("u1"), PolicySnapshotId: 1,
            Reason: FireAbortReason.RoeHoldFire, AttemptedKind: OrderKind.Engage));

        var states = OrderLifecycleProjection.Project(log);

        var key = new OrderLifecycleProjection.OrderKey("u1", 1);
        Assert.That(states[key], Is.EqualTo(OrderLifecycleState.Denied));
    }

    [Test]
    public void Preflight_aborted_engagement_moves_matching_order_to_Aborted()
    {
        var log = new DecisionLog();
        log.AppendPlayerOrder(new PlayerOrderRecord(
            0, 10.0, 10, new TargetId("u1"), OrderKind.Engage, ExecuteSimTick: 10));
        log.AppendEngagement(new EngagementRecord(
            0, 11.0, 11, new TargetId("u1"), EngagementId: 501, Launched: false,
            AbortReasonCode: "NO_TRACK"));

        var states = OrderLifecycleProjection.Project(log);

        var key = new OrderLifecycleProjection.OrderKey("u1", 1);
        Assert.That(states[key], Is.EqualTo(OrderLifecycleState.Aborted));
    }

    [Test]
    public void Evidence_matches_oldest_open_order_of_same_kind_for_the_unit_FIFO()
    {
        var log = new DecisionLog();
        log.AppendPlayerOrder(new PlayerOrderRecord(
            0, 10.0, 10, new TargetId("u1"), OrderKind.Engage, ExecuteSimTick: 10));
        log.AppendPlayerOrder(new PlayerOrderRecord(
            0, 11.0, 11, new TargetId("u1"), OrderKind.Engage, ExecuteSimTick: 11));
        log.AppendPolicyDenial(new PolicyDenialRecord(
            0, 12.0, 12, new AgentId("a1"), new TargetId("u1"), PolicySnapshotId: 1,
            Reason: FireAbortReason.RoeHoldFire, AttemptedKind: OrderKind.Engage));

        var states = OrderLifecycleProjection.Project(log);

        var firstKey = new OrderLifecycleProjection.OrderKey("u1", 1);
        var secondKey = new OrderLifecycleProjection.OrderKey("u1", 2);
        Assert.That(states[firstKey], Is.EqualTo(OrderLifecycleState.Denied));
        Assert.That(states[secondKey], Is.EqualTo(OrderLifecycleState.Accepted));
    }

    [Test]
    public void ProjectLatestForUnit_returns_the_most_recently_logged_order_state()
    {
        var log = new DecisionLog();
        log.AppendPlayerOrder(new PlayerOrderRecord(
            0, 10.0, 10, new TargetId("u1"), OrderKind.Move, ExecuteSimTick: 10));
        log.AppendPlayerOrder(new PlayerOrderRecord(
            0, 11.0, 11, new TargetId("u1"), OrderKind.Hold, ExecuteSimTick: 20));

        var latest = OrderLifecycleProjection.ProjectLatestForUnit(log, "u1");

        Assert.That(latest, Is.Not.Null);
        Assert.That(latest!.Value.Kind, Is.EqualTo(OrderKind.Hold));
        Assert.That(latest.Value.State, Is.EqualTo(OrderLifecycleState.Queued));
    }

    [Test]
    public void ProjectLatestForUnit_returns_null_when_unit_has_no_orders()
    {
        var log = new DecisionLog();

        var latest = OrderLifecycleProjection.ProjectLatestForUnit(log, "u1");

        Assert.That(latest, Is.Null);
    }

    [Test]
    public void Cancel_correlates_by_CancelledExecuteSimTick_not_FIFO_oldest()
    {
        // Codex P2 on PR #257: non-Engage kinds stay open after drain. A later Move is queued then
        // canceled — correlation must abort the canceled execute tick, not the older still-open Move.
        var log = new DecisionLog();
        log.AppendPlayerOrder(new PlayerOrderRecord(
            0, 10.0, 10, new TargetId("u1"), OrderKind.Move, ExecuteSimTick: 10));
        log.AppendPlayerOrder(new PlayerOrderRecord(
            0, 11.0, 11, new TargetId("u1"), OrderKind.Move, ExecuteSimTick: 25));
        log.AppendPlayerOrderCancelled(new PlayerOrderCancelledRecord(
            0, 12.0, 12, new TargetId("u1"), OrderKind.Move, CancelledExecuteSimTick: 25));

        var states = OrderLifecycleProjection.Project(log);

        var firstKey = new OrderLifecycleProjection.OrderKey("u1", 1);
        var secondKey = new OrderLifecycleProjection.OrderKey("u1", 2);
        Assert.That(states[firstKey], Is.EqualTo(OrderLifecycleState.Accepted),
            "already-drained Move must stay Accepted; cancel of a later Move must not FIFO-abort it");
        Assert.That(states[secondKey], Is.EqualTo(OrderLifecycleState.Aborted));
    }

    [Test]
    public void Cancel_matches_the_oldest_open_order_of_the_same_kind_FIFO_when_ticks_align()
    {
        // When CancelledExecuteSimTick matches the oldest open order, result is identical to FIFO.
        var log = new DecisionLog();
        log.AppendPlayerOrder(new PlayerOrderRecord(
            0, 10.0, 10, new TargetId("u1"), OrderKind.Move, ExecuteSimTick: 20));
        log.AppendPlayerOrder(new PlayerOrderRecord(
            0, 11.0, 11, new TargetId("u1"), OrderKind.Move, ExecuteSimTick: 21));
        log.AppendPlayerOrderCancelled(new PlayerOrderCancelledRecord(
            0, 12.0, 12, new TargetId("u1"), OrderKind.Move, CancelledExecuteSimTick: 20));

        var states = OrderLifecycleProjection.Project(log);

        var firstKey = new OrderLifecycleProjection.OrderKey("u1", 1);
        var secondKey = new OrderLifecycleProjection.OrderKey("u1", 2);
        Assert.That(states[firstKey], Is.EqualTo(OrderLifecycleState.Aborted));
        Assert.That(states[secondKey], Is.EqualTo(OrderLifecycleState.Queued));
    }

    [Test]
    public void Cancel_after_a_cancel_of_the_same_unit_and_kind_only_aborts_the_matching_execute_tick()
    {
        // Two queued Move orders, cancel twice by execute tick — each cancel aborts its match.
        var log = new DecisionLog();
        log.AppendPlayerOrder(new PlayerOrderRecord(
            0, 10.0, 10, new TargetId("u1"), OrderKind.Move, ExecuteSimTick: 20));
        log.AppendPlayerOrder(new PlayerOrderRecord(
            0, 11.0, 11, new TargetId("u1"), OrderKind.Move, ExecuteSimTick: 21));
        log.AppendPlayerOrderCancelled(new PlayerOrderCancelledRecord(
            0, 12.0, 12, new TargetId("u1"), OrderKind.Move, CancelledExecuteSimTick: 20));
        log.AppendPlayerOrderCancelled(new PlayerOrderCancelledRecord(
            0, 13.0, 13, new TargetId("u1"), OrderKind.Move, CancelledExecuteSimTick: 21));

        var states = OrderLifecycleProjection.Project(log);

        var firstKey = new OrderLifecycleProjection.OrderKey("u1", 1);
        var secondKey = new OrderLifecycleProjection.OrderKey("u1", 2);
        Assert.That(states[firstKey], Is.EqualTo(OrderLifecycleState.Aborted));
        Assert.That(states[secondKey], Is.EqualTo(OrderLifecycleState.Aborted));
    }

    [Test]
    public void Cancel_of_a_non_Engage_kind_Accepted_order_maps_to_Aborted()
    {
        // Known-MVP-gap kinds (Move, Hold, SetEwPosture, ReturnToBase) stay Accepted absent evidence —
        // a cancel of one of those (immediate, no comms delay) must still resolve to Aborted.
        var log = new DecisionLog();
        log.AppendPlayerOrder(new PlayerOrderRecord(
            0, 10.0, 10, new TargetId("u1"), OrderKind.Hold, ExecuteSimTick: 10));

        var beforeCancel = OrderLifecycleProjection.Project(log);
        var key = new OrderLifecycleProjection.OrderKey("u1", 1);
        Assert.That(beforeCancel[key], Is.EqualTo(OrderLifecycleState.Accepted));

        log.AppendPlayerOrderCancelled(new PlayerOrderCancelledRecord(
            0, 11.0, 11, new TargetId("u1"), OrderKind.Hold, CancelledExecuteSimTick: 10));

        var afterCancel = OrderLifecycleProjection.Project(log);
        Assert.That(afterCancel[key], Is.EqualTo(OrderLifecycleState.Aborted));
    }

    [Test]
    public void A_cancel_logged_after_the_order_has_already_launched_does_not_downgrade_Executing()
    {
        // PlayerOrderExecutionQueue.TryRemove only finds STILL-PENDING orders, so the real bridge can
        // never cancel a launched engage — but the pure projection must not downgrade Executing if a
        // stray cancel for the same unit+kind+execute tick appears after launch.
        var log = new DecisionLog();
        log.AppendPlayerOrder(new PlayerOrderRecord(
            0, 10.0, 10, new TargetId("u1"), OrderKind.Engage, ExecuteSimTick: 10));
        log.AppendEngagement(new EngagementRecord(
            0, 11.0, 11, new TargetId("u1"), EngagementId: 501, Launched: true));
        log.AppendPlayerOrderCancelled(new PlayerOrderCancelledRecord(
            0, 12.0, 12, new TargetId("u1"), OrderKind.Engage, CancelledExecuteSimTick: 10));

        var states = OrderLifecycleProjection.Project(log);

        var key = new OrderLifecycleProjection.OrderKey("u1", 1);
        Assert.That(states[key], Is.EqualTo(OrderLifecycleState.Executing),
            "an order that has already launched must stay Executing; a late/stray cancel record must not " +
            "retroactively downgrade it to Aborted");
    }

    [Test]
    public void A_cancel_logged_after_the_order_has_already_completed_is_a_noop()
    {
        // Once Completed, open-order tracking is cleared — a late cancel finds nothing and leaves Completed.
        var log = new DecisionLog();
        log.AppendPlayerOrder(new PlayerOrderRecord(
            0, 10.0, 10, new TargetId("u1"), OrderKind.Engage, ExecuteSimTick: 10));
        log.AppendEngagement(new EngagementRecord(
            0, 11.0, 11, new TargetId("u1"), EngagementId: 501, Launched: true));
        log.AppendEngagementOutcome(new EngagementOutcomeRecord(
            0, 12.0, 12, new TargetId("u1"), new TargetId("hostile-1"), EngagementId: 501,
            OutcomeCode: "KILL", PkDraw: 0.1));
        log.AppendPlayerOrderCancelled(new PlayerOrderCancelledRecord(
            0, 13.0, 13, new TargetId("u1"), OrderKind.Engage, CancelledExecuteSimTick: 10));

        var states = OrderLifecycleProjection.Project(log);

        var key = new OrderLifecycleProjection.OrderKey("u1", 1);
        Assert.That(states[key], Is.EqualTo(OrderLifecycleState.Completed));
    }
}
