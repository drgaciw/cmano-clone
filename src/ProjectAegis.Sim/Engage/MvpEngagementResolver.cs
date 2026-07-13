namespace ProjectAegis.Sim.Engage;

using ProjectAegis.Sim.Core;
using ProjectAegis.Sim.Policy;
using ProjectAegis.Sim.Scenario;

/// <summary>MVP resolver: policy, fire-control track, envelope/DLZ, magazine consumption, combat outcome.</summary>
public sealed class MvpEngagementResolver : IEngagementResolver
{
    private readonly SimSeed _seed;
    private readonly IEngageWorldQuery _world;
    private readonly MagazineLedger _magazines;
    private readonly IPolicyEvaluator? _policyEvaluator;
    private readonly Func<ulong, EffectivePolicy>? _resolvePolicy;
    private readonly KilledTargetRegistry _killedTargets;
    private readonly ScenarioSpeculativeSettings _speculative;
    private readonly bool _combatDomainsEnabled;
    private readonly DomainValidatorRegistry _domainValidators;
    private ulong _nextEngagementId = 1;

    /// <summary>
    /// Opaque non-zero PolicySnapshotId used for policy contexts this resolver builds itself
    /// from its own <see cref="_resolvePolicy"/> callback. Never registered in, or looked up
    /// from, any PolicySnapshotRegistry — its sole purpose is to signal to
    /// <see cref="IPolicyEvaluator"/> implementations (like <see cref="PolicyEvaluator"/>) that
    /// the accompanying <see cref="PolicyContext.Effective"/> value is already resolved and
    /// must be trusted rather than re-resolved internally.
    /// </summary>
    private const ulong ResolvedPolicySnapshotMarker = 1;

    public MvpEngagementResolver(
        IEngageWorldQuery world,
        MagazineLedger magazines,
        IPolicyEvaluator? policyEvaluator = null,
        Func<ulong, EffectivePolicy>? resolvePolicy = null,
        SimSeed? seed = null,
        KilledTargetRegistry? killedTargets = null,
        ScenarioSpeculativeSettings? speculative = null,
        bool combatDomainsEnabled = false,
        DomainValidatorRegistry? domainValidators = null)
    {
        _seed = seed ?? SimSeed.FromScenario(0);
        _world = world;
        _magazines = magazines;
        _policyEvaluator = policyEvaluator;
        _resolvePolicy = resolvePolicy;
        _killedTargets = killedTargets ?? new KilledTargetRegistry();
        _speculative = speculative ?? ScenarioSpeculativeSettings.CampaignDefault;
        _combatDomainsEnabled = combatDomainsEnabled;
        _domainValidators = domainValidators ?? DomainValidatorRegistry.MvpStubs;
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

        var speculativeAbort = SpeculativeEngageGate.Evaluate(_speculative, in ctx);
        if (speculativeAbort != null)
        {
            return EngageResult.Aborted(speculativeAbort.Value);
        }

        if (_policyEvaluator != null)
        {
            var effective = _resolvePolicy?.Invoke(request.ShooterUnitId) ?? EffectivePolicy.DefaultFree;
            var salvoSize = Math.Max(1, ctx.SalvoSize);

            // PolicySnapshotId must be non-zero here: PolicyEvaluator.Evaluate treats 0 as
            // "no snapshot resolved yet, re-resolve via my own internal resolvePolicy delegate"
            // (see PolicySnapshotRegistry/RoePolicyAdapter, which legitimately rely on that
            // sentinel). This resolver already resolved the live per-unit policy above via its
            // own `_resolvePolicy` callback, so `effective` must be trusted as-is instead of
            // being silently discarded in favor of whatever fallback the injected evaluator
            // happens to carry — otherwise a unit's own ROE (e.g. HoldFire) can be ignored
            // whenever it differs from the evaluator's unrelated default.
            var policyCtx = new PolicyContext(
                request.ShooterUnitId,
                ResolvedPolicySnapshotMarker,
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

        if (_combatDomainsEnabled)
        {
            var domainResult = _domainValidators.Validate(ctx.CombatDomain, in ctx);
            if (!domainResult.Allowed)
            {
                return EngageResult.Aborted(MapDomainDenial(domainResult.AbortReason!.Value));
            }
        }

        if (!ctx.AirOperationsReady)
        {
            return EngageResult.Aborted(EngagementAbortReason.AirNotReady);
        }

        var damageWithdrawAbort = CatalogDamageWithdrawEngageGate.Evaluate(in ctx);
        if (damageWithdrawAbort != null)
        {
            return EngageResult.Aborted(damageWithdrawAbort.Value);
        }

        if (ctx.TrackSpoofed)
        {
            return EngageResult.Aborted(EngagementAbortReason.TrackSpoofed);
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

        var domainAbort = CombatDomainValidator.Validate(ctx.CombatDomain, in ctx);
        if (domainAbort != null)
        {
            return EngageResult.Aborted(domainAbort.Value);
        }

        var hypersonicAbort = HypersonicEngageGate.Evaluate(in ctx);
        if (hypersonicAbort != null)
        {
            return EngageResult.Aborted(hypersonicAbort.Value);
        }

        if (!ctx.Envelope.Contains(ctx.RangeMeters))
        {
            return EngageResult.Aborted(EngagementAbortReason.OutOfEnvelope);
        }

        if (!DlzEngageGate.AllowsLaunch(ctx.RangeMeters, ctx.Envelope, ctx.DlzPersonality))
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
            FireAbortReason.WraRange => EngagementAbortReason.OutOfEnvelope,
            FireAbortReason.EmconOff => EngagementAbortReason.EmconOff,
            FireAbortReason.NoFireControlTrack => EngagementAbortReason.NoFireControlTrack,
            _ => EngagementAbortReason.RoeHoldFire,
        };

    private static EngagementAbortReason MapDomainDenial(FireAbortReason reason) =>
        reason switch
        {
            FireAbortReason.NoFireControlTrack => EngagementAbortReason.NoFireControlTrack,
            FireAbortReason.EmconOff => EngagementAbortReason.EmconOff,
            FireAbortReason.AirAspectBlock => EngagementAbortReason.AirAspectBlock,
            FireAbortReason.SurfaceAspectBlock => EngagementAbortReason.SurfaceAspectBlock,
            FireAbortReason.SubsurfaceAspectBlock => EngagementAbortReason.SubsurfaceAspectBlock,
            FireAbortReason.LandAspectBlock => EngagementAbortReason.LandAspectBlock,
            FireAbortReason.MineAspectBlock => EngagementAbortReason.MineAspectBlock,
            FireAbortReason.FacilityAspectBlock => EngagementAbortReason.FacilityAspectBlock,
            _ => EngagementAbortReason.DomainNoSolution,
        };
}