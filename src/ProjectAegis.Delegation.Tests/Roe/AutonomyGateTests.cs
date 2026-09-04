namespace ProjectAegis.Delegation.Tests.Roe;

using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Orchestration;
using ProjectAegis.Delegation.Roe;
using ProjectAegis.Sim.Policy;
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

    /// <summary>
    /// Adversarial: player approval must never bypass ROE reject (policy legality before Manual branch).
    /// Catches autonomy-before-ROE or "approved = superuser" regressions.
    /// </summary>
    [Test]
    public void player_approval_cannot_override_roe_reject_even_on_manual()
    {
        var gate = new AutonomyGate(new RoePolicyAdapter(
            new PolicyEvaluator(_ => new EffectivePolicy(RoeLevel.HoldFire))));
        var engage = new Order(new OrderId(1), new TargetId("u1"), 0, OrderKind.Engage, RiskLevel.High);

        var result = gate.Evaluate(AutonomyLevel.Manual, engage, playerApproved: true);

        Assert.That(result.Rejected, Is.True);
        Assert.That(result.ExecuteNow, Is.False);
        Assert.That(result.QueueForApproval, Is.False,
            "ROE reject is terminal; must not enter approval queue as if legal.");
        Assert.That(result.PolicyDenialReason, Is.EqualTo(FireAbortReason.RoeHoldFire));
    }

    /// <summary>
    /// HOL-04B (AEGIS-302 / DRG-232): SemiAutonomous lethal fire orders without prior authorization
    /// route to hold or pending review rather than instantaneous ExecuteNow. Non-lethal orders execute now.
    /// </summary>
    [Test]
    public void SemiAutonomous_holds_unauthorized_lethal_orders_for_approval()
    {
        var gate = new AutonomyGate(new PassthroughRoeFilter());
        var engage = new Order(new OrderId(1), new TargetId("u1"), 0, OrderKind.Engage, RiskLevel.High);
        var move = new Order(new OrderId(2), new TargetId("u1"), 0, OrderKind.Move, RiskLevel.Low);

        var engageResult = gate.Evaluate(AutonomyLevel.SemiAutonomous, engage, playerApproved: false);
        var moveResult = gate.Evaluate(AutonomyLevel.SemiAutonomous, move, playerApproved: false);
        var approvedEngageResult = gate.Evaluate(AutonomyLevel.SemiAutonomous, engage, playerApproved: true);

        Assert.That(engageResult.ExecuteNow, Is.False);
        Assert.That(engageResult.QueueForApproval, Is.True);
        Assert.That(moveResult.ExecuteNow, Is.True);
        Assert.That(moveResult.QueueForApproval, Is.False);
        Assert.That(approvedEngageResult.ExecuteNow, Is.True);
        Assert.That(approvedEngageResult.QueueForApproval, Is.False);
    }
}
