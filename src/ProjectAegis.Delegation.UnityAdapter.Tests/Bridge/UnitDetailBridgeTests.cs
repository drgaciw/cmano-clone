namespace ProjectAegis.Delegation.UnityAdapter.Tests.Bridge;

using System;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.UnityAdapter.Baltic;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using NUnit.Framework;

/// <summary>
/// Headless dogfood for unit detail presentation bridge (UCA-P1b / DRG-145).
/// Proves BuildPrimary/BuildSelected are projection-only with null guards.
/// </summary>
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

    [Test]
    public void BuildPrimary_null_snapshot_throws()
    {
        var bridge = new DelegationBridge(42, mvpEngagement: false);
        Assert.Throws<ArgumentNullException>((Action)(() =>
            UnitDetailBridge.BuildPrimary(
                null!,
                bridge.Registry,
                bridge.Orchestrator.DecisionLog,
                null)));
    }

    [Test]
    public void BuildPrimary_null_registry_throws()
    {
        var snapshot = new SimWorldSnapshotStub();
        var log = new DecisionLog();
        Assert.Throws<ArgumentNullException>((Action)(() =>
            UnitDetailBridge.BuildPrimary(snapshot, null!, log, null)));
    }

    [Test]
    public void BuildPrimary_null_log_throws()
    {
        var bridge = new DelegationBridge(42, mvpEngagement: false);
        var snapshot = new SimWorldSnapshotStub();
        Assert.Throws<ArgumentNullException>((Action)(() =>
            UnitDetailBridge.BuildPrimary(snapshot, bridge.Registry, null!, null)));
    }

    [Test]
    public void BuildPrimary_bridge_overload_null_bridge_throws()
    {
        var snapshot = new SimWorldSnapshotStub();
        Assert.Throws<ArgumentNullException>((Action)(() =>
            UnitDetailBridge.BuildPrimary(snapshot, null!)));
    }

    [Test]
    public void BuildSelected_null_snapshot_throws()
    {
        var log = new DecisionLog();
        Assert.Throws<ArgumentNullException>((Action)(() =>
            UnitDetailBridge.BuildSelected(new TargetId("u1"), null!, log, null)));
    }
}
