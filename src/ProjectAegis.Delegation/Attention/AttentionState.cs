namespace ProjectAegis.Delegation.Attention;

public sealed record AttentionDegradation(
    bool SlowerReactions,
    bool NarrowedFocus,
    bool SimplerDecisions);

public sealed record AttentionEvaluation(
    double Budget,
    double Load,
    AttentionDegradation Degradation)
{
    public bool IsOverloaded => Load > Budget;
}
