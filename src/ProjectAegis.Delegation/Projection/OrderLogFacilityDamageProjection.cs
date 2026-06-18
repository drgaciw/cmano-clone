namespace ProjectAegis.Delegation.Projection;

using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Sim.Engage;

/// <summary>Order-log-only facility damage: sorted hit/kill outcomes emit capacity change rows (ADR-009 / TR-combat-dom-003).</summary>
public static class OrderLogFacilityDamageProjection
{
    public static IReadOnlyList<FacilityDamageChangeRecord> ProjectDamageChanges(
        DecisionLog log,
        IReadOnlyDictionary<string, FacilityPictureEntry> facilityByTargetId) =>
        ProjectDamageChanges(log.EngagementOutcomes, facilityByTargetId);

    public static IReadOnlyList<FacilityDamageChangeRecord> ProjectDamageChanges(
        IReadOnlyList<EngagementOutcomeRecord> outcomes,
        IReadOnlyDictionary<string, FacilityPictureEntry> facilityByTargetId)
    {
        if (facilityByTargetId.Count == 0)
        {
            return Array.Empty<FacilityDamageChangeRecord>();
        }

        var damageOutcomes = outcomes
            .Where(o =>
                (o.OutcomeCode == EngagementOutcomeCodes.Hit || o.OutcomeCode == EngagementOutcomeCodes.Kill) &&
                facilityByTargetId.ContainsKey(o.VictimTargetId.Value))
            .Select(o => new EngagementDamageOutcome(o.EngagementId, o.SequenceId, o.OutcomeCode))
            .ToArray();

        var sortedDamage = DeterministicDamageApplyBatch.Sort(damageOutcomes);
        var changes = new List<FacilityDamageChangeRecord>(sortedDamage.Count);
        var capacityByTargetId = facilityByTargetId.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.CapacityState,
            StringComparer.Ordinal);

        foreach (var damage in sortedDamage)
        {
            var outcome = outcomes.First(o =>
                o.EngagementId == damage.EngagementId &&
                o.SequenceId == damage.SequenceId &&
                o.OutcomeCode == damage.OutcomeCode);

            var targetId = outcome.VictimTargetId.Value;
            if (!facilityByTargetId.TryGetValue(targetId, out var facility))
            {
                continue;
            }

            var previous = capacityByTargetId.GetValueOrDefault(targetId, FacilityCapacityStates.Operational);
            if (string.Equals(previous, FacilityCapacityStates.Destroyed, StringComparison.Ordinal))
            {
                continue;
            }

            var next = MapOutcomeToCapacityState(damage.OutcomeCode);
            if (next is null ||
                (string.Equals(next, FacilityCapacityStates.Damaged, StringComparison.Ordinal) &&
                 string.Equals(previous, FacilityCapacityStates.Damaged, StringComparison.Ordinal)))
            {
                continue;
            }

            changes.Add(new FacilityDamageChangeRecord(
                outcome.SequenceId,
                outcome.SimTime,
                outcome.SimTick,
                facility.FacilityId,
                facility.TargetId,
                previous,
                next));

            capacityByTargetId[targetId] = next;
        }

        return changes;
    }

    private static string? MapOutcomeToCapacityState(string outcomeCode) =>
        outcomeCode switch
        {
            EngagementOutcomeCodes.Hit => FacilityCapacityStates.Damaged,
            EngagementOutcomeCodes.Kill => FacilityCapacityStates.Destroyed,
            _ => null,
        };
}