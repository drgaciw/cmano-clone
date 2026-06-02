namespace ProjectAegis.Delegation.UnityAdapter.Tests.Bridge;

using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using NUnit.Framework;

[TestFixture]
public sealed class OobTreeBridgeTests
{
    [Test]
    public void RegisterUnit_appears_in_oob_tree_with_alive_state()
    {
        var bridge = new DelegationBridge(42);
        bridge.Registry.RegisterUnit(new EntityKey(1), "u1");
        var snapshot = new SimWorldSnapshotStub(contactCount: 0);
        var oob = OobTreeBridge.Build(snapshot, bridge.Registry);
        Assert.That(oob, Has.Count.EqualTo(1));
        Assert.That(oob[0].UnitId, Is.EqualTo("u1"));
        Assert.That(oob[0].IsAlive, Is.True);
    }
}