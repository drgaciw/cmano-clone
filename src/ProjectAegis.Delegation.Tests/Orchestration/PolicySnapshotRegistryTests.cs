using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Orchestration;
using ProjectAegis.Sim.Policy;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Orchestration;

[TestFixture]
public sealed class PolicySnapshotRegistryTests
{
    [Test]
    public void Capture_assigns_monotonic_snapshot_ids()
    {
        var registry = new PolicySnapshotRegistry();
        var id1 = registry.Capture(new TargetId("a"), new EffectivePolicy(RoeLevel.HoldFire), 0);
        var id2 = registry.Capture(new TargetId("b"), EffectivePolicy.DefaultFree, 0);
        Assert.That(id2, Is.GreaterThan(id1));
        Assert.That(registry.TryGet(new TargetId("a"), out var snap), Is.True);
        Assert.That(snap.Effective.Roe, Is.EqualTo(RoeLevel.HoldFire));
    }
}
