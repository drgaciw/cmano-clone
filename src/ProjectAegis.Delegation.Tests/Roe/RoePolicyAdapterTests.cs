using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Orchestration;
using ProjectAegis.Delegation.Roe;
using ProjectAegis.Sim.Policy;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Roe;

[TestFixture]
public sealed class RoePolicyAdapterTests
{
    [Test]
    public void HoldFire_rejects_engage_via_adapter()
    {
        var gate = new AutonomyGate(new RoePolicyAdapter(
            new PolicyEvaluator(_ => new EffectivePolicy(RoeLevel.HoldFire))));
        var order = new Order(new OrderId(1), new TargetId("u1"), 0, OrderKind.Engage, RiskLevel.High);
        var result = gate.Evaluate(AutonomyLevel.FullAutonomous, order, playerApproved: false);
        Assert.That(result.Rejected, Is.True);
        Assert.That(result.ExecuteNow, Is.False);
    }

    [Test]
    public void WeaponsFree_allows_engage_for_autonomous()
    {
        var gate = new AutonomyGate(new RoePolicyAdapter(new PolicyEvaluator()));
        var order = new Order(new OrderId(1), new TargetId("u1"), 0, OrderKind.Engage, RiskLevel.High);
        var result = gate.Evaluate(AutonomyLevel.FullAutonomous, order, playerApproved: false);
        Assert.That(result.ExecuteNow, Is.True);
    }
}
