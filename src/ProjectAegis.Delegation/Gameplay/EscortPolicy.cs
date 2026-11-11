namespace ProjectAegis.Delegation.Gameplay;

using Core;
using Decision;
using Policy;
using Sim;
using Traits;

/// <summary>Authoritative facts available to the escort policy, scoped to its own side.</summary>
public readonly record struct EscortPolicyFacts(bool CanEngage, bool TargetAlive, int RoundsRemaining, int ReserveRounds);

/// <summary>Contextual escort policy: identification, geometry and reserve limits precede weapon release.</summary>
public sealed class EscortPolicy : IPolicy
{
    private static readonly IReadOnlyList<ScoredIntent> Hold = new[] { new ScoredIntent(OrderKind.Hold, 1, RiskLevel.Low) };
    private static readonly IReadOnlyList<ScoredIntent> Engage = new[] { new ScoredIntent(OrderKind.Engage, 1, RiskLevel.High) };
    private readonly Func<EscortPolicyFacts> _facts;

    /// <summary>Injects a side-limited view of the authoritative mission world.</summary>
    public EscortPolicy(Func<EscortPolicyFacts> facts) => _facts = facts ?? throw new ArgumentNullException(nameof(facts));

    /// <summary>Withholds fire when track, geometry, liveness or the selected reserve policy forbids release.</summary>
    public IReadOnlyList<ScoredIntent> GenerateCandidates(PerceivedState perceived, TraitVector traits)
    {
        var facts = _facts();
        return facts.CanEngage && facts.TargetAlive && facts.RoundsRemaining > facts.ReserveRounds ? Engage : Hold;
    }
}
