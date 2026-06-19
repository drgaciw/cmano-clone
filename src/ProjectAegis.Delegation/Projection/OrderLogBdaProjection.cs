namespace ProjectAegis.Delegation.Projection;

using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Sim.Catalog;
using ProjectAegis.Sim.Engage;

/// <summary>Order-log-only BDA: sorted damage outcomes emit contact degraded/Lost rows (ADR-009 / TR-combat-dom-003).</summary>
public static class OrderLogBdaProjection
{
    public static IReadOnlyList<ContactChangeRecord> ProjectBdaContactChanges(
        DecisionLog log,
        IReadOnlyDictionary<string, ContactPictureEntry> contactByTargetId) =>
        ProjectBdaContactChanges(
            log.PlatformDamageChanges,
            log.EngagementOutcomes,
            contactByTargetId);

    public static IReadOnlyList<ContactChangeRecord> ProjectBdaContactChanges(
        IReadOnlyList<PlatformDamageChangeRecord> platformDamageChanges,
        IReadOnlyList<EngagementOutcomeRecord> engagementOutcomes,
        IReadOnlyDictionary<string, ContactPictureEntry> contactByTargetId)
    {
        if (contactByTargetId.Count == 0)
        {
            return Array.Empty<ContactChangeRecord>();
        }

        var events = new List<BdaContactEvent>();
        events.AddRange(CollectPlatformDamageEvents(platformDamageChanges, contactByTargetId));
        events.AddRange(CollectKillOutcomeEvents(engagementOutcomes, contactByTargetId));

        if (events.Count == 0)
        {
            return Array.Empty<ContactChangeRecord>();
        }

        events.Sort(static (a, b) =>
        {
            var tick = a.SimTick.CompareTo(b.SimTick);
            if (tick != 0)
            {
                return tick;
            }

            var engagement = a.EngagementId.CompareTo(b.EngagementId);
            return engagement != 0 ? engagement : a.SequenceId.CompareTo(b.SequenceId);
        });

        var changes = new List<ContactChangeRecord>(events.Count);
        var stateByTargetId = contactByTargetId.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.LifecycleState,
            StringComparer.Ordinal);

        foreach (var evt in events)
        {
            if (!contactByTargetId.TryGetValue(evt.TargetId, out var contact))
            {
                continue;
            }

            var previous = stateByTargetId.GetValueOrDefault(evt.TargetId, contact.LifecycleState);
            if (string.Equals(previous, BdaContactDamageStates.Lost, StringComparison.Ordinal))
            {
                continue;
            }

            if (BdaContactDamageStates.Rank(evt.NewState) <= BdaContactDamageStates.Rank(previous))
            {
                continue;
            }

            changes.Add(new ContactChangeRecord(
                evt.SequenceId,
                evt.SimTime,
                evt.SimTick,
                contact.ObserverId,
                contact.ContactId,
                contact.TargetId,
                previous,
                evt.NewState));

            stateByTargetId[evt.TargetId] = evt.NewState;
        }

        return changes;
    }

    public static IReadOnlyList<ContactChangeRecord> ProjectLostContacts(
        DecisionLog log,
        IReadOnlyDictionary<string, ContactPictureEntry> contactByTargetId) =>
        ProjectBdaContactChanges(log, contactByTargetId)
            .Where(c => string.Equals(c.NewState, BdaContactDamageStates.Lost, StringComparison.Ordinal))
            .ToArray();

    public static IReadOnlyList<ContactChangeRecord> ProjectLostContacts(
        IReadOnlyList<EngagementOutcomeRecord> outcomes,
        IReadOnlyDictionary<string, ContactPictureEntry> contactByTargetId) =>
        ProjectBdaContactChanges(
                Array.Empty<PlatformDamageChangeRecord>(),
                outcomes,
                contactByTargetId)
            .Where(c => string.Equals(c.NewState, BdaContactDamageStates.Lost, StringComparison.Ordinal))
            .ToArray();

    private static IEnumerable<BdaContactEvent> CollectPlatformDamageEvents(
        IReadOnlyList<PlatformDamageChangeRecord> platformDamageChanges,
        IReadOnlyDictionary<string, ContactPictureEntry> contactByTargetId)
    {
        foreach (var damage in platformDamageChanges
                     .OrderBy(d => d.SimTick)
                     .ThenBy(d => d.SequenceId))
        {
            var targetId = damage.UnitId.Value;
            if (!contactByTargetId.ContainsKey(targetId))
            {
                continue;
            }

            var newState = ResolvePlatformDamageContactState(damage);
            if (newState is null)
            {
                continue;
            }

            yield return new BdaContactEvent(
                damage.SequenceId,
                damage.SimTime,
                damage.SimTick,
                EngagementId: 0,
                targetId,
                newState);
        }
    }

    private static IEnumerable<BdaContactEvent> CollectKillOutcomeEvents(
        IReadOnlyList<EngagementOutcomeRecord> outcomes,
        IReadOnlyDictionary<string, ContactPictureEntry> contactByTargetId)
    {
        var damageOutcomes = outcomes
            .Select(o => new EngagementDamageOutcome(o.EngagementId, o.SequenceId, o.OutcomeCode))
            .Where(o => o.OutcomeCode == EngagementOutcomeCodes.Kill)
            .ToArray();

        foreach (var kill in DeterministicDamageApplyBatch.Sort(damageOutcomes))
        {
            var outcome = outcomes.First(o =>
                o.EngagementId == kill.EngagementId &&
                o.SequenceId == kill.SequenceId &&
                o.OutcomeCode == EngagementOutcomeCodes.Kill);

            var targetId = outcome.VictimTargetId.Value;
            if (!contactByTargetId.ContainsKey(targetId))
            {
                continue;
            }

            yield return new BdaContactEvent(
                outcome.SequenceId,
                outcome.SimTime,
                outcome.SimTick,
                kill.EngagementId,
                targetId,
                BdaContactDamageStates.Lost);
        }
    }

    private static string? ResolvePlatformDamageContactState(PlatformDamageChangeRecord damage)
    {
        if (damage.NewHpPct <= 0 ||
            string.Equals(damage.ReasonCode, PlatformDamageChangeReasonCodes.Kill, StringComparison.Ordinal))
        {
            return BdaContactDamageStates.Lost;
        }

        if (!string.Equals(damage.ReasonCode, PlatformDamageChangeReasonCodes.Hit, StringComparison.Ordinal))
        {
            return null;
        }

        return BdaContactDamageStates.MapDamageLevel(damage.DamageLevel);
    }

    private sealed record BdaContactEvent(
        ulong SequenceId,
        double SimTime,
        ulong SimTick,
        ulong EngagementId,
        string TargetId,
        string NewState);
}