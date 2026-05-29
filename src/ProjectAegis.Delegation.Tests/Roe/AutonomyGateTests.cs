namespace ProjectAegis.Delegation.Tests.Roe;

using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Orchestration;
using ProjectAegis.Delegation.Roe;
using NUnit.Framework;

[TestFixture]
public sealed class AutonomyGateTests
{
    [Test]
    public void Manual_never_executes_without_approval()
    {
        var gate = new AutonomyGate(new PassthroughRoeFilter());
        var order = new Order(new OrderId(1), new TargetId("u1"), 0, OrderKind.Engage, RiskLevel.High);
        var result = gate.Evaluate(AutonomyLevel.Manual, order, playerApproved: false);
        Assert.That(result.ExecuteNow, Is.False);
        Assert.That(result.QueueForApproval, Is.True);
    }

    [Test]
    public void Assisted_auto_executes_low_risk_only()
    {
        var gate = new AutonomyGate(new PassthroughRoeFilter());
        var low = new Order(new OrderId(1), new TargetId("u1"), 0, OrderKind.Hold, RiskLevel.Low);
        var high = new Order(new OrderId(2), new TargetId("u1"), 0, OrderKind.Engage, RiskLevel.High);
        Assert.That(gate.Evaluate(AutonomyLevel.Assisted, low, playerApproved: false).ExecuteNow, Is.True);
        Assert.That(gate.Evaluate(AutonomyLevel.Assisted, high, playerApproved: false).QueueForApproval, Is.True);
    }
}
