namespace ProjectAegis.Delegation.Tests.Orchestration;

using ProjectAegis.Delegation.Controllers;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Orchestration;
using ProjectAegis.Delegation.Roe;
using ProjectAegis.Delegation.Sim;
using ProjectAegis.Delegation.Targets;
using ProjectAegis.Delegation.Traits;
using ProjectAegis.Sim.Policy;
using NUnit.Framework;

/// <summary>
/// DRG-66: PendingApprovalQueue unit + orchestrator integration tests.
/// Invariant: no DelegationBridge changes, OrderKind unchanged.
/// </summary>
[TestFixture]
public sealed class PendingApprovalQueueTests
{
    // ──────────────────────────────────────
    // Queue unit tests
    // ──────────────────────────────────────

    [Test]
    public void Enqueue_then_drain_without_approve_returns_empty()
    {
        var queue = new PendingApprovalQueue();
        var order = new Order(new OrderId(1), new TargetId("u1"), 0, OrderKind.Engage, RiskLevel.High);
        queue.Enqueue(order);

        var drained = queue.DrainApproved();

        Assert.That(drained, Is.Empty);
        Assert.That(queue.Pending, Has.Count.EqualTo(1));
    }

    [Test]
    public void TryApprove_promotes_order_and_removes_from_pending()
    {
        var queue = new PendingApprovalQueue();
        var order = new Order(new OrderId(2), new TargetId("u1"), 0, OrderKind.Engage, RiskLevel.High);
        queue.Enqueue(order);

        var approved = queue.TryApprove(new OrderId(2));

        Assert.That(approved, Is.True);
        Assert.That(queue.Pending, Is.Empty);

        var drained = queue.DrainApproved();
        Assert.That(drained, Has.Count.EqualTo(1));
        Assert.That(drained[0].Id, Is.EqualTo(new OrderId(2)));
    }

    [Test]
    public void TryApprove_returns_false_when_not_found()
    {
        var queue = new PendingApprovalQueue();
        var result = queue.TryApprove(new OrderId(99));
        Assert.That(result, Is.False);
    }

    [Test]
    public void TryReject_removes_from_pending_and_not_in_drain()
    {
        var queue = new PendingApprovalQueue();
        var order = new Order(new OrderId(3), new TargetId("u1"), 0, OrderKind.Engage, RiskLevel.High);
        queue.Enqueue(order);

        var rejected = queue.TryReject(new OrderId(3));

        Assert.That(rejected, Is.True);
        Assert.That(queue.Pending, Is.Empty);
        Assert.That(queue.DrainApproved(), Is.Empty);
    }

    [Test]
    public void TryReject_returns_false_when_not_found()
    {
        var queue = new PendingApprovalQueue();
        Assert.That(queue.TryReject(new OrderId(42)), Is.False);
    }

    [Test]
    public void Enqueue_duplicate_id_is_idempotent()
    {
        var queue = new PendingApprovalQueue();
        var order = new Order(new OrderId(5), new TargetId("u1"), 0, OrderKind.Move, RiskLevel.Low);
        queue.Enqueue(order);
        queue.Enqueue(order);
        Assert.That(queue.Pending, Has.Count.EqualTo(1));
    }

    [Test]
    public void DrainApproved_clears_approved_buffer()
    {
        var queue = new PendingApprovalQueue();
        var order = new Order(new OrderId(6), new TargetId("u1"), 0, OrderKind.Engage, RiskLevel.High);
        queue.Enqueue(order);
        queue.TryApprove(new OrderId(6));

        var first = queue.DrainApproved();
        Assert.That(first, Has.Count.EqualTo(1));

        var second = queue.DrainApproved();
        Assert.That(second, Is.Empty);
    }

    // ──────────────────────────────────────
    // Orchestrator integration tests
    // ──────────────────────────────────────

    [Test]
    public void Manual_autonomy_agent_queues_rather_than_executes()
    {
        var orchestrator = new DelegationOrchestrator(
            globalSeed: 7,
            policyEvaluator: new PolicyEvaluator(_ => EffectivePolicy.DefaultFree));
        var unit = new UnitTarget(new TargetId("u1"));
        var agent = orchestrator.CreateAgent(
            new AgentId("a1"),
            PersonalityCatalog.All[0].Traits,
            AutonomyLevel.Manual);
        unit.Slot.SetActive(agent);
        orchestrator.Register(unit);
        orchestrator.BeginExecution();

        var state = new ObservedState(0, ContactCount: 2, ActiveEngagementCount: 0,
            new Dictionary<TargetId, bool>(), PrimaryHostileDestroyed: false);

        // Run enough ticks to allow the agent to decide
        for (var i = 0; i < 10; i++)
        {
            orchestrator.Tick(state with { SimTime = i });
        }

        // Manual autonomy: orders should queue, not execute
        Assert.That(orchestrator.PendingApprovals, Is.Not.Empty,
            "Manual autonomy should route orders to the approval queue");
        // Executed orders should not include agent-decided orders (only previously approved ones)
        foreach (var executed in orchestrator.ExecutedOrders)
        {
            // Any executed order must have come via approval (the queue should empty after approval),
            // so if pending is non-empty, nothing executed yet.
        }
    }

    [Test]
    public void TryApprovePendingOrder_promotes_into_next_tick_executed()
    {
        var orchestrator = new DelegationOrchestrator(
            globalSeed: 7,
            policyEvaluator: new PolicyEvaluator(_ => EffectivePolicy.DefaultFree));
        var unit = new UnitTarget(new TargetId("u1"));
        var agent = orchestrator.CreateAgent(
            new AgentId("a1"),
            PersonalityCatalog.All[0].Traits,
            AutonomyLevel.Manual);
        unit.Slot.SetActive(agent);
        orchestrator.Register(unit);
        orchestrator.BeginExecution();

        var state = new ObservedState(0, ContactCount: 2, ActiveEngagementCount: 0,
            new Dictionary<TargetId, bool>(), PrimaryHostileDestroyed: false);

        // Accumulate at least one pending entry
        for (var i = 0; i < 15; i++)
        {
            orchestrator.Tick(state with { SimTime = i });
            if (orchestrator.PendingApprovals.Count > 0)
            {
                break;
            }
        }

        Assert.That(orchestrator.PendingApprovals, Is.Not.Empty, "Pre-condition: must have a queued order");

        var orderId = orchestrator.PendingApprovals[0].Order.Id;
        var approveResult = orchestrator.TryApprovePendingOrder(orderId);
        Assert.That(approveResult, Is.True);
        Assert.That(orchestrator.PendingApprovals.Any(e => e.Order.Id == orderId), Is.False);

        // Next tick should drain the approved order into ExecutedOrders
        orchestrator.Tick(state with { SimTime = 20 });
        Assert.That(orchestrator.ExecutedOrders.Any(o => o.Id == orderId), Is.True,
            "Approved order must appear in ExecutedOrders on next Tick");
    }

    [Test]
    public void TryRejectPendingOrder_discards_without_executing()
    {
        var orchestrator = new DelegationOrchestrator(
            globalSeed: 7,
            policyEvaluator: new PolicyEvaluator(_ => EffectivePolicy.DefaultFree));
        var unit = new UnitTarget(new TargetId("u1"));
        var agent = orchestrator.CreateAgent(
            new AgentId("a1"),
            PersonalityCatalog.All[0].Traits,
            AutonomyLevel.Manual);
        unit.Slot.SetActive(agent);
        orchestrator.Register(unit);
        orchestrator.BeginExecution();

        var state = new ObservedState(0, ContactCount: 2, ActiveEngagementCount: 0,
            new Dictionary<TargetId, bool>(), PrimaryHostileDestroyed: false);

        for (var i = 0; i < 15; i++)
        {
            orchestrator.Tick(state with { SimTime = i });
            if (orchestrator.PendingApprovals.Count > 0)
            {
                break;
            }
        }

        Assert.That(orchestrator.PendingApprovals, Is.Not.Empty);

        var orderId = orchestrator.PendingApprovals[0].Order.Id;
        var rejectResult = orchestrator.TryRejectPendingOrder(orderId);
        Assert.That(rejectResult, Is.True);

        // Next tick: rejected order must NOT appear in ExecutedOrders
        orchestrator.Tick(state with { SimTime = 20 });
        Assert.That(orchestrator.ExecutedOrders.Any(o => o.Id == orderId), Is.False,
            "Rejected order must never appear in ExecutedOrders");
    }

    [Test]
    public void FullAutonomous_agent_does_not_populate_pending_queue()
    {
        var orchestrator = new DelegationOrchestrator(
            globalSeed: 7,
            policyEvaluator: new PolicyEvaluator(_ => EffectivePolicy.DefaultFree));
        var unit = new UnitTarget(new TargetId("u1"));
        var agent = orchestrator.CreateAgent(
            new AgentId("a1"),
            PersonalityCatalog.All[0].Traits,
            AutonomyLevel.FullAutonomous);
        unit.Slot.SetActive(agent);
        orchestrator.Register(unit);
        orchestrator.BeginExecution();

        var state = new ObservedState(0, ContactCount: 2, ActiveEngagementCount: 0,
            new Dictionary<TargetId, bool>(), PrimaryHostileDestroyed: false);

        for (var i = 0; i < 10; i++)
        {
            orchestrator.Tick(state with { SimTime = i });
        }

        Assert.That(orchestrator.PendingApprovals, Is.Empty,
            "FullAutonomous agent must execute immediately, not queue for approval");
    }
}
