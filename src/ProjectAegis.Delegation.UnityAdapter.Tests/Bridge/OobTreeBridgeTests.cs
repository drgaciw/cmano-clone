namespace ProjectAegis.Delegation.UnityAdapter.Tests.Bridge;

using System;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using NUnit.Framework;

/// <summary>
/// Headless dogfood for OOB presentation bridge (UCA-A4a / DRG-140).
/// Proves Build is projection-only: snapshot + registry to IReadOnlyList of OobTreeEntry.
/// </summary>
[TestFixture]
public sealed class OobTreeBridgeTests
{
    [Test]
    public void RegisterUnit_appears_in_oob_tree_with_alive_state()
    {
        var bridge = new DelegationBridge(42, mvpEngagement: false);
        bridge.Registry.RegisterUnit(new EntityKey(1), "u1");
        var snapshot = new SimWorldSnapshotStub(contactCount: 0);
        var oob = OobTreeBridge.Build(snapshot, bridge.Registry);
        Assert.That(oob, Has.Count.EqualTo(1));
        Assert.That(oob[0].UnitId, Is.EqualTo("u1"));
        Assert.That(oob[0].IsAlive, Is.True);
    }

    [Test]
    public void Build_dead_member_marks_not_alive()
    {
        var bridge = new DelegationBridge(42, mvpEngagement: false);
        bridge.Registry.RegisterUnit(new EntityKey(1), "u1");
        var snapshot = new SimWorldSnapshotStub(contactCount: 0, memberAlive: false);
        var oob = OobTreeBridge.Build(snapshot, bridge.Registry);
        Assert.That(oob, Has.Count.EqualTo(1));
        Assert.That(oob[0].IsAlive, Is.False);
    }

    [Test]
    public void Build_empty_registry_returns_empty_readonly_list()
    {
        var bridge = new DelegationBridge(42, mvpEngagement: false);
        var snapshot = new SimWorldSnapshotStub(contactCount: 0);
        var oob = OobTreeBridge.Build(snapshot, bridge.Registry);
        Assert.That(oob, Is.InstanceOf<IReadOnlyList<OobTreeEntry>>());
        Assert.That(oob, Is.Empty);
    }

    [Test]
    public void Build_null_snapshot_throws()
    {
        var bridge = new DelegationBridge(42, mvpEngagement: false);
        Assert.Throws<ArgumentNullException>((Action)(() =>
            OobTreeBridge.Build(null!, bridge.Registry)));
    }

    [Test]
    public void Build_null_registry_throws()
    {
        var snapshot = new SimWorldSnapshotStub();
        Assert.Throws<ArgumentNullException>((Action)(() =>
            OobTreeBridge.Build(snapshot, null!)));
    }
}
