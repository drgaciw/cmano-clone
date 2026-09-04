using System;
using System.Collections.Generic;
using NUnit.Framework;
using ProjectAegis.Delegation.Controllers;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Orchestration;
using ProjectAegis.Delegation.Policy;
using ProjectAegis.Delegation.Roe;
using ProjectAegis.Delegation.Sim;
using ProjectAegis.Delegation.Targets;
using ProjectAegis.Delegation.Traits;
using ProjectAegis.Sim.Policy;

namespace ProjectAegis.Delegation.Tests.Orchestration;

/// <summary>
/// Interim safety gate HOL-04B (AEGIS-302 / DRG-232):
/// Proves that for SemiAutonomous controllers, lethal fire orders requested without
/// prior authorization or outside pre-cleared ROE parameters route to hold or pending review
/// rather than instantaneous uninhibited ExecuteNow.
/// Invariants:
/// - ZERO edits to DelegationBridge hotpath.
/// - Preserve clean null safety.
/// </summary>
[TestFixture]
public sealed class SemiAutonomousSafetyGateTests
{
    private sealed class CustomRoeFilter : IRoeFilter
    {
        private readonly RoeEvaluation _evaluation;

        public CustomRoeFilter(RoeEvaluation evaluation) => _evaluation = evaluation;

        public RoeEvaluation Evaluate(Order order) => _evaluation;
    }

    private sealed class SingleOrderPolicy : IPolicy
    {
        private readonly OrderKind _kind;

        public SingleOrderPolicy(OrderKind kind) => _kind = kind;

        public IReadOnlyList<ScoredIntent> GenerateCandidates(
            PerceivedState perceived,
            TraitVector traits) =>
            new[] { new ScoredIntent(_kind, 1.0, DefaultRiskClassifier.Classify(_kind)) };
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 1. Core Safety Gate HOL-04B Evaluations
    // ─────────────────────────────────────────────────────────────────────────

    [Test]
    public void SemiAutonomous_unauthorized_lethal_fire_order_routes_to_pending_review()
    {
        var gate = new AutonomyGate(new PassthroughRoeFilter());
        var engageOrder = new Order(new OrderId(1), new TargetId("u1"), 0, OrderKind.Engage, RiskLevel.High);

        var result = gate.Evaluate(AutonomyLevel.SemiAutonomous, engageOrder, playerApproved: false);

        Assert.That(result.ExecuteNow, Is.False, "Unauthorized lethal fire order must not execute immediately.");
        Assert.That(result.QueueForApproval, Is.True, "Unauthorized lethal fire order must route to hold / pending review.");
        Assert.That(result.Rejected, Is.False, "Order must not be rejected if ROE permits; it awaits review.");
    }

    [Test]
    public void SemiAutonomous_authorized_lethal_fire_order_executes_now()
    {
        var gate = new AutonomyGate(new PassthroughRoeFilter());
        var engageOrder = new Order(new OrderId(2), new TargetId("u1"), 0, OrderKind.Engage, RiskLevel.High);

        var result = gate.Evaluate(AutonomyLevel.SemiAutonomous, engageOrder, playerApproved: true);

        Assert.That(result.ExecuteNow, Is.True, "Prior-authorized lethal fire order within ROE must execute immediately.");
        Assert.That(result.QueueForApproval, Is.False);
        Assert.That(result.Rejected, Is.False);
    }

    [Test]
    public void SemiAutonomous_unauthorized_nonlethal_order_executes_now()
    {
        var gate = new AutonomyGate(new PassthroughRoeFilter());
        var moveOrder = new Order(new OrderId(3), new TargetId("u1"), 0, OrderKind.Move, RiskLevel.Low);

        var result = gate.Evaluate(AutonomyLevel.SemiAutonomous, moveOrder, playerApproved: false);

        Assert.That(result.ExecuteNow, Is.True, "Non-lethal maneuver orders must execute autonomously without holding.");
        Assert.That(result.QueueForApproval, Is.False);
        Assert.That(result.Rejected, Is.False);
    }

    [Test]
    public void SemiAutonomous_outside_precleared_roe_parameters_routes_to_pending_review()
    {
        // IRoeFilter returns RoeVerdict.Queue when parameters require operator review/confirmation
        var queueRoe = new CustomRoeFilter(new RoeEvaluation(RoeVerdict.Queue, FireAbortReason.None));
        var gate = new AutonomyGate(queueRoe);
        var engageOrder = new Order(new OrderId(4), new TargetId("u1"), 0, OrderKind.Engage, RiskLevel.High);

        var result = gate.Evaluate(AutonomyLevel.SemiAutonomous, engageOrder, playerApproved: true);

        Assert.That(result.ExecuteNow, Is.False, "Outside pre-cleared ROE parameters must not execute immediately.");
        Assert.That(result.QueueForApproval, Is.True, "Outside pre-cleared ROE parameters must route to hold / review.");
        Assert.That(result.Rejected, Is.False);
    }

    [Test]
    public void SemiAutonomous_player_approval_cannot_override_roe_reject()
    {
        var gate = new AutonomyGate(new RoePolicyAdapter(
            new PolicyEvaluator(_ => new EffectivePolicy(RoeLevel.HoldFire))));
        var engageOrder = new Order(new OrderId(5), new TargetId("u1"), 0, OrderKind.Engage, RiskLevel.High);

        var result = gate.Evaluate(AutonomyLevel.SemiAutonomous, engageOrder, playerApproved: true);

        Assert.That(result.Rejected, Is.True, "ROE reject is terminal and strictly supersedes player approval.");
        Assert.That(result.ExecuteNow, Is.False);
        Assert.That(result.QueueForApproval, Is.False, "ROE-rejected fire order must not enter pending review queue.");
        Assert.That(result.PolicyDenialReason, Is.EqualTo(FireAbortReason.RoeHoldFire));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 2. FullAutonomous & Comparative Autonomy Matrix Verification
    // ─────────────────────────────────────────────────────────────────────────

    [Test]
    public void FullAutonomous_continues_to_execute_lethal_orders_when_roe_permits()
    {
        var gate = new AutonomyGate(new PassthroughRoeFilter());
        var engageOrder = new Order(new OrderId(6), new TargetId("u1"), 0, OrderKind.Engage, RiskLevel.High);

        var result = gate.Evaluate(AutonomyLevel.FullAutonomous, engageOrder, playerApproved: false);

        Assert.That(result.ExecuteNow, Is.True, "FullAutonomous retains immediate execution per ADR-023.");
        Assert.That(result.QueueForApproval, Is.False);
        Assert.That(result.Rejected, Is.False);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 3. AgentController End-to-End Decision & Queue Routing
    // ─────────────────────────────────────────────────────────────────────────

    [Test]
    public void SemiAutonomous_agent_try_decide_enqueues_lethal_fire_order_into_pending_approval_queue()
    {
        var gate = new AutonomyGate(new PassthroughRoeFilter());
        var log = new DecisionLog();
        var pendingQueue = new PendingApprovalQueue();

        var agent = new AgentController(
            new AgentId("semi-1"),
            PersonalityCatalog.All[0].Traits,
            AutonomyLevel.SemiAutonomous,
            new SeededRng(42, agentSalt: 0),
            new SingleOrderPolicy(OrderKind.Engage),
            attentionBudget: 10.0);

        long seq = 1;
        var state = new ObservedState(10.0, ContactCount: 1, ActiveEngagementCount: 0, new Dictionary<TargetId, bool>());

        agent.TryDecide(
            new TargetId("t1"),
            state,
            memberCount: 1,
            ref seq,
            gate,
            log,
            pendingQueue);

        // Lethal order must NOT be issued immediately to the agent's drainable list
        var issued = agent.DrainIssuedOrders(10);
        Assert.That(issued, Is.Empty, "Unauthorized lethal fire order must not be in issued orders.");

        // Instead, order must be enqueued in PendingApprovalQueue
        Assert.That(pendingQueue.Count, Is.EqualTo(1));
        Assert.That(pendingQueue.Pending[0].Order.Kind, Is.EqualTo(OrderKind.Engage));
        Assert.That(pendingQueue.Pending[0].Order.Risk, Is.EqualTo(RiskLevel.High));

        // Approving the order promotes it to DrainApproved
        var orderId = pendingQueue.Pending[0].Order.Id;
        var approved = pendingQueue.TryApprove(orderId);
        Assert.That(approved, Is.True);
        Assert.That(pendingQueue.Count, Is.EqualTo(0));

        var drained = pendingQueue.DrainApproved();
        Assert.That(drained, Has.Count.EqualTo(1));
        Assert.That(drained[0].Id, Is.EqualTo(orderId));
    }

    [Test]
    public void SemiAutonomous_agent_try_decide_directly_issues_nonlethal_move_order()
    {
        var gate = new AutonomyGate(new PassthroughRoeFilter());
        var log = new DecisionLog();
        var pendingQueue = new PendingApprovalQueue();

        var agent = new AgentController(
            new AgentId("semi-2"),
            PersonalityCatalog.All[0].Traits,
            AutonomyLevel.SemiAutonomous,
            new SeededRng(42, agentSalt: 0),
            new SingleOrderPolicy(OrderKind.Move),
            attentionBudget: 10.0);

        long seq = 1;
        var state = new ObservedState(10.0, ContactCount: 1, ActiveEngagementCount: 0, new Dictionary<TargetId, bool>());

        agent.TryDecide(
            new TargetId("t1"),
            state,
            memberCount: 1,
            ref seq,
            gate,
            log,
            pendingQueue);

        // Non-lethal order must be issued directly without pending review
        var issued = agent.DrainIssuedOrders(10);
        Assert.That(issued, Has.Length.EqualTo(1));
        Assert.That(issued[0].Kind, Is.EqualTo(OrderKind.Move));
        Assert.That(pendingQueue.Count, Is.EqualTo(0));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 4. Null Safety Invariants
    // ─────────────────────────────────────────────────────────────────────────

    [Test]
    public void AutonomyGate_throws_ArgumentNullException_on_null_roe_or_order()
    {
        Assert.Throws<ArgumentNullException>((Action)(() => new AutonomyGate(null!)));

        var gate = new AutonomyGate(new PassthroughRoeFilter());
        Assert.Throws<ArgumentNullException>((Action)(() =>
            gate.Evaluate(AutonomyLevel.SemiAutonomous, null!, playerApproved: false)));
    }

    [Test]
    public void IsLethalFireOrder_safely_handles_null_order()
    {
        Assert.That(AutonomyGate.IsLethalFireOrder(null!), Is.False);
        Assert.That(AutonomyGate.IsLethalOrder(null!), Is.False);
    }
}
