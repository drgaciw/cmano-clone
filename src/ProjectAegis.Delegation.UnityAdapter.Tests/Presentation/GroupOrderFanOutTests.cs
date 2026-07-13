using ProjectAegis.Delegation.Controllers;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Targets;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using ProjectAegis.Delegation.UnityAdapter.Presentation;
using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

/// <summary>
/// req 20 §Selection, TR-c2-005 (AC-7): group-order fan-out issues exactly one existing-bridge call
/// per eligible unit — ZERO diff to DelegationBridge.cs (fan-out is a thin loop over
/// <see cref="DelegationBridge.TryEnqueueAttackOption"/> / <see cref="DelegationBridge.TryEnqueueHumanOrder"/>,
/// both pre-existing bridge methods). Follows the DelegationBridgeAttackOptionTests fixture pattern.
/// </summary>
[TestFixture]
public sealed class GroupOrderFanOutTests
{
    [Test]
    public void ExecuteAttackOption_dispatches_one_call_per_eligible_unit()
    {
        var bridge = new DelegationBridge(42, mvpEngagement: true, scenarioPolicyId: "baltic-patrol");
        var u1 = bridge.Registry.RegisterUnit(new EntityKey(1), "u1");
        u1.Target.Slot.SetActive(new HumanController());
        var u2 = bridge.Registry.RegisterUnit(new EntityKey(2), "u2");
        u2.Target.Slot.SetActive(new HumanController());
        bridge.BeginExecution();

        var snapshot = new StubSnapshot(
            SimTime: 1,
            ContactCount: 2,
            Alive: new Dictionary<TargetId, bool> { [u1.TargetId] = true, [u2.TargetId] = true },
            hasFireControlTrack: true);

        var plan = GroupOrderPlan.Build(
            new[] { "u1", "u2" },
            id => GroupOrderUnitVerdict.Eligible(id));

        var result = GroupOrderFanOut.ExecuteAttackOption(plan, bridge, snapshot, "hold-fire");

        Assert.That(result.Dispatched, Is.EquivalentTo(new[] { "u1", "u2" }));
        Assert.That(result.Failed, Is.Empty);
        Assert.That(bridge.Orchestrator.DecisionLog.PlayerOrders, Has.Count.EqualTo(2));
        Assert.That(bridge.Orchestrator.DecisionLog.PlayerOrders, Has.All.Matches<PlayerOrderRecord>(
            r => r.Kind == OrderKind.Hold));
    }

    [Test]
    public void ExecuteAttackOption_only_fans_out_to_the_plans_eligible_survivors()
    {
        var bridge = new DelegationBridge(42, mvpEngagement: true, scenarioPolicyId: "baltic-patrol");
        var u1 = bridge.Registry.RegisterUnit(new EntityKey(1), "u1");
        u1.Target.Slot.SetActive(new HumanController());
        var u2 = bridge.Registry.RegisterUnit(new EntityKey(2), "u2");
        u2.Target.Slot.SetActive(new HumanController());
        bridge.BeginExecution();

        var snapshot = new StubSnapshot(
            SimTime: 1,
            ContactCount: 2,
            Alive: new Dictionary<TargetId, bool> { [u1.TargetId] = true, [u2.TargetId] = true },
            hasFireControlTrack: true);

        // Selection includes a destroyed unit ("u2") — GroupOrderPlan drops it before fan-out.
        var plan = GroupOrderPlan.Build(
            new[] { "u1", "u2" },
            id => id == "u2"
                ? GroupOrderUnitVerdict.Ineligible(id, GroupOrderIneligibleReason.Destroyed)
                : GroupOrderUnitVerdict.Eligible(id));

        var result = GroupOrderFanOut.ExecuteAttackOption(plan, bridge, snapshot, "hold-fire");

        Assert.That(result.Dispatched, Is.EqualTo(new[] { "u1" }));
        Assert.That(bridge.Orchestrator.DecisionLog.PlayerOrders, Has.Count.EqualTo(1),
            "destroyed unit dropped from the plan — never reaches the bridge at all");
    }

    [Test]
    public void ExecuteAttackOption_reports_failure_for_a_unit_id_that_no_longer_resolves_to_an_entity()
    {
        var bridge = new DelegationBridge(42, mvpEngagement: true, scenarioPolicyId: "baltic-patrol");
        var u1 = bridge.Registry.RegisterUnit(new EntityKey(1), "u1");
        u1.Target.Slot.SetActive(new HumanController());
        bridge.BeginExecution();

        var snapshot = new StubSnapshot(
            SimTime: 1,
            ContactCount: 2,
            Alive: new Dictionary<TargetId, bool> { [u1.TargetId] = true },
            hasFireControlTrack: true);

        // "ghost" was eligible per the plan's evaluator but was never registered on the bridge —
        // fan-out must not throw; it reports the id as failed and continues.
        var plan = GroupOrderPlan.Build(
            new[] { "u1", "ghost" },
            id => GroupOrderUnitVerdict.Eligible(id));

        var result = GroupOrderFanOut.ExecuteAttackOption(plan, bridge, snapshot, "hold-fire");

        Assert.That(result.Dispatched, Is.EqualTo(new[] { "u1" }));
        Assert.That(result.Failed, Is.EqualTo(new[] { "ghost" }));
    }

    [Test]
    public void ExecuteHumanOrder_dispatches_one_call_per_eligible_unit()
    {
        var bridge = new DelegationBridge(42, mvpEngagement: false, scenarioPolicyId: "baltic-patrol");
        var u1 = bridge.Registry.RegisterUnit(new EntityKey(1), "u1");
        u1.Target.Slot.SetActive(new HumanController());
        var u2 = bridge.Registry.RegisterUnit(new EntityKey(2), "u2");
        u2.Target.Slot.SetActive(new HumanController());
        bridge.BeginExecution();

        var plan = GroupOrderPlan.Build(
            new[] { "u1", "u2" },
            id => GroupOrderUnitVerdict.Eligible(id));

        var result = GroupOrderFanOut.ExecuteHumanOrder(plan, bridge, OrderKind.ReturnToBase, simTime: 5);

        Assert.That(result.Dispatched, Is.EquivalentTo(new[] { "u1", "u2" }));
        Assert.That(bridge.Orchestrator.DecisionLog.PlayerOrders, Has.Count.EqualTo(2));
        Assert.That(bridge.Orchestrator.DecisionLog.PlayerOrders, Has.All.Matches<PlayerOrderRecord>(
            r => r.Kind == OrderKind.ReturnToBase));
    }

    [Test]
    public void ExecuteHumanOrder_with_an_empty_plan_dispatches_nothing()
    {
        var bridge = new DelegationBridge(42, mvpEngagement: false, scenarioPolicyId: "baltic-patrol");
        bridge.BeginExecution();

        var plan = GroupOrderPlan.Build(Array.Empty<string>(), GroupOrderUnitVerdict.Eligible);

        var result = GroupOrderFanOut.ExecuteHumanOrder(plan, bridge, OrderKind.Hold, simTime: 0);

        Assert.That(result.Dispatched, Is.Empty);
        Assert.That(result.Failed, Is.Empty);
        Assert.That(bridge.Orchestrator.DecisionLog.PlayerOrders, Is.Empty);
    }

    private sealed class StubSnapshot(
        double SimTime,
        int ContactCount,
        IReadOnlyDictionary<TargetId, bool> Alive,
        bool hasFireControlTrack = true) : ISimWorldSnapshot
    {
        public double SimTime { get; } = SimTime;

        public int ContactCount { get; } = ContactCount;

        public int ActiveEngagementCount => 0;

        public TargetId? PrimaryHostileContactId => ContactCount > 0 ? new TargetId("hostile-1") : null;

        public bool HasFireControlTrackOnPrimaryContact => hasFireControlTrack;

        public bool ObserverRadarEmconActive => true;

        public bool IsMemberAlive(TargetId memberId) =>
            Alive.TryGetValue(memberId, out var alive) && alive;
    }
}
