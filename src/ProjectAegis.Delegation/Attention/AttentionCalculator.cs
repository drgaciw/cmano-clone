namespace ProjectAegis.Delegation.Attention;

using ProjectAegis.Delegation.Sim;

public static class AttentionCalculator
{
    public static AttentionEvaluation Evaluate(
        double budget,
        int memberCount,
        ObservedState state)
    {
        var load =
            state.ContactCount * 0.5 +
            state.ActiveEngagementCount * 1.0 +
            memberCount * 0.25;

        var degradation = new AttentionDegradation(
            SlowerReactions: load > budget,
            NarrowedFocus: load > budget * 1.25,
            SimplerDecisions: load > budget * 1.5);

        return new AttentionEvaluation(budget, load, degradation);
    }
}
