namespace ProjectAegis.Delegation.Policy;

using Decision;
using Sim;
using Traits;

public interface IPolicy
{
    IReadOnlyList<ScoredIntent> GenerateCandidates(PerceivedState perceived, TraitVector traits);
}
