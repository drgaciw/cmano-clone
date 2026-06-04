namespace ProjectAegis.Sim.Policy;

/// <summary>MVP ROE + WRA evaluator per policy GDD (HoldFire / WeaponsTight / WeaponsFree + max salvo).</summary>
public sealed class PolicyEvaluator : IPolicyEvaluator
{
    private readonly Func<ulong, EffectivePolicy> _resolvePolicy;

    public PolicyEvaluator(Func<ulong, EffectivePolicy>? resolvePolicy = null)
    {
        _resolvePolicy = resolvePolicy ?? (_ => EffectivePolicy.DefaultFree);
    }

    public PolicyVerdict Evaluate(in PolicyContext ctx, in ActionRequest request)
    {
        var policy = ctx.PolicySnapshotId != 0
            ? ctx.Effective
            : _resolvePolicy(ctx.UnitId);

        if (!IsFireAction(request.Kind))
        {
            return PolicyVerdict.Allow();
        }

        var roeVerdict = EvaluateRoe(policy.Roe);
        if (!roeVerdict.Allowed)
        {
            return roeVerdict;
        }

        var salvo = Math.Max(1, ctx.SalvoSize);
        if (salvo > policy.MaxSalvo)
        {
            return PolicyVerdict.Deny(FireAbortReason.WraSalvo);
        }

        return PolicyVerdict.Allow();
    }

    private static PolicyVerdict EvaluateRoe(RoeLevel roe) =>
        roe switch
        {
            RoeLevel.HoldFire => PolicyVerdict.Deny(FireAbortReason.RoeHoldFire),
            RoeLevel.WeaponsTight => PolicyVerdict.Deny(FireAbortReason.WeaponsTight),
            RoeLevel.WeaponsFree => PolicyVerdict.Allow(),
            _ => PolicyVerdict.Allow(),
        };

    private static bool IsFireAction(ActionKind kind) =>
        kind is ActionKind.FireBallistic or ActionKind.FireGuided;
}