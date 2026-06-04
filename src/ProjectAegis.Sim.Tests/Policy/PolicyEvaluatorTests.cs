using ProjectAegis.Sim.Policy;
using Xunit;

namespace ProjectAegis.Sim.Tests.Policy;

public sealed class PolicyEvaluatorTests
{
    [Theory]
    [InlineData(RoeLevel.HoldFire, ActionKind.FireGuided, false)]
    [InlineData(RoeLevel.WeaponsTight, ActionKind.FireBallistic, false)]
    [InlineData(RoeLevel.WeaponsFree, ActionKind.FireGuided, true)]
    [InlineData(RoeLevel.HoldFire, ActionKind.Observe, true)]
    public void Roe_gates_fire_actions_only(RoeLevel roe, ActionKind kind, bool allowed)
    {
        var evaluator = new PolicyEvaluator(_ => new EffectivePolicy(roe));
        var ctx = new PolicyContext(1, 0, 0, new EffectivePolicy(roe));
        var verdict = evaluator.Evaluate(ctx, new ActionRequest(kind, 2, 0));
        Assert.Equal(allowed, verdict.Allowed);
    }

    [Fact]
    public void HoldFire_denies_with_RoeHoldFire_reason()
    {
        var evaluator = new PolicyEvaluator(_ => new EffectivePolicy(RoeLevel.HoldFire));
        var verdict = evaluator.Evaluate(
            new PolicyContext(1, 0, 0, new EffectivePolicy(RoeLevel.HoldFire)),
            new ActionRequest(ActionKind.FireGuided, 2, 0));
        Assert.False(verdict.Allowed);
        Assert.Equal(FireAbortReason.RoeHoldFire, verdict.Reason);
    }

    [Fact]
    public void Salvo_exceeding_max_salvo_denies_with_WraSalvo()
    {
        var evaluator = new PolicyEvaluator(_ => new EffectivePolicy(RoeLevel.WeaponsFree, 1));
        var ctx = new PolicyContext(1, 0, 0, new EffectivePolicy(RoeLevel.WeaponsFree, 1), SalvoSize: 2);
        var verdict = evaluator.Evaluate(ctx, new ActionRequest(ActionKind.FireGuided, 2, 0));
        Assert.False(verdict.Allowed);
        Assert.Equal(FireAbortReason.WraSalvo, verdict.Reason);
    }

    [Fact]
    public void Salvo_within_max_salvo_allows_weapons_free()
    {
        var evaluator = new PolicyEvaluator(_ => new EffectivePolicy(RoeLevel.WeaponsFree, 2));
        var ctx = new PolicyContext(1, 0, 0, new EffectivePolicy(RoeLevel.WeaponsFree, 2), SalvoSize: 2);
        var verdict = evaluator.Evaluate(ctx, new ActionRequest(ActionKind.FireGuided, 2, 0));
        Assert.True(verdict.Allowed);
    }
}