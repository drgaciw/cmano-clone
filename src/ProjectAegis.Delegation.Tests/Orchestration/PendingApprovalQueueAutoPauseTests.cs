namespace ProjectAegis.Delegation.Tests.Orchestration;

using ProjectAegis.Delegation.Controllers;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Orchestration;
using ProjectAegis.Delegation.Roe;
using ProjectAegis.Delegation.Sim;
using ProjectAegis.Delegation.Targets;
using ProjectAegis.Delegation.Tests.Helpers;
using ProjectAegis.Delegation.Traits;
using ProjectAegis.Delegation.Watch;
using ProjectAegis.Sim.Engage;
using ProjectAegis.Sim.Policy;
using NUnit.Framework;

/// <summary>
/// AEGIS-301 (DRG-231): Verifies that enqueuing an order proposal into PendingApprovalQueue
/// triggers WatchAutoPauseGate to auto-pause the sim when auto-pause is enabled.
/// </summary>
[TestFixture]
public sealed class PendingApprovalQueueAutoPauseTests
{
    [Test]
    public void Enqueued_order_proposal_triggers_auto_pause_when_auto_pause_is_enabled()
    {
        var session = new SimulationSession(101, new StubEngagementResolver());
        Assert.That(session.IsSimPaused, Is.False, "Initial clock state should not be paused");
        Assert.That(session.WatchPauseGate.AutoPauseEnabled, Is.True, "Auto-pause should be enabled by default");

        var order = new Order(new OrderId(1), new TargetId("u1"), 0, OrderKind.Engage, RiskLevel.High);
        session.PendingApprovalQueue.Enqueue(order);

        Assert.That(session.IsSimPaused, Is.True, "Enqueuing an order proposal must trigger auto-pause");
        Assert.That(session.LastWatchPauseReason, Is.EqualTo(WatchPauseReason.OrderProposal));
        Assert.That(session.PendingApprovalQueue.HasPendingProposals, Is.True);
        Assert.That(session.PendingApprovalQueue.Count, Is.EqualTo(1));
    }

    [Test]
    public void Enqueued_order_proposal_does_not_trigger_auto_pause_when_auto_pause_is_disabled()
    {
        var session = new SimulationSession(102, new StubEngagementResolver());
        session.WatchPauseGate.AutoPauseEnabled = false;

        var order = new Order(new OrderId(2), new TargetId("u1"), 0, OrderKind.Engage, RiskLevel.High);
        session.PendingApprovalQueue.Enqueue(order);

        Assert.That(session.IsSimPaused, Is.False, "Auto-pause must not fire when disabled");
        Assert.That(session.PendingApprovalQueue.HasPendingProposals, Is.True);
        Assert.That(session.LastWatchPauseReason, Is.EqualTo(WatchPauseReason.None));
    }

    [Test]
    public void WatchAutoPauseGate_evaluates_PendingApprovalQueue_directly()
    {
        var gate = new WatchAutoPauseGate();
        var queue = new PendingApprovalQueue();

        Assert.That(gate.ShouldAutoPause(queue), Is.False, "Empty queue should not trigger auto-pause");
        Assert.That(queue.HasPendingProposals, Is.False);
        Assert.That(queue.Count, Is.EqualTo(0));

        var order = new Order(new OrderId(3), new TargetId("u1"), 0, OrderKind.Move, RiskLevel.Low);
        queue.Enqueue(order);

        Assert.That(queue.HasPendingProposals, Is.True);
        Assert.That(queue.Count, Is.EqualTo(1));
        Assert.That(gate.ShouldAutoPause(queue), Is.True);
        Assert.That(gate.LastPauseReason, Is.EqualTo(WatchPauseReason.OrderProposal));

        gate.AutoPauseEnabled = false;
        Assert.That(gate.ShouldAutoPause(queue), Is.False, "Disabled gate must return false even with pending proposals");
    }

    [Test]
    public void PendingApprovalQueue_connected_to_WatchAutoPauseGate_sets_reason_on_enqueue()
    {
        var gate = new WatchAutoPauseGate();
        var queue = new PendingApprovalQueue
        {
            AutoPauseGate = gate,
        };

        var order = new Order(new OrderId(4), new TargetId("u1"), 0, OrderKind.Engage, RiskLevel.High);
        queue.Enqueue(order);

        Assert.That(gate.LastPauseReason, Is.EqualTo(WatchPauseReason.OrderProposal));
        Assert.That(queue.HasPendingProposals, Is.True);

        // Duplicate enqueue should be idempotent
        queue.Enqueue(order);
        Assert.That(queue.Count, Is.EqualTo(1));
    }

    [Test]
    public void Agent_decision_in_manual_autonomy_triggers_session_auto_pause()
    {
        var session = new SimulationSession(
            globalSeed: 103,
            engagement: new StubEngagementResolver(),
            policyEvaluator: new PolicyEvaluator(_ => EffectivePolicy.DefaultFree));

        var unit = new UnitTarget(new TargetId("u1"));
        var agent = session.Orchestrator.CreateAgent(
            new AgentId("a1"),
            PersonalityCatalog.All[0].Traits,
            AutonomyLevel.Manual);
        unit.Slot.SetActive(agent);
        session.Orchestrator.Register(unit);
        session.BeginExecution();

        var state = new ObservedState(0, ContactCount: 2, ActiveEngagementCount: 0,
            new Dictionary<TargetId, bool>(), PrimaryHostileDestroyed: false);

        for (var i = 0; i < 15 && !session.IsSimPaused; i++)
        {
            session.Tick(state with { SimTime = i });
        }

        Assert.That(session.PendingApprovalQueue.HasPendingProposals, Is.True,
            "Manual agent decision should populate the pending approval queue");
        Assert.That(session.IsSimPaused, Is.True,
            "Session clock should be auto-paused after agent enqueues pending order");
        Assert.That(session.LastWatchPauseReason, Is.EqualTo(WatchPauseReason.OrderProposal));
    }

    [Test]
    public void TryResumeSim_is_gated_by_pending_order_proposals()
    {
        var session = new SimulationSession(104, new StubEngagementResolver());
        var order = new Order(new OrderId(5), new TargetId("u1"), 0, OrderKind.Engage, RiskLevel.High);
        session.PendingApprovalQueue.Enqueue(order);

        Assert.That(session.IsSimPaused, Is.True);

        // Gated resume fails while pending proposals remain
        Assert.That(session.TryResumeSim(explicitOverride: false), Is.False);
        Assert.That(session.IsSimPaused, Is.True);

        // Explicit override allows resume
        Assert.That(session.TryResumeSim(explicitOverride: true), Is.True);
        Assert.That(session.IsSimPaused, Is.False);
        Assert.That(session.LastWatchPauseReason, Is.EqualTo(WatchPauseReason.None));

        // Enqueue another order proposal
        var order2 = new Order(new OrderId(6), new TargetId("u1"), 0, OrderKind.Move, RiskLevel.Low);
        session.PendingApprovalQueue.Enqueue(order2);
        Assert.That(session.IsSimPaused, Is.True);

        // Approving all pending orders unblocks resume
        Assert.That(session.TryApprovePendingOrder(new OrderId(5)), Is.True);
        Assert.That(session.TryApprovePendingOrder(new OrderId(6)), Is.True);
        Assert.That(session.PendingApprovalQueue.HasPendingProposals, Is.False);

        Assert.That(session.TryResumeSim(explicitOverride: false), Is.True);
        Assert.That(session.IsSimPaused, Is.False);
    }

    [Test]
    public void Null_safety_is_preserved_across_gate_and_queue()
    {
        var gate = new WatchAutoPauseGate();
        Assert.That(gate.ShouldAutoPause((PendingApprovalQueue?)null), Is.False);
        Assert.That(gate.ShouldAutoPause((PendingApprovalEntry?)null), Is.False);
        Assert.That(gate.ShouldAutoPause((Order?)null), Is.False);
        Assert.That(gate.CanResume((PendingApprovalQueue?)null, explicitOverride: false), Is.True);
        Assert.That(gate.CanResume((WatchAttentionQueue?)null, (PendingApprovalQueue?)null, explicitOverride: false), Is.True);

        var queue = new PendingApprovalQueue();
        Assert.Throws<ArgumentNullException>((Action)(() => queue.Enqueue(null!)));
    }
}
