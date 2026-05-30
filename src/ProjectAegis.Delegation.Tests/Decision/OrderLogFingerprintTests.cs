using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Sim.Policy;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Decision;

[TestFixture]
public sealed class OrderLogFingerprintTests
{
    [Test]
    public void Fingerprint_includes_policy_denial_and_is_stable()
    {
        var log = new DecisionLog();
        log.Append(new DecisionRecord(
            1, new AgentId("a"), new TargetId("u"), AutonomyLevel.FullAutonomous,
            OrderKind.Hold, Array.Empty<ScoredIntent>(), "r", 0, 20, 0.5));
        log.AppendPolicyDenial(new PolicyDenialRecord(
            0, 1, 1, new AgentId("a"), new TargetId("u"), 7,
            FireAbortReason.RoeHoldFire, OrderKind.Engage));

        var a = log.ComputeFingerprint();
        var b = log.ComputeFingerprint();
        Assert.That(a, Is.EqualTo(b));
        Assert.That(a, Does.Contain("PolicyDenial"));
        Assert.That(a, Does.Contain("RoeHoldFire"));
    }
}
