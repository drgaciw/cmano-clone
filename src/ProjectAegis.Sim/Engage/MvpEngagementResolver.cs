namespace ProjectAegis.Sim.Engage;

using ProjectAegis.Sim.Core;
using ProjectAegis.Sim.Policy;

/// <summary>MVP resolver: policy, fire-control track, envelope/DLZ, magazine consumption, combat outcome.</summary>
public sealed class MvpEngagementResolver : IEngagementResolver
{
    private readonly SimSeed _seed;
    private readonly IEngageWorldQuery _world;
    private readonly MagazineLedger _magazines;
    private readonly IPolicyEvaluator? _policyEvaluator;
    private readonly Func<ulong, EffectivePolicy>? _resolvePolicy;
    private readonly KilledTargetRegistry _killedTargets;
    private ulong _nextEngagementId = 1;

    public MvpEngagementResolver(
        IEngageWorldQuery world,
        MagazineLedger magazines,
        IPolicyEvaluator? policyEvaluator = null,
        Func<ulong, EffectivePolicy>? resolvePolicy = null,
        SimSeed? seed = null,
        KilledTargetRegistry? killedTargets = null)
    {
        _seed = seed ?? SimSeed.FromScenario(0);
        _world = world;
        _magazines = magazines;
        _policyEvaluator = policyEvaluator;
        _resolvePolicy = resolvePolicy;
        _killedTargets = killedTargets ?? new KilledTargetRegistry();
    }

    public MagazineLedger Magazines => _magazines;

    public KilledTargetRegistry KilledTargets => _killedTargets;

    public EngageResult Resolve(in EngageRequest request)
    {
        if (request.TargetId != 0 && _killedTargets.IsKilled(request.TargetId))
        {
            return EngageResult.Aborted(EngagementAbortReason.TargetDestroyed);
        }

        if (!_world.TryGetContext(request, out var ctx))
        {
            return EngageResult.Aborted(EngagementAbortReason.NoFireControlTrack);
        }

        if (_policyEvaluator != null)
        {
            var effective = _resolvePolicy?.Invoke(request.ShooterUnitId) ?? EffectivePolicy.DefaultFree;
            var salvoSize = Math.Max(1, ctx.SalvoSize);
            var policyCtx = new PolicyContext(
                request.ShooterUnitId,
                0,
                request.SimTick,
                effective,
                salvoSize);
            var action = new ActionRequest(ActionKind.FireGuided, request.TargetId, request.MountId);
            var verdict = _policyEvaluator.Evaluate(in policyCtx, in action);
            if (!verdict.Allowed)
            {
                return EngageResult.Aborted(MapPolicyDenial(verdict.Reason));
            }
        }

        if (!ctx.RadarEmconActive)
        {
            return EngageResult.Aborted(EngagementAbortReason.EmconOff);
        }

        if (!ctx.HasFireControlTrack)
        {
            return EngageResult.Aborted(EngagementAbortReason.NoFireControlTrack);
        }

        if (ctx.RoundsRemaining <= 0 && _magazines.GetRounds(request.ShooterUnitId, request.MountId) <= 0)
        {
            return EngageResult.Aborted(EngagementAbortReason.MagazineEmpty);
        }

        if (!ctx.Envelope.Contains(ctx.RangeMeters))
        {
            return EngageResult.Aborted(EngagementAbortReason.OutOfEnvelope);
        }

        if (DlzEvaluator.Evaluate(ctx.RangeMeters, ctx.Envelope) != DlzState.InZone)
        {
            return EngageResult.Aborted(EngagementAbortReason.DlzOut);
        }

        if (!_magazines.TryConsumeSalvo(request.ShooterUnitId, request.MountId, Math.Max(1, ctx.SalvoSize)))
        {
            return EngageResult.Aborted(EngagementAbortReason.MagazineEmpty);
        }

        var launch = EngageResult.Launch(_nextEngagementId++);
        var afterHit = CombatOutcomeResolver.Apply(_seed, request, launch, ctx.PkBase);
        var afterIntercept = CombatOutcomeResolver.ApplyInterceptOnHit(_seed, request, afterHit, ctx.PkIntercept);
        return CombatOutcomeResolver.ApplyKillOnHit(_seed, request, afterIntercept, ctx.PkKill);
    }

    private static EngagementAbortReason MapPolicyDenial(FireAbortReason reason) =>
        reason switch
        {
            FireAbortReason.RoeHoldFire => EngagementAbortReason.RoeHoldFire,
            FireAbortReason.WeaponsTight => EngagementAbortReason.WeaponsTight,
            FireAbortReason.WraSalvo => EngagementAbortReason.WraSalvo,
            FireAbortReason.EmconOff => EngagementAbortReason.EmconOff,
            FireAbortReason.NoFireControlTrack => EngagementAbortReason.NoFireControlTrack,
            _ => EngagementAbortReason.RoeHoldFire,
        };
}