namespace ProjectAegis.Sim.Policy;

/// <summary>MVP ROE + WRA evaluator per policy GDD (HoldFire / WeaponsTight / WeaponsFree + max salvo + SWARM-15).</summary>
public sealed class PolicyEvaluator : IPolicyEvaluator
{
    private readonly Func<ulong, EffectivePolicy> _resolvePolicy;
    private readonly EngagementSalvoLedger _salvoLedger;

    /// <summary>Engagement salvo ledger tracking munitions fired within active window ticks (AEGIS-303 / DRG-233).</summary>
    public EngagementSalvoLedger SalvoLedger => _salvoLedger;

    public PolicyEvaluator(
        Func<ulong, EffectivePolicy>? resolvePolicy = null,
        EngagementSalvoLedger? salvoLedger = null)
    {
        _resolvePolicy = resolvePolicy ?? (_ => EffectivePolicy.DefaultFree);
        _salvoLedger = salvoLedger ?? new EngagementSalvoLedger();
    }

    /// <summary>
    /// Registers fired munitions against a target track in the salvo ledger.
    /// </summary>
    public void RegisterFired(ulong targetTrackId, int count = 1, ulong simTick = 0, ulong? windowTicks = null) =>
        _salvoLedger.RegisterFired(targetTrackId, count, simTick, windowTicks);

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

        // SWARM-15: auto-engage posture (assault shots without explicit player click).
        if (request.IsAutoEngage && !policy.AutoEngageAuthorized)
        {
            return PolicyVerdict.Deny(FireAbortReason.AutoEngageDenied);
        }

        // SWARM-15/19: expend / kamikaze pulse requires explicit doctrine grant.
        if (request.IsExpend && !policy.ExpendAuthorized)
        {
            return PolicyVerdict.Deny(FireAbortReason.ExpendUnauthorized);
        }

        var salvo = Math.Max(1, ctx.SalvoSize);
        if (salvo > policy.MaxSalvo)
        {
            return PolicyVerdict.Deny(FireAbortReason.WraSalvo);
        }

        // AEGIS-303 (DRG-233): verify cumulative salvo count against the target track within active window ticks.
        var cumulativeCount = _salvoLedger.GetActiveSalvoCount(request.TargetId, ctx.SimTick);
        if (cumulativeCount + salvo > policy.MaxSalvo)
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
