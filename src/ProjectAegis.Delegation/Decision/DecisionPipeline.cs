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

        // A policy signals "exclude this candidate from consideration" by scoring it at
        // zero or negative rather than omitting it from the list entirely (see
        // PatrolCandidateEngagePolicy's post-kill Engage pre-filter, which scores Engage
        // at 0.0 once the primary target is confirmed destroyed so it still surfaces in
        // the logged candidate set). The weight below is Math.Exp(score / temperature),
        // and Math.Exp(0) == 1 -- not 0 -- so without this filter a "de-prioritized"
        // zero-scored candidate still carries real softmax weight (comparable to any
        // other Low-magnitude candidate) and can still be drawn, silently defeating the
        // pre-filter. Only drop non-positive candidates when a positive-scored
        // alternative remains, so the pool is never emptied out.
        if (pool.Any(c => c.Score > 0) && pool.Any(c => c.Score <= 0))
        {
            pool = pool.Where(c => c.Score > 0).ToList();
        }

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
