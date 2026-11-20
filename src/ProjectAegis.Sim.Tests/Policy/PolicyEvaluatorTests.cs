using ProjectAegis.Sim.Policy;
using NUnit.Framework;

namespace ProjectAegis.Sim.Tests.Policy;

public sealed class PolicyEvaluatorTests
{

    [TestCase(RoeLevel.HoldFire, ActionKind.FireGuided, false)]
    [TestCase(RoeLevel.WeaponsTight, ActionKind.FireBallistic, false)]
    [TestCase(RoeLevel.WeaponsFree, ActionKind.FireGuided, true)]
    [TestCase(RoeLevel.HoldFire, ActionKind.Observe, true)]
    public void Roe_gates_fire_actions_only(RoeLevel roe, ActionKind kind, bool allowed)
    {
        var evaluator = new PolicyEvaluator(_ => new EffectivePolicy(roe));
        var ctx = new PolicyContext(1, 0, 0, new EffectivePolicy(roe));
        var verdict = evaluator.Evaluate(ctx, new ActionRequest(kind, 2, 0));
        Assert.That(verdict.Allowed, Is.EqualTo(allowed));
    }

    [Test]
    public void HoldFire_denies_with_RoeHoldFire_reason()
    {
        var evaluator = new PolicyEvaluator(_ => new EffectivePolicy(RoeLevel.HoldFire));
        var verdict = evaluator.Evaluate(
            new PolicyContext(1, 0, 0, new EffectivePolicy(RoeLevel.HoldFire)),
            new ActionRequest(ActionKind.FireGuided, 2, 0));
        Assert.That(verdict.Allowed, Is.False);
        Assert.That(verdict.Reason, Is.EqualTo(FireAbortReason.RoeHoldFire));
    }

    [Test]
    public void Salvo_exceeding_max_salvo_denies_with_WraSalvo()
    {
        var evaluator = new PolicyEvaluator(_ => new EffectivePolicy(RoeLevel.WeaponsFree, 1));
        var ctx = new PolicyContext(1, 0, 0, new EffectivePolicy(RoeLevel.WeaponsFree, 1), SalvoSize: 2);
        var verdict = evaluator.Evaluate(ctx, new ActionRequest(ActionKind.FireGuided, 2, 0));
        Assert.That(verdict.Allowed, Is.False);
        Assert.That(verdict.Reason, Is.EqualTo(FireAbortReason.WraSalvo));
    }

    [Test]
    public void Salvo_within_max_salvo_allows_weapons_free()
    {
        var evaluator = new PolicyEvaluator(_ => new EffectivePolicy(RoeLevel.WeaponsFree, 2));
        var ctx = new PolicyContext(1, 0, 0, new EffectivePolicy(RoeLevel.WeaponsFree, 2), SalvoSize: 2);
        var verdict = evaluator.Evaluate(ctx, new ActionRequest(ActionKind.FireGuided, 2, 0));
        Assert.That(verdict.Allowed, Is.True);
    }

    /// <summary>Wave 2 adversarial: WeaponsTight must not collapse to RoeHoldFire (doc 13 ROE-03 / dual abort codes).</summary>
    [Test]
    public void WeaponsTight_denies_with_WeaponsTight_not_RoeHoldFire()
    {
        var evaluator = new PolicyEvaluator(_ => new EffectivePolicy(RoeLevel.WeaponsTight));
        var verdict = evaluator.Evaluate(
            new PolicyContext(1, 0, 0, new EffectivePolicy(RoeLevel.WeaponsTight)),
            new ActionRequest(ActionKind.FireGuided, 2, 0));
        Assert.That(verdict.Allowed, Is.False);
        Assert.That(verdict.Reason, Is.EqualTo(FireAbortReason.WeaponsTight));
        Assert.That(verdict.Reason, Is.Not.EqualTo(FireAbortReason.RoeHoldFire));
    }

    /// <summary>Wave 2 adversarial: WRA exact boundary + zero MaxSalvo (doc 13 ROE-04).</summary>

    [TestCase(1, 1, true)]
    [TestCase(2, 1, false)]
    [TestCase(1, 0, false)]
    public void Wra_max_salvo_exact_boundary(int salvoSize, int maxSalvo, bool allowed)
    {
        var policy = new EffectivePolicy(RoeLevel.WeaponsFree, maxSalvo);
        var evaluator = new PolicyEvaluator(_ => policy);
        var ctx = new PolicyContext(1, 0, 0, policy, SalvoSize: salvoSize);
        var verdict = evaluator.Evaluate(ctx, new ActionRequest(ActionKind.FireGuided, 2, 0));
        Assert.That(verdict.Allowed, Is.EqualTo(allowed));
        if (!allowed)
        {
            Assert.That(verdict.Reason, Is.EqualTo(FireAbortReason.WraSalvo));
        }
    }
}