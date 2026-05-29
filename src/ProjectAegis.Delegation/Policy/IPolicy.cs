namespace ProjectAegis.Delegation.Policy;

using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Sim;
using ProjectAegis.Delegation.Traits;

public interface IPolicy
{
    IReadOnlyList<ScoredIntent> GenerateCandidates(PerceivedState perceived, TraitVector traits);
}
