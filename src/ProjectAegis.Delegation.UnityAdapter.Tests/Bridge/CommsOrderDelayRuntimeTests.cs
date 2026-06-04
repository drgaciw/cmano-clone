using ProjectAegis.Delegation.Comms;
using ProjectAegis.Delegation.Controllers;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Sim;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Bridge;

public sealed class CommsOrderDelayRuntimeTests
{
    [Test]
    public void TryEnqueueHumanOrder_defers_dispatch_until_execute_tick_under_degraded_comms()
    {
        var bridge = new DelegationBridge(42, mvpEngagement: false, scenarioPolicyId: "baltic-patrol-comms");
        var unit = bridge.Registry.RegisterUnit(new EntityKey(1), "u1");
        unit.Target.Slot.SetActive(new HumanController());
        bridge.BeginExecution();
        var sink = new RecordingSink();

        bridge.Tick(new StubSnapshot(2, 0, 0, new Dictionary<TargetId, bool>()), sink);
        Assert.That(bridge.CurrentCommsState, Is.EqualTo(CommsState.Degraded));

        Assert.That(
            bridge.TryEnqueueHumanOrder(new EntityKey(1), OrderKind.Hold, simTime: 3),
            Is.True);

        bridge.Tick(new StubSnapshot(3, 0, 0, new Dictionary<TargetId, bool>()), sink);
        Assert.That(sink.Applied, Is.Empty, "delay=2 at tick 3 -> execute tick 5");

        bridge.Tick(new StubSnapshot(4, 0, 0, new Dictionary<TargetId, bool>()), sink);
        Assert.That(sink.Applied, Is.Empty);

        bridge.Tick(new StubSnapshot(5, 0, 0, new Dictionary<TargetId, bool>()), sink);
        Assert.That(sink.Applied, Has.Count.EqualTo(1));
        Assert.That(sink.Applied[0].Order.Kind, Is.EqualTo(OrderKind.Hold));
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

        public TargetId? PrimaryHostileContactId => null;

        public bool HasFireControlTrackOnPrimaryContact => false;

        public bool ObserverRadarEmconActive => true;

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