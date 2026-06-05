namespace ProjectAegis.Delegation.Projection;

using ProjectAegis.Delegation.Decision;
using ProjectAegis.Sim.Engage;

/// <summary>Order-log projection for doc 17 losses/scoring MVP.</summary>
public static class LossesScoringProjection
{
    public const int DefaultPointsPerKill = 100;
    public const int DefaultPenaltyDenial = 5;

    public static LossesScoringSnapshot Project(DecisionLog log, int baseScore = 0)
    {
        var kills = log.EngagementOutcomes.Count(o => o.OutcomeCode == EngagementOutcomeCodes.Kill);
        var missiles = log.MagazineChanges.Where(m => m.Delta < 0).Sum(m => -m.Delta);
        var denials = log.PolicyDenials.Count;
        var score = baseScore + (kills * DefaultPointsPerKill) - (denials * DefaultPenaltyDenial);
        return new LossesScoringSnapshot(score, kills, missiles, denials);
    }
}