namespace ProjectAegis.Delegation.Projection;

using Decision;
using ProjectAegis.Data.Scenario;
using ProjectAegis.Sim.Engage;
using ProjectAegis.Sim.Scenario;

/// <summary>Order-log projection for doc 17 losses/scoring MVP.</summary>
public static class LossesScoringProjection
{
    public const int DefaultPointsPerKill = 100;
    public const int DefaultPenaltyDenial = 5;

    /// <param name="log">Shared decision log containing engagement, magazine, and policy-denial entries.</param>
    /// <param name="baseScore">Score before kill points and denial penalties are applied.</param>
    /// <param name="scoredSide">
    /// Side the tally is being computed for ("blue"/"red", case-insensitive). The
    /// <see cref="DecisionLog"/> is shared by both sides, so without this a kill the *enemy*
    /// scored against one of your own units was credited to your own
    /// <see cref="LossesScoringSnapshot.HostileKills"/>
    /// (BUG-losses-scoring-side-unaware). Pass null only where no side context exists; that
    /// preserves the historical count-everything behaviour.
    /// </param>
    /// <param name="scenarioSides">
    /// Optional catalog side map captured before <see cref="BalticV3SideRegistry.ClearScenarioSides"/>.
    /// When provided, it is consulted before the live registry so scoring after a harness
    /// <c>finally</c> clear still filters catalog-backed shooters correctly (DRG-257 / Codex P2).
    /// </param>
    public static LossesScoringSnapshot Project(
        DecisionLog log,
        int baseScore = 0,
        string? scoredSide = null,
        IReadOnlyDictionary<string, string>? scenarioSides = null)
    {
        var kills = log.EngagementOutcomes.Count(o =>
            o.OutcomeCode == EngagementOutcomeCodes.Kill
            && ShooterCountsFor(scoredSide, o.ShooterTargetId.Value, scenarioSides));
        var missiles = log.MagazineChanges.Where(m => m.Delta < 0).Sum(m => -m.Delta);
        var denials = log.PolicyDenials.Count;
        var score = baseScore + (kills * DefaultPointsPerKill) - (denials * DefaultPenaltyDenial);
        return new LossesScoringSnapshot(score, kills, missiles, denials);
    }

    /// <summary>
    /// Reads gauntlet ORBAT sides from the scenario policy JSON without mutating the live
    /// <see cref="BalticV3SideRegistry"/>. Empty when the scenario has no catalog units.
    /// </summary>
    public static IReadOnlyDictionary<string, string> LoadScenarioSideMap(string scenarioPolicyId)
    {
        var dto = ScenarioPolicyJsonCatalog.TryGetJson(scenarioPolicyId);
        var units = dto?.Gauntlet?.Units;
        if (units == null || units.Count == 0)
        {
            return new Dictionary<string, string>(StringComparer.Ordinal);
        }

        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var unit in units)
        {
            if (string.IsNullOrWhiteSpace(unit.PlatformId))
            {
                continue;
            }

            var isBlue = !string.Equals(unit.Side, "red", StringComparison.OrdinalIgnoreCase);
            map[unit.PlatformId] = isBlue ? "blue" : "red";
        }

        return map;
    }

    /// <summary>
    /// A kill counts for <paramref name="scoredSide"/> unless the shooter is positively known to
    /// be on a different side. Unknown/unregistered shooters still count: legacy synthetic
    /// scenarios never call <c>RegisterScenarioSide</c>, and excluding them would silently
    /// score every pre-ORBAT scenario as zero.
    /// </summary>
    private static bool ShooterCountsFor(
        string? scoredSide,
        string shooterUnitId,
        IReadOnlyDictionary<string, string>? scenarioSides)
    {
        if (string.IsNullOrWhiteSpace(scoredSide))
        {
            return true;
        }

        string? shooterSide = null;
        if (scenarioSides != null
            && scenarioSides.TryGetValue(shooterUnitId, out var mapped))
        {
            shooterSide = mapped;
        }
        else
        {
            shooterSide = BalticV3SideRegistry.GetSideForUnit(shooterUnitId);
        }

        return shooterSide == null
            || string.Equals(shooterSide, scoredSide, StringComparison.OrdinalIgnoreCase);
    }
}
