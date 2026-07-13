namespace ProjectAegis.Delegation.Projection;

using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Sim.Catalog;
using ProjectAegis.Sim.Engage;

/// <summary>Order-log facility damage: HP-ledger projection (S31-05) with S28-09 outcome fallback.</summary>
public static class OrderLogFacilityDamageProjection
{
    public static IReadOnlyList<FacilityDamageChangeRecord> ProjectDamageChanges(
        DecisionLog log,
        IReadOnlyDictionary<string, FacilityPictureEntry> facilityByTargetId) =>
        ProjectDamageChanges(log.PlatformDamageChanges, log.EngagementOutcomes, facilityByTargetId);

    public static IReadOnlyList<FacilityDamageChangeRecord> ProjectDamageChanges(
        IReadOnlyList<PlatformDamageChangeRecord> platformDamageChanges,
        IReadOnlyList<EngagementOutcomeRecord> outcomes,
        IReadOnlyDictionary<string, FacilityPictureEntry> facilityByTargetId)
    {
        if (facilityByTargetId.Count == 0)
        {
            return Array.Empty<FacilityDamageChangeRecord>();
        }

        var facilityHpChanges = platformDamageChanges
            .Where(c => facilityByTargetId.ContainsKey(c.UnitId.Value))
            .OrderBy(c => c.SimTick)
            .ThenBy(c => c.SequenceId)
            .ToArray();
        if (facilityHpChanges.Length > 0)
        {
            return ProjectDamageChangesFromHpLedger(facilityHpChanges, facilityByTargetId);
        }

        return ProjectDamageChangesFromOutcomes(outcomes, facilityByTargetId);
    }

    public static IReadOnlyList<FacilityDamageChangeRecord> ProjectDamageChanges(
        IReadOnlyList<EngagementOutcomeRecord> outcomes,
        IReadOnlyDictionary<string, FacilityPictureEntry> facilityByTargetId) =>
        ProjectDamageChangesFromOutcomes(outcomes, facilityByTargetId);

    private static IReadOnlyList<FacilityDamageChangeRecord> ProjectDamageChangesFromHpLedger(
        IReadOnlyList<PlatformDamageChangeRecord> hpChanges,
        IReadOnlyDictionary<string, FacilityPictureEntry> facilityByTargetId)
    {
        var changes = new List<FacilityDamageChangeRecord>(hpChanges.Count);
        var capacityByTargetId = facilityByTargetId.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.CapacityState,
            StringComparer.Ordinal);

        foreach (var hpChange in hpChanges)
        {
            var targetId = hpChange.UnitId.Value;
            if (!facilityByTargetId.TryGetValue(targetId, out var facility))
            {
                continue;
            }

            var previous = capacityByTargetId.GetValueOrDefault(targetId, FacilityCapacityStates.Operational);
            var nextHpCapacity = FacilityHpCapacity.MapHpPctToCapacityState(hpChange.NewHpPct);

            if (!FacilityHpCapacity.ShouldEmitCapacityTransition(previous, nextHpCapacity))
            {
                continue;
            }

            changes.Add(new FacilityDamageChangeRecord(
                hpChange.SequenceId,
                hpChange.SimTime,
                hpChange.SimTick,
                facility.FacilityId,
                facility.TargetId,
                previous,
                nextHpCapacity));

            capacityByTargetId[targetId] = nextHpCapacity;
        }

        return changes;
    }

    private static IReadOnlyList<FacilityDamageChangeRecord> ProjectDamageChangesFromOutcomes(
        IReadOnlyList<EngagementOutcomeRecord> outcomes,
        IReadOnlyDictionary<string, FacilityPictureEntry> facilityByTargetId)
    {
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