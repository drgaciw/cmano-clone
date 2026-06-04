using ProjectAegis.Delegation.Controllers;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Decision;

public sealed class PlayerOrderExecutionQueueTests
{
    [Test]
    public void HumanController_drains_orders_only_after_execute_tick()
    {
        var human = new HumanController();
        var order = new Order(new OrderId(1), new TargetId("u1"), 5, OrderKind.Hold, RiskLevel.Low);
        human.Enqueue(order, executeSimTick: 7);

        Assert.That(human.DrainIssuedOrders(5), Is.Empty);
        Assert.That(human.DrainIssuedOrders(6), Is.Empty);
        var ready = human.DrainIssuedOrders(7);
        Assert.That(ready, Has.Count.EqualTo(1));
        Assert.That(ready[0].Kind, Is.EqualTo(OrderKind.Hold));
        Assert.That(human.DrainIssuedOrders(8), Is.Empty);
    }

    [Test]
    public void Queue_drains_in_enqueue_order_when_multiple_ready()
    {
        var queue = new PlayerOrderExecutionQueue();
        queue.Enqueue(new Order(new OrderId(1), new TargetId("a"), 0, OrderKind.Hold, RiskLevel.Low), 1);
        queue.Enqueue(new Order(new OrderId(2), new TargetId("b"), 0, OrderKind.Move, RiskLevel.Low), 1);

        var ready = queue.DrainReady(1);
        Assert.That(ready.Select(o => o.Kind), Is.EqualTo(new[] { OrderKind.Hold, OrderKind.Move }));
    }
}