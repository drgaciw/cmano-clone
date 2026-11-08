namespace ProjectAegis.Sim.Engage;

/// <summary>ADR-009: stable damage apply order by engagement id then sequence id.</summary>
public static class DeterministicDamageApplyBatch
{
    public static IReadOnlyList<EngagementDamageOutcome> Sort(IEnumerable<EngagementDamageOutcome> outcomes) =>
        outcomes
            .OrderBy(o => o.EngagementId)
            .ThenBy(o => o.SequenceId)
            .ToArray();
}