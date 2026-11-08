namespace ProjectAegis.Delegation.Policy;

using Core;
using Decision;
using Sim;
using Traits;

public sealed class StubPatrolPolicy : IPolicy
{
    public static readonly IReadOnlyList<ScoredIntent> DefaultCandidates =
    [
        new ScoredIntent(OrderKind.Hold, 1.0, RiskLevel.Low),
        new ScoredIntent(OrderKind.Move, 0.8, RiskLevel.Low),
        new ScoredIntent(OrderKind.Engage, 0.6, RiskLevel.High),
    ];

    public IReadOnlyList<ScoredIntent> GenerateCandidates(PerceivedState perceived, TraitVector traits) =>
        DefaultCandidates;
}
