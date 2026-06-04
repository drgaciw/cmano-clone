using ProjectAegis.Delegation.Controllers;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Targets;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Bridge;

[TestFixture]
public sealed class DelegationBridgeAttackOptionTests
{
    [Test]
    public void TryEnqueueAttackOption_appends_player_order_for_human_unit()
    {
        var bridge = new DelegationBridge(42, mvpEngagement: true, scenarioPolicyId: "baltic-patrol");
        var unit = bridge.Registry.RegisterUnit(new EntityKey(1), "u1");
        unit.Target.Slot.SetActive(new HumanController());
        bridge.BeginExecution();

        var snapshot = new StubSnapshot(
            SimTime: 1,
            ContactCount: 2,
            ActiveEngagementCount: 0,
            Alive: new Dictionary<TargetId, bool> { [unit.TargetId] = true },
            hasFireControlTrack: true);

        Assert.That(
            bridge.TryEnqueueAttackOption(new EntityKey(1), "hold-fire", snapshot, out _),
            Is.True);
        Assert.That(bridge.Orchestrator.DecisionLog.PlayerOrders, Has.Count.EqualTo(1));
        Assert.That(bridge.Orchestrator.DecisionLog.PlayerOrders[0].Kind, Is.EqualTo(OrderKind.Hold));
    }

    private sealed class StubSnapshot(
        double SimTime,
        int ContactCount,
        int ActiveEngagementCount,
        IReadOnlyDictionary<TargetId, bool> Alive,
        bool hasFireControlTrack = true) : ISimWorldSnapshot
    {
        public double SimTime { get; } = SimTime;

        public int ContactCount { get; } = ContactCount;

        public int ActiveEngagementCount { get; } = ActiveEngagementCount;

        public TargetId? PrimaryHostileContactId => ContactCount > 0 ? new TargetId("hostile-1") : null;

        public bool HasFireControlTrackOnPrimaryContact => hasFireControlTrack;

        public bool ObserverRadarEmconActive => true;

        public bool IsMemberAlive(TargetId memberId) =>
            Alive.TryGetValue(memberId, out var alive) && alive;
    }
}