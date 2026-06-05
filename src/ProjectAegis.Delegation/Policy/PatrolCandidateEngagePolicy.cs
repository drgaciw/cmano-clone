namespace ProjectAegis.Delegation.Policy;

using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Sim;
using ProjectAegis.Delegation.Traits;

/// <summary>Logs full patrol scored-intent set while biasing stochastic choice toward Engage (Baltic replay).</summary>
public sealed class PatrolCandidateEngagePolicy : IPolicy
{
    public static readonly IReadOnlyList<ScoredIntent> Candidates =
    [
        new ScoredIntent(OrderKind.Hold, 1.0, RiskLevel.Low),
        new ScoredIntent(OrderKind.Move, 0.8, RiskLevel.Low),
        new ScoredIntent(OrderKind.Engage, 99.0, RiskLevel.High),
    ];

    public IReadOnlyList<ScoredIntent> GenerateCandidates(PerceivedState perceived, TraitVector traits)
    {
        _ = perceived;
        _ = traits;
        return Candidates;
    }
}