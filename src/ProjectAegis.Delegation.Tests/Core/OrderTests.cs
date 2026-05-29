namespace ProjectAegis.Delegation.Tests.Core;

using ProjectAegis.Delegation.Core;
using NUnit.Framework;

[TestFixture]
public sealed class OrderTests
{
    [Test]
    public void Order_stores_kind_target_and_sim_time()
    {
        var target = new TargetId("unit-1");
        var order = new Order(
            new OrderId(1),
            target,
            simTime: 42.5,
            OrderKind.Hold,
            RiskLevel.Low);

        Assert.That(order.Target, Is.EqualTo(target));
        Assert.That(order.SimTime, Is.EqualTo(42.5));
        Assert.That(order.Kind, Is.EqualTo(OrderKind.Hold));
        Assert.That(order.Risk, Is.EqualTo(RiskLevel.Low));
    }
}
