namespace ProjectAegis.Delegation.Roe;

using ProjectAegis.Delegation.Core;
using ProjectAegis.Sim.Policy;

/// <summary>ADR-002: bridges legacy IRoeFilter to ProjectAegis.Sim.IPolicyEvaluator.</summary>
public sealed class RoePolicyAdapter : IRoeFilter
{
    private readonly IPolicyEvaluator _evaluator;
    private readonly Func<Order, PolicyContext> _contextFactory;

    public RoePolicyAdapter(
        IPolicyEvaluator evaluator,
        Func<Order, PolicyContext>? contextFactory = null)
    {
        _evaluator = evaluator;
        _contextFactory = contextFactory ?? DefaultContext;
    }

    public RoeEvaluation Evaluate(Order order)
    {
        var ctx = _contextFactory(order);
        var request = OrderActionMapper.ToActionRequest(order);
        var verdict = _evaluator.Evaluate(ctx, request);
        return verdict.Allowed
            ? RoeEvaluation.Allow()
            : RoeEvaluation.Reject(verdict.Reason);
    }

    private static PolicyContext DefaultContext(Order order) =>
        new(
            OrderActionMapper.TargetIdToUlong(order.Target),
            PolicySnapshotId: 0,
            SimTick: (ulong)Math.Max(0, (long)order.SimTime),
            EffectivePolicy.DefaultFree);
}
