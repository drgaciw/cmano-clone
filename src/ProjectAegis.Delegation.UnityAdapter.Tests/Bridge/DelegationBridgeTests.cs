namespace ProjectAegis.Delegation.UnityAdapter.Tests.Bridge;

using ProjectAegis.Delegation.Controllers;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Orchestration;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using NUnit.Framework;

[TestFixture]
public sealed class DelegationBridgeTests
{
    [Test]
    public void Tick_is_no_op_while_planning()
    {
        var bridge = new DelegationBridge(42);
        var friendly = bridge.Registry.RegisterUnit(new EntityKey(1), "friendly-1");
        var opposing = bridge.Registry.RegisterUnit(new EntityKey(2), "opposing-1");

        bridge.ConfigureSimulationMode(
            new SimulationModeProfile(SimulationModeKind.Mixed, PlayerControlsFriendlySide: true),
            friendly: [friendly.Target],
            opposing: [opposing.Target],
            defaultTraits: ProjectAegis.Delegation.Traits.PersonalityCatalog.All[0].Traits);

        var sink = new RecordingSink();
        var result = bridge.Tick(
            new StubSnapshot(1, 2, 0, new Dictionary<TargetId, bool>()),
            sink);

        Assert.That(bridge.Phase, Is.EqualTo(SimulationPhase.Planning));
        Assert.That(result.ExecutedOrders, Is.Empty);
        Assert.That(sink.Applied, Is.Empty);
    }

    [Test]
    public void Tick_builds_observed_state_and_dispatches_orders_to_sink()
    {
        var bridge = new DelegationBridge(globalSeed: 42);
        var friendly = bridge.Registry.RegisterUnit(new EntityKey(1), "friendly-1");
        var opposing = bridge.Registry.RegisterUnit(new EntityKey(2), "opposing-1");

        bridge.ConfigureSimulationMode(
            new SimulationModeProfile(SimulationModeKind.Mixed, PlayerControlsFriendlySide: true),
            friendly: [friendly.Target],
            opposing: [opposing.Target],
            defaultTraits: ProjectAegis.Delegation.Traits.PersonalityCatalog.All[0].Traits);

        bridge.BeginExecution();

        var snapshot = new StubSnapshot(
            SimTime: 1,
            ContactCount: 2,
            ActiveEngagementCount: 0,
            Alive: new Dictionary<TargetId, bool>
            {
                [friendly.TargetId] = true,
                [opposing.TargetId] = true,
            });

        var sink = new RecordingSink();
        var result = bridge.Tick(snapshot, sink);

        Assert.That(result.ExecutedOrders, Is.Not.Empty);
        Assert.That(result.DispatchedToSim, Is.EqualTo(result.ExecutedOrders.Count));
        Assert.That(sink.Applied, Has.Count.EqualTo(result.DispatchedToSim));
        Assert.That(sink.Applied[0].Entity, Is.EqualTo(new EntityKey(2)));
    }

    [Test]
    public void TryEnqueueHumanOrder_only_when_human_controls_entity()
    {
        var bridge = new DelegationBridge(1);
        var unit = bridge.Registry.RegisterUnit(new EntityKey(10), "u10");
        unit.Target.Slot.SetActive(new HumanController());

        Assert.That(
            bridge.TryEnqueueHumanOrder(new EntityKey(10), OrderKind.Hold, simTime: 0),
            Is.True);

        var sink = new RecordingSink();
        bridge.BeginExecution();
        bridge.Tick(new StubSnapshot(0, 0, 0, new Dictionary<TargetId, bool>()), sink);

        Assert.That(sink.Applied, Has.Count.EqualTo(1));
        Assert.That(sink.Applied[0].Order.Kind, Is.EqualTo(OrderKind.Hold));
    }

    [Test]
    public void TryEnqueueHumanOrder_returns_false_when_AttachReplayViewer_enabled()
    {
        var bridge = new DelegationBridge(1);
        var unit = bridge.Registry.RegisterUnit(new EntityKey(10), "u10");
        unit.Target.Slot.SetActive(new HumanController());
        bridge.AttachReplayViewer = true;

        Assert.That(
            bridge.TryEnqueueHumanOrder(new EntityKey(10), OrderKind.Hold, simTime: 0),
            Is.False);
    }

    [Test]
    public void ObservedStateBuilder_includes_registered_member_alive_flags()
    {
        var bridge = new DelegationBridge(1);
        var group = bridge.Registry.RegisterGroup(new EntityKey(100), "g1");
        var member = bridge.Registry.RegisterUnit(new EntityKey(101), "u1");
        bridge.Registry.LinkGroupMember(group.TargetId, member.TargetId);

        var snapshot = new StubSnapshot(
            5,
            3,
            1,
            new Dictionary<TargetId, bool> { [member.TargetId] = false });

        var observed = ObservedStateBuilder.Build(snapshot, bridge.Registry.CollectMemberIds());

        Assert.That(observed.SimTime, Is.EqualTo(5));
        Assert.That(observed.MemberAlive[member.TargetId], Is.False);
    }

    private sealed class StubSnapshot(
        double SimTime,
        int ContactCount,
        int ActiveEngagementCount,
        IReadOnlyDictionary<TargetId, bool> Alive) : ISimWorldSnapshot
    {
        public double SimTime { get; } = SimTime;

        public int ContactCount { get; } = ContactCount;

        public int ActiveEngagementCount { get; } = ActiveEngagementCount;

        public bool IsMemberAlive(TargetId memberId) =>
            Alive.TryGetValue(memberId, out var alive) && alive;
    }

    private sealed class RecordingSink : IOrderSink
    {
        public List<(EntityKey Entity, Order Order)> Applied { get; } = new();

        public void ApplyOrder(EntityKey entity, in Order order) =>
            Applied.Add((entity, order));
    }
}
