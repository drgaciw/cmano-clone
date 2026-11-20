using ProjectAegis.Sim.Policy;
using NUnit.Framework;

namespace ProjectAegis.Sim.Tests.Policy;

public sealed class PolicySnapshotEvaluatorTests
{
    [Test]
    public void Snapshot_effective_overrides_resolver()
    {
        var evaluator = new PolicyEvaluator(_ => new EffectivePolicy(RoeLevel.WeaponsFree));
        var ctx = new PolicyContext(1, 99, 1, new EffectivePolicy(RoeLevel.HoldFire));
        var verdict = evaluator.Evaluate(ctx, new ActionRequest(ActionKind.FireGuided, 2, 0));
        Assert.That(verdict.Allowed, Is.False);
        Assert.That(verdict.Reason, Is.EqualTo(FireAbortReason.RoeHoldFire));
    }
}
