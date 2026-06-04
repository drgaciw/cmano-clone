namespace ProjectAegis.Delegation.UnityAdapter.Tests.Bridge;

using ProjectAegis.Delegation.UnityAdapter.Baltic;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using NUnit.Framework;

[TestFixture]
public sealed class UnitDetailBridgeTests
{
    [Test]
    public void Baltic_run_exposes_operational_u1_detail()
    {
        var result = BalticReplayHarness.Run(42, "baltic-patrol", ticks: 4);
        _ = result;
        var bridge = new DelegationBridge(42, mvpEngagement: true, scenarioPolicyId: "baltic-patrol");
        bridge.Registry.RegisterUnit(new EntityKey(1), "u1");
        bridge.BeginExecution();
        var snapshot = new SimWorldSnapshotStub(contactCount: 1, hasFireControlTrackOnPrimaryContact: true);
        var detail = UnitDetailBridge.BuildPrimary(
            snapshot,
            bridge.Registry,
            bridge.Orchestrator.DecisionLog,
            bridge.Orchestrator.ScenarioPolicy);
        Assert.That(detail, Is.Not.Null);
        Assert.That(detail!.UnitId, Is.EqualTo("u1"));
        Assert.That(detail.IsAlive, Is.True);
    }

    [Test]
    public void BuildSelected_with_bridge_includes_live_attack_menu()
    {
        var bridge = new DelegationBridge(42, mvpEngagement: true, scenarioPolicyId: "baltic-patrol");
        var unit = bridge.Registry.RegisterUnit(new EntityKey(1), "u1");
        bridge.BeginExecution();
        var snapshot = new SimWorldSnapshotStub(contactCount: 1, hasFireControlTrackOnPrimaryContact: true);

        var detail = UnitDetailBridge.BuildSelected(
            unit.TargetId,
            snapshot,
            bridge);

        Assert.That(detail, Is.Not.Null);
        Assert.That(detail!.AttackMenu, Has.Count.EqualTo(3));
        Assert.That(detail.AttackMenu[0].Id, Is.EqualTo("fire-single"));
    }
}