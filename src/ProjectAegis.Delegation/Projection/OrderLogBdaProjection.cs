namespace ProjectAegis.Delegation.Projection;

using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Sim.Engage;

/// <summary>Order-log-only BDA: sorted kill outcomes emit contact Lost rows (ADR-009 / TR-combat-dom-003).</summary>
public static class OrderLogBdaProjection
{
    public static IReadOnlyList<ContactChangeRecord> ProjectLostContacts(
        DecisionLog log,
        IReadOnlyDictionary<string, ContactPictureEntry> contactByTargetId) =>
        ProjectLostContacts(log.EngagementOutcomes, contactByTargetId);

    public static IReadOnlyList<ContactChangeRecord> ProjectLostContacts(
        IReadOnlyList<EngagementOutcomeRecord> outcomes,
        IReadOnlyDictionary<string, ContactPictureEntry> contactByTargetId)
    {
        var damageOutcomes = outcomes
            .Select(o => new EngagementDamageOutcome(o.EngagementId, o.SequenceId, o.OutcomeCode))
            .Where(o => o.OutcomeCode == EngagementOutcomeCodes.Kill)
            .ToArray();

        var sortedKills = DeterministicDamageApplyBatch.Sort(damageOutcomes);
        var lost = new List<ContactChangeRecord>(sortedKills.Count);
        foreach (var kill in sortedKills)
        {
            var outcome = outcomes.First(o =>
                o.EngagementId == kill.EngagementId &&
                o.SequenceId == kill.SequenceId &&
                o.OutcomeCode == EngagementOutcomeCodes.Kill);

            var targetId = outcome.VictimTargetId.Value;
            if (!contactByTargetId.TryGetValue(targetId, out var contact))
            {
                continue;
            }

            lost.Add(new ContactChangeRecord(
                outcome.SequenceId,
                outcome.SimTime,
                outcome.SimTick,
                contact.ObserverId,
                contact.ContactId,
                contact.TargetId,
                contact.LifecycleState,
                "Lost"));
        }

        return lost;
    }
}