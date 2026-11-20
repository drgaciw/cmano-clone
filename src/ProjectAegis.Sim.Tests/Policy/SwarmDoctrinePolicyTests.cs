using ProjectAegis.Sim.Policy;
using NUnit.Framework;

namespace ProjectAegis.Sim.Tests.Policy;

/// <summary>DRG-99 / SWARM-B7: doctrine/WRA for swarm auto-engage (SWARM-15).</summary>
public sealed class SwarmDoctrinePolicyTests
{
    private static PolicyVerdict Eval(EffectivePolicy policy, ActionRequest request, int salvo = 1)
    {
        var evaluator = new PolicyEvaluator(_ => policy);
        var ctx = new PolicyContext(1, 0, 0, policy, SalvoSize: salvo);
        return evaluator.Evaluate(ctx, request);
    }

    [Test]
    public void HoldFire_denies_auto_engage_with_RoeHoldFire()
    {
        var v = Eval(
            new EffectivePolicy(RoeLevel.HoldFire, AutoEngageAuthorized: true),
            new ActionRequest(ActionKind.FireGuided, 2, 0, IsAutoEngage: true));
        Assert.That(v.Allowed, Is.False);
        Assert.That(v.Reason, Is.EqualTo(FireAbortReason.RoeHoldFire));
    }

    [Test]
    public void WeaponsFree_auto_engage_denied_when_not_authorized()
    {
        var v = Eval(
            new EffectivePolicy(RoeLevel.WeaponsFree, AutoEngageAuthorized: false),
            new ActionRequest(ActionKind.FireGuided, 2, 0, IsAutoEngage: true));
        Assert.That(v.Allowed, Is.False);
        Assert.That(v.Reason, Is.EqualTo(FireAbortReason.AutoEngageDenied));
    }

    [Test]
    public void WeaponsFree_auto_engage_allowed_when_authorized()
    {
        var v = Eval(
            new EffectivePolicy(RoeLevel.WeaponsFree, AutoEngageAuthorized: true),
            new ActionRequest(ActionKind.FireGuided, 2, 0, IsAutoEngage: true));
        Assert.That(v.Allowed, Is.True);
    }

    [Test]
    public void Expend_without_authorization_denied()
    {
        var v = Eval(
            new EffectivePolicy(RoeLevel.WeaponsFree, ExpendAuthorized: false),
            new ActionRequest(ActionKind.FireGuided, 2, 0, IsExpend: true));
        Assert.That(v.Allowed, Is.False);
        Assert.That(v.Reason, Is.EqualTo(FireAbortReason.ExpendUnauthorized));
    }

    [Test]
    public void Expend_with_authorization_allowed()
    {
        var v = Eval(
            new EffectivePolicy(RoeLevel.WeaponsFree, ExpendAuthorized: true),
            new ActionRequest(ActionKind.FireGuided, 2, 0, IsExpend: true));
        Assert.That(v.Allowed, Is.True);
    }

    [Test]
    public void Wra_max_salvo_gates_auto_engage()
    {
        var v = Eval(
            new EffectivePolicy(RoeLevel.WeaponsFree, MaxSalvo: 1, AutoEngageAuthorized: true),
            new ActionRequest(ActionKind.FireGuided, 2, 0, IsAutoEngage: true),
            salvo: 2);
        Assert.That(v.Allowed, Is.False);
        Assert.That(v.Reason, Is.EqualTo(FireAbortReason.WraSalvo));
    }

    [Test]
    public void Default_policy_still_allows_manual_FireGuided()
    {
        var v = Eval(
            EffectivePolicy.DefaultFree,
            new ActionRequest(ActionKind.FireGuided, 2, 0));
        Assert.That(v.Allowed, Is.True);
        Assert.That(EffectivePolicy.DefaultFree.AutoEngageAuthorized, Is.True);
        Assert.That(EffectivePolicy.DefaultFree.ExpendAuthorized, Is.False);
    }

    [Test]
    public void WeaponsTight_denies_auto_engage_with_WeaponsTight_not_RoeHoldFire()
    {
        var v = Eval(
            new EffectivePolicy(RoeLevel.WeaponsTight, AutoEngageAuthorized: true),
            new ActionRequest(ActionKind.FireGuided, 2, 0, IsAutoEngage: true));
        Assert.That(v.Allowed, Is.False);
        Assert.That(v.Reason, Is.EqualTo(FireAbortReason.WeaponsTight));
        Assert.That(v.Reason, Is.Not.EqualTo(FireAbortReason.RoeHoldFire));
    }

    [Test]
    public void Manual_fire_ignores_AutoEngageAuthorized_false()
    {
        // Manual player fire is not auto-engage — AutoEngageAuthorized only gates IsAutoEngage.
        var v = Eval(
            new EffectivePolicy(RoeLevel.WeaponsFree, AutoEngageAuthorized: false),
            new ActionRequest(ActionKind.FireGuided, 2, 0, IsAutoEngage: false));
        Assert.That(v.Allowed, Is.True);
    }
}
