namespace ProjectAegis.Delegation.UnityAdapter.Tests.CommandReview;

using Controllers;
using Core;
using C2Nodes;
using MissionIntent;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using ProjectAegis.Delegation.UnityAdapter.CommandReview;
using NUnit.Framework;

[TestFixture]
public sealed class CoordinationCommandBridgeTests
{
    [Test]
    public void Submit_hold_on_human_group_applies_to_each_member_after_tick()
    {
        var (bridge, snapshot) = CreateHumanGroup();
        var downstream = new RecordingSink();
        var result = CoordinationCommandBridge.Submit(
            bridge, snapshot, "g1", CoordinationDecision.Hold);
        var sink = new CoordinationOrderSink(bridge.Registry, snapshot, downstream, [result.ApprovedScope!]);
        bridge.BeginExecution();
        bridge.Tick(snapshot, sink);

        Assert.That(result.Accepted, Is.True);
        Assert.That(result.AffectedUnitIds, Is.EqualTo(new[] { "u1", "u2" }));
        Assert.That(downstream.Applied.Select(x => x.Entity), Is.EqualTo(new[] { new EntityKey(1), new EntityKey(2) }));
        Assert.That(downstream.Applied.All(x => x.Order.Kind == OrderKind.Hold), Is.True);
    }

    [Test]
    public void Submit_fails_closed_for_replay_missing_member_and_withheld_authority()
    {
        var (bridge, snapshot) = CreateHumanGroup();
        bridge.AttachReplayViewer = true;
        var replay = CoordinationCommandBridge.Submit(bridge, snapshot, "g1", CoordinationDecision.Hold);
        bridge.AttachReplayViewer = false;

        var missingSnapshot = new Snapshot(new Dictionary<string, bool> { ["u1"] = true, ["u2"] = false });
        var missing = CoordinationCommandBridge.Submit(bridge, missingSnapshot, "g1", CoordinationDecision.Hold);
        var withheld = CoordinationCommandBridge.Submit(bridge, snapshot, "g1", CoordinationDecision.Reattack);

        Assert.That(replay.FailureReason, Is.EqualTo(CoordinationCommandBridge.ReasonReplayAttached));
        Assert.That(missing.FailureReason, Is.EqualTo(CoordinationCommandBridge.ReasonMissingMember));
        Assert.That(withheld.FailureReason, Is.EqualTo(CoordinationCommandBridge.ReasonRetattackAdvisoryOnly));
        Assert.That(bridge.Orchestrator.DecisionLog.PlayerOrders, Is.Empty);
    }

    [Test]
    public void Submit_reattack_stays_advisory_without_actor_target_and_time_bound_authority()
    {
        var (bridge, snapshot) = CreateHumanGroup();
        var result = CoordinationCommandBridge.Submit(bridge, snapshot, "g1", CoordinationDecision.Reattack);

        Assert.That(result.Accepted, Is.False);
        Assert.That(result.FailureReason, Is.EqualTo(CoordinationCommandBridge.ReasonRetattackAdvisoryOnly));
        Assert.That(bridge.Orchestrator.DecisionLog.PlayerOrders, Is.Empty);
    }

    [Test]
    public void Submit_withdraw_respects_hold_mission_constraint()
    {
        var (bridge, snapshot) = CreateHumanGroup();
        var intent = MissionIntentProjection.Project(new MissionIntentInput(
            "g1", "", MissionIntentCode.Hold, [MissionIntentConstraintCode.Hold]));

        var result = CoordinationCommandBridge.Submit(
            bridge, snapshot, "g1", CoordinationDecision.Withdraw, intent);

        Assert.That(result.Accepted, Is.False);
        Assert.That(result.FailureReason, Is.EqualTo(CoordinationCommandBridge.ReasonIntentConstraint));
        Assert.That(bridge.Orchestrator.DecisionLog.PlayerOrders, Is.Empty);
    }

    [Test]
    public void Submit_rejects_supplied_intent_for_another_group()
    {
        var (bridge, snapshot) = CreateHumanGroup();
        var unrelated = MissionIntentProjection.Project(new MissionIntentInput(
            "other", "", MissionIntentCode.Hold, []));

        var result = CoordinationCommandBridge.Submit(
            bridge, snapshot, "g1", CoordinationDecision.Hold, unrelated);

        Assert.That(result.FailureReason, Is.EqualTo(CoordinationCommandBridge.ReasonIntentScopeMismatch));
        Assert.That(bridge.Orchestrator.DecisionLog.PlayerOrders, Is.Empty);
    }

    [Test]
    public void Submit_uses_current_snapshot_intent_facts_instead_of_weaker_caller_review()
    {
        var (bridge, _) = CreateHumanGroup();
        var current = new FactsSnapshot(
            AliveMembers(),
            3,
            [new MissionIntentInput("g1", "", MissionIntentCode.Hold, [MissionIntentConstraintCode.Hold])]);
        var weaker = MissionIntentProjection.Project(new MissionIntentInput(
            "g1", "", MissionIntentCode.Hold, []));

        var result = CoordinationCommandBridge.Submit(
            bridge, current, "g1", CoordinationDecision.Withdraw, weaker);

        Assert.That(result.FailureReason, Is.EqualTo(CoordinationCommandBridge.ReasonIntentConstraint));
        Assert.That(bridge.Orchestrator.DecisionLog.PlayerOrders, Is.Empty);
    }

    [Test]
    public void Submit_rejects_detached_member_before_enqueue()
    {
        var (bridge, snapshot) = CreateHumanGroup();
        Assert.That(bridge.Registry.TryGetBinding(new EntityKey(2), out var member), Is.True);
        ((Targets.UnitTarget)member.Target).SetDetached(true, new TargetId("g1"));

        var result = CoordinationCommandBridge.Submit(bridge, snapshot, "g1", CoordinationDecision.Hold);

        Assert.That(result.Accepted, Is.False);
        Assert.That(result.FailureReason, Is.EqualTo(CoordinationCommandBridge.ReasonMissingMember));
        Assert.That(bridge.Orchestrator.DecisionLog.PlayerOrders, Is.Empty);
    }

    [TestCase(double.NaN)]
    [TestCase(double.PositiveInfinity)]
    [TestCase(-1d)]
    public void Submit_rejects_invalid_sim_time(double simTime)
    {
        var (bridge, _) = CreateHumanGroup();

        var result = CoordinationCommandBridge.Submit(
            bridge, new Snapshot(AliveMembers(), simTime), "g1", CoordinationDecision.Hold);

        Assert.That(result.FailureReason, Is.EqualTo(CoordinationCommandBridge.ReasonInvalidSimTime));
        Assert.That(bridge.Orchestrator.DecisionLog.PlayerOrders, Is.Empty);
    }

    [Test]
    public void Delayed_order_uses_fresh_tick_sinks_and_scope_remains_pending_until_consumed()
    {
        var (bridge, _) = CreateHumanGroup("baltic-patrol-comms");
        bridge.BeginExecution();
        var downstream = new RecordingSink();
        bridge.Tick(new Snapshot(AliveMembers(), 2), downstream);
        var submitSnapshot = new Snapshot(AliveMembers(), 3);
        var result = CoordinationCommandBridge.Submit(bridge, submitSnapshot, "g1", CoordinationDecision.Hold);
        var pending = new List<CoordinationApprovedScope> { result.ApprovedScope! };

        foreach (var tick in new[] { 3d, 4d, 5d })
        {
            var current = new Snapshot(AliveMembers(), tick);
            var sink = new CoordinationOrderSink(bridge.Registry, current, downstream, pending);
            bridge.Tick(current, sink);
            pending.RemoveAll(s => sink.ConsumedScopeIds.Contains(s.ScopeId));
            if (tick < 5)
            {
                Assert.That(downstream.Applied, Is.Empty);
                Assert.That(pending, Has.Count.EqualTo(1));
            }
        }

        Assert.That(downstream.Applied.Select(x => x.Entity), Is.EqualTo(new[] { new EntityKey(1), new EntityKey(2) }));
        Assert.That(pending, Is.Empty);
    }

    [Test]
    public void Duplicate_same_tick_decisions_receive_unique_log_bound_scope_ids()
    {
        var (bridge, snapshot) = CreateHumanGroup();

        var first = CoordinationCommandBridge.Submit(bridge, snapshot, "g1", CoordinationDecision.Hold);
        var second = CoordinationCommandBridge.Submit(bridge, snapshot, "g1", CoordinationDecision.Hold);

        Assert.That(first.ApprovedScope!.ScopeId, Is.Not.EqualTo(second.ApprovedScope!.ScopeId));
        Assert.That(first.ApprovedScope.PlayerOrderSequenceId, Is.Not.EqualTo(second.ApprovedScope.PlayerOrderSequenceId));
    }

    [Test]
    public void Pending_group_scope_blocks_duplicate_and_reconciles_lost_authority()
    {
        var (bridge, snapshot) = CreateHumanGroup();
        var first = CoordinationCommandBridge.Submit(bridge, snapshot, "g1", CoordinationDecision.Hold);
        var pending = new[] { first.ApprovedScope! };
        var duplicate = CoordinationCommandBridge.Submit(
            bridge, snapshot, "g1", CoordinationDecision.Hold, pendingScopes: pending);
        Assert.That(bridge.Registry.TryGetBinding(new EntityKey(100), out var group), Is.True);
        group.Target.Slot.SetActive(null);

        var stale = CoordinationScopeReconciler.FindInvalidOrExpired(bridge, pending, 3);

        Assert.That(duplicate.FailureReason, Is.EqualTo(CoordinationCommandBridge.ReasonGroupDecisionPending));
        Assert.That(stale, Is.EqualTo(new[] { first.ApprovedScope!.ScopeId }));
    }

    [Test]
    public void Order_sink_passes_unapproved_group_order_and_consumes_only_exact_approved_scope()
    {
        var (bridge, snapshot) = CreateHumanGroup();
        var result = CoordinationCommandBridge.Submit(bridge, snapshot, "g1", CoordinationDecision.Hold);
        var downstream = new RecordingSink();
        var sink = new CoordinationOrderSink(bridge.Registry, snapshot, downstream, [result.ApprovedScope!]);
        var unrelated = new Order(new OrderId(8), new TargetId("g1"), 99, OrderKind.Move, RiskLevel.Low);
        sink.ApplyOrder(new EntityKey(100), unrelated);
        var approved = new Order(new OrderId(9), new TargetId("g1"), 3, OrderKind.Hold, RiskLevel.Low);
        sink.ApplyOrder(new EntityKey(100), approved);
        sink.ApplyOrder(new EntityKey(100), approved);

        Assert.That(downstream.Applied[0].Entity, Is.EqualTo(new EntityKey(100)));
        Assert.That(downstream.Applied.Skip(1).Select(x => x.Entity), Is.EqualTo(new[] { new EntityKey(1), new EntityKey(2), new EntityKey(100) }));
        Assert.That(sink.ConsumedScopeIds, Does.Contain(result.ApprovedScope!.ScopeId));
    }

    private static (DelegationBridge Bridge, Snapshot Snapshot) CreateHumanGroup(string? scenarioPolicyId = null)
    {
        var bridge = new DelegationBridge(42, mvpEngagement: false, scenarioPolicyId: scenarioPolicyId);
        var group = bridge.Registry.RegisterGroup(new EntityKey(100), "g1");
        group.Target.Slot.SetActive(new HumanController());
        var u1 = bridge.Registry.RegisterUnit(new EntityKey(1), "u1");
        var u2 = bridge.Registry.RegisterUnit(new EntityKey(2), "u2");
        bridge.Registry.LinkGroupMember(group.TargetId, u1.TargetId);
        bridge.Registry.LinkGroupMember(group.TargetId, u2.TargetId);
        return (bridge, new Snapshot(AliveMembers(), 3));
    }

    private static IReadOnlyDictionary<string, bool> AliveMembers() =>
        new Dictionary<string, bool> { ["u1"] = true, ["u2"] = true };

    private sealed class Snapshot(IReadOnlyDictionary<string, bool> alive, double simTime = 3) : ISimWorldSnapshot
    {
        public double SimTime { get; } = simTime;
        public int ContactCount => 0;
        public int ActiveEngagementCount => 0;
        public TargetId? PrimaryHostileContactId => null;
        public bool HasFireControlTrackOnPrimaryContact => false;
        public bool ObserverRadarEmconActive => true;
        public bool IsMemberAlive(TargetId memberId) => alive.TryGetValue(memberId.Value, out var value) && value;
    }

    private sealed class FactsSnapshot(
        IReadOnlyDictionary<string, bool> alive,
        double simTime,
        IReadOnlyList<MissionIntentInput> intents) : ISimWorldSnapshot, ICoordinationFacts
    {
        public double SimTime { get; } = simTime;
        public int ContactCount => 0;
        public int ActiveEngagementCount => 0;
        public TargetId? PrimaryHostileContactId => null;
        public bool HasFireControlTrackOnPrimaryContact => false;
        public bool ObserverRadarEmconActive => true;
        public bool IsMemberAlive(TargetId memberId) => alive.TryGetValue(memberId.Value, out var value) && value;
        public IReadOnlyList<PackageDefinition> Packages => [];
        public IReadOnlyList<CoverageFact> Coverage => [];
        public IReadOnlyList<MissionIntentInput> Intents { get; } = intents;
    }

    private sealed class RecordingSink : IOrderSink
    {
        public List<(EntityKey Entity, Order Order)> Applied { get; } = [];
        public void ApplyOrder(EntityKey entity, in Order order) => Applied.Add((entity, order));
    }
}
