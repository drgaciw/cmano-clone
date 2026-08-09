namespace ProjectAegis.Delegation.Projection;

using ProjectAegis.Delegation.Decision;
using ProjectAegis.Sim.Engage;
using ProjectAegis.Sim.Scenario;

/// <summary>Order-log projection for doc 17 losses/scoring MVP.</summary>
public static class LossesScoringProjection
{
    public const int DefaultPointsPerKill = 100;
    public const int DefaultPenaltyDenial = 5;

    /// <param name="scoredSide">
    /// Side the tally is being computed for ("blue"/"red", case-insensitive). The
    /// <see cref="DecisionLog"/> is shared by both sides, so without this a kill the *enemy*
    /// scored against one of your own units was credited to your own
    /// <see cref="LossesScoringSnapshot.HostileKills"/>
    /// (BUG-losses-scoring-side-unaware). Pass null only where no side context exists; that
    /// preserves the historical count-everything behaviour.
    /// </param>
    public static LossesScoringSnapshot Project(DecisionLog log, int baseScore = 0, string? scoredSide = null)
    {
        var kills = log.EngagementOutcomes.Count(o =>
            o.OutcomeCode == EngagementOutcomeCodes.Kill
            && ShooterCountsFor(scoredSide, o.ShooterTargetId.Value));
        var missiles = log.MagazineChanges.Where(m => m.Delta < 0).Sum(m => -m.Delta);
        var denials = log.PolicyDenials.Count;
        var score = baseScore + (kills * DefaultPointsPerKill) - (denials * DefaultPenaltyDenial);
        return new LossesScoringSnapshot(score, kills, missiles, denials);
    }

    /// <summary>
    /// A kill counts for <paramref name="scoredSide"/> unless the shooter is positively known to
    /// be on a different side. Unknown/unregistered shooters still count: legacy synthetic
    /// scenarios never call <c>RegisterScenarioSide</c>, and excluding them would silently
    /// score every pre-ORBAT scenario as zero.
    /// </summary>
    private static bool ShooterCountsFor(string? scoredSide, string shooterUnitId)
    {
        if (string.IsNullOrWhiteSpace(scoredSide))
        {
            return true;
        }

        var shooterSide = BalticV3SideRegistry.GetSideForUnit(shooterUnitId);
        return shooterSide == null
            || string.Equals(shooterSide, scoredSide, StringComparison.OrdinalIgnoreCase);
    }
}
