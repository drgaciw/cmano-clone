namespace ProjectAegis.Delegation.Decision;

using ProjectAegis.Delegation.Attention;
using ProjectAegis.Delegation.Traits;

public sealed record PipelineChoice(ScoredIntent Chosen, double RngDraw, string Rationale);

public static class DecisionPipeline
{
    public static PipelineChoice Choose(
        IReadOnlyList<ScoredIntent> candidates,
        TraitVector traits,
        AttentionEvaluation attention,
        SeededRng rng)
    {
        var pool = candidates.ToList();
        if (attention.Degradation.NarrowedFocus)
        {
            pool = pool.OrderByDescending(c => c.Score).Take(2).ToList();
        }

        var temperature = Math.Max(0.05, traits.Decisiveness * (attention.IsOverloaded ? 0.5 : 1.0));
        var weights = pool.Select(c => Math.Exp(c.Score / temperature)).ToArray();
        var sum = weights.Sum();
        var draw = rng.NextUnit() * sum;
        var acc = 0.0;
        var index = 0;
        for (; index < pool.Count; index++)
        {
            acc += weights[index];
            if (draw <= acc)
            {
                break;
            }
        }

        var chosen = pool[Math.Min(index, pool.Count - 1)];
        var rationale = attention.IsOverloaded
            ? "overload: narrowed focus applied"
            : "nominal: trait-weighted stochastic choice";

        return new PipelineChoice(chosen, draw, rationale);
    }
}
