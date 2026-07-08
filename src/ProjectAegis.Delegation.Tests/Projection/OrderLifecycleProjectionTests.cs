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
}
