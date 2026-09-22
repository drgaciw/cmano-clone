using ProjectAegis.Delegation.BdaAssess;
using ProjectAegis.Delegation.CombatEvents;
using ProjectAegis.Delegation.Decision;

namespace ProjectAegis.Delegation.UnityAdapter.Bridge;

/// <summary>One sim-time combat picture shared by map, history and detail surfaces (ADR-010).</summary>
public sealed record CombatPresentationFrame(
    CombatEventSnapshot Events, SliceAContactFrame Contacts, BdaAssessSnapshot Assessments, double SimTime)
{
    /// <summary>Correlated facts captured when the simulation processed the shot.</summary>
    public IReadOnlyList<CombatEngagementExplanation> Explanations { get; init; } = Array.Empty<CombatEngagementExplanation>();

    /// <summary>No combat facts received.</summary>
    public static CombatPresentationFrame Empty { get; } = new(
        CombatEventSnapshot.Empty, SliceAContactFrame.Empty, BdaAssessSnapshot.Empty, 0);

    /// <summary>
    /// Latest track/allocation fact for a shooter and correlation.
    /// Presentation read only — does not mutate the frame or the order log.
    /// </summary>
    public CombatEngagementExplanation? FindTrackAllocation(ulong correlationId, string shooterId) =>
        FindTrackAllocation(Explanations, correlationId, shooterId);

    /// <summary>
    /// Latest matching explanation. A later row with the same shooter and correlation replaces an earlier one.
    /// </summary>
    public static CombatEngagementExplanation? FindTrackAllocation(
        IReadOnlyList<CombatEngagementExplanation>? explanations,
        ulong correlationId,
        string shooterId)
    {
        if (shooterId is null)
        {
            throw new ArgumentNullException(nameof(shooterId));
        }

        if (explanations is null)
        {
            return null;
        }

        CombatEngagementExplanation? found = null;
        for (var i = 0; i < explanations.Count; i++)
        {
            var row = explanations[i];
            if (row is null)
            {
                continue;
            }

            if (row.CorrelationId == correlationId
                && string.Equals(row.ShooterId, shooterId, StringComparison.Ordinal))
            {
                found = row;
            }
        }

        return found;
    }
}

/// <summary>Known firing solution and salvo at execution; absent values stay unknown.</summary>
public sealed record CombatEngagementExplanation(ulong CorrelationId, string ShooterId,
    string TargetId, bool? HasFireControlTrack, int SalvoSize);

/// <summary>Builds the combat read model once per tick or explicit replay seek.</summary>
public static class CombatPresentationFrameBridge
{
    /// <summary>Uses only log facts at or before the requested sim time; never writes the supplied log.</summary>
    public static CombatPresentationFrame Build(DecisionLog log, SliceAContactFrame contacts, double simTime)
    {
        if (log == null) throw new ArgumentNullException(nameof(log));
        if (double.IsNaN(simTime) || double.IsInfinity(simTime) || simTime < 0)
            throw new ArgumentOutOfRangeException(nameof(simTime));
        // A replay may pass a complete log: BDA and contact projections must not see future records.
        var bounded = log;
        if (log.ChronologicalEntries().Any(e => e.SimTime > simTime))
        {
            bounded = new DecisionLog();
            foreach (var entry in log.ChronologicalEntries())
                if (entry.SimTime <= simTime) bounded.Append(entry);
        }
        var explanations = bounded.Engagements.Select(e => new CombatEngagementExplanation(
            e.SequenceId, e.ShooterTargetId.Value,
            e.VictimTargetId?.Value ?? CombatEventLogProjection.UnknownTargetId,
            e.HasFireControlTrack, e.SalvoSize)).ToArray();
        return new CombatPresentationFrame(CombatEventLogProjection.Build(bounded, simTime),
            contacts.SimTime > simTime || contacts.SimTick > simTime || contacts.Contacts.Any(c => c.LastSimTime > simTime)
                || contacts.KillChain.Contacts.Any(c => c.LastSimTime > simTime)
                || contacts.KillChain.Transitions.Any(c => c.SimTime > simTime)
                ? SliceAContactFrame.Empty : contacts,
            BdaAssessProjection.Project(bounded, (ulong)simTime), simTime)
        { Explanations = Array.AsReadOnly(explanations) };
    }
}
