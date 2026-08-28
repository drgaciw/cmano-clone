namespace ProjectAegis.Delegation.TrackCustody;

using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Scenario.Authoring;
using ProjectAegis.Delegation.Comms;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Sim.Scenario;

/// <summary>
/// DRG-222: folds kill-chain, provenance, comms, and sensor-to-shooter facts into a
/// replay-stable custody + drop-reason ledger. Sim/order-log truth only; no UI chrome.
/// </summary>
public static class TrackCustodyProjection
{
    public static TrackCustodySnapshot Project(
        DecisionLog? log,
        ulong currentSimTick,
        IKillChainFireControlSource? fireControl = null,
        ICatalogReader? catalog = null,
        ScenarioCommsDisplaySettings? commsDisplay = null,
        int staleThresholdTicks = KillChainContactStateProjection.DefaultStaleThresholdTicks,
        int dropThresholdTicks = KillChainContactStateProjection.DefaultDropThresholdTicks,
        IReadOnlyList<ScenarioOrbatUnitDto>? orbatUnits = null)
    {
        if (log is null)
        {
            return TrackCustodySnapshot.Empty;
        }

        var killChain = KillChainContactStateProjection.Project(
            log,
            currentSimTick,
            fireControl,
            staleThresholdTicks,
            dropThresholdTicks);
        if (killChain.Contacts.Count == 0)
        {
            return TrackCustodySnapshot.Empty;
        }

        var comms = CommsStateProjection.Project(log);
        var provenance = ContactProvenanceProjection.Project(
            log,
            currentSimTick,
            catalog,
            commsDisplay,
            staleThresholdTicks,
            orbatUnits);
        var provenanceByContact = provenance.Contacts.ToDictionary(c => c.ContactId, StringComparer.Ordinal);
        var activePictureIds = ContactPictureProjection.Project(log)
            .Select(c => c.ContactId)
            .ToHashSet(StringComparer.Ordinal);

        var rows = new TrackCustodyRow[killChain.Contacts.Count];
        for (var i = 0; i < killChain.Contacts.Count; i++)
        {
            var contact = killChain.Contacts[i];
            var prov = provenanceByContact.GetValueOrDefault(contact.ContactId);
            rows[i] = BuildRow(contact, prov, comms.State, activePictureIds);
        }

        Array.Sort(rows, CompareRows);
        var entries = BuildLedgerEntries(
            killChain.Transitions,
            comms.State,
            provenanceByContact,
            activePictureIds);
        return new TrackCustodySnapshot(rows, entries);
    }

    private static TrackCustodyRow BuildRow(
        KillChainContactState contact,
        ContactProvenanceState? provenance,
        CommsState commsState,
        HashSet<string> activePictureIds)
    {
        var custody = ResolveCustody(contact);
        var cause = ResolveCause(contact, provenance, commsState, custody, activePictureIds);
        return new TrackCustodyRow(
            contact.ContactId,
            contact.TargetId,
            contact.ObserverId,
            custody,
            cause,
            contact.LastSimTick,
            contact.LastSimTime,
            contact.CorrelationSequenceId);
    }

    private static TrackCustodyState ResolveCustody(KillChainContactState contact) =>
        contact.Loss == KillChainLossKind.Lost
            ? TrackCustodyState.Dropped
            : TrackCustodyState.Held;

    private static TrackCustodyCause ResolveCause(
        KillChainContactState contact,
        ContactProvenanceState? provenance,
        CommsState commsState,
        TrackCustodyState custody,
        HashSet<string> activePictureIds)
    {
        if (custody == TrackCustodyState.Dropped)
        {
            return ResolveDropCause(contact, provenance, commsState, activePictureIds);
        }

        if (contact.Loss == KillChainLossKind.Stale)
        {
            return TrackCustodyCause.Stale;
        }

        if (HasCommsDeniedBreak(provenance, commsState))
        {
            return TrackCustodyCause.CommsDenied;
        }

        return TrackCustodyCause.None;
    }

    private static TrackCustodyCause ResolveDropCause(
        KillChainContactState contact,
        ContactProvenanceState? provenance,
        CommsState commsState,
        HashSet<string> activePictureIds)
    {
        if (IsExplicitDrop(contact, activePictureIds))
        {
            return TrackCustodyCause.ExplicitDrop;
        }

        if (contact.Loss == KillChainLossKind.Lost)
        {
            return TrackCustodyCause.LostSensor;
        }

        if (HasCommsDeniedBreak(provenance, commsState))
        {
            return TrackCustodyCause.CommsDenied;
        }

        return TrackCustodyCause.Unknown;
    }

    /// <summary>
    /// Explicit lifecycle Lost removes the contact from the active picture; timeout drop keeps it.
    /// </summary>
    private static bool IsExplicitDrop(KillChainContactState contact, HashSet<string> activePictureIds) =>
        contact.Loss == KillChainLossKind.Lost && !activePictureIds.Contains(contact.ContactId);

    private static bool HasCommsDeniedBreak(ContactProvenanceState? provenance, CommsState commsState) =>
        commsState == CommsState.Denied
        && provenance is not null
        && (provenance.OutOfCommsUnknown
            || provenance.QualityState.HasFlag(ContactProvenanceQualityState.SilentComms));

    private static TrackCustodyLedgerEntry[] BuildLedgerEntries(
        IReadOnlyList<KillChainContactTransition> transitions,
        CommsState commsState,
        IReadOnlyDictionary<string, ContactProvenanceState> provenanceByContact,
        HashSet<string> activePictureIds)
    {
        if (transitions.Count == 0)
        {
            return Array.Empty<TrackCustodyLedgerEntry>();
        }

        var entries = new List<TrackCustodyLedgerEntry>(transitions.Count);
        for (var i = 0; i < transitions.Count; i++)
        {
            var transition = transitions[i];
            var entry = MapTransition(transition, commsState, provenanceByContact, activePictureIds);
            if (entry is not null)
            {
                entries.Add(entry);
            }
        }

        return entries.ToArray();
    }

    private static TrackCustodyLedgerEntry? MapTransition(
        KillChainContactTransition transition,
        CommsState commsState,
        IReadOnlyDictionary<string, ContactProvenanceState> provenanceByContact,
        HashSet<string> activePictureIds)
    {
        switch (transition.Kind)
        {
            case KillChainTransitionKind.Lost:
            {
                var contact = new KillChainContactState(
                    transition.ContactId,
                    transition.TargetId,
                    transition.ObserverId,
                    transition.NewPhase,
                    transition.Loss,
                    true,
                    false,
                    false,
                    false,
                    transition.SimTick,
                    transition.SimTime,
                    transition.SimTick,
                    transition.SimTime,
                    transition.CorrelationSequenceId,
                    Array.Empty<ulong>(),
                    transition.SourceRefs);
                var prov = provenanceByContact.GetValueOrDefault(contact.ContactId);
                var cause = ResolveDropCause(contact, prov, commsState, activePictureIds);
                return new TrackCustodyLedgerEntry(
                    transition.ContactId,
                    transition.TargetId,
                    transition.ObserverId,
                    TrackCustodyState.Dropped,
                    cause,
                    transition.SimTick,
                    transition.SimTime,
                    transition.CorrelationSequenceId);
            }

            case KillChainTransitionKind.Degraded when transition.Loss == KillChainLossKind.Stale:
                return new TrackCustodyLedgerEntry(
                    transition.ContactId,
                    transition.TargetId,
                    transition.ObserverId,
                    TrackCustodyState.Held,
                    TrackCustodyCause.Stale,
                    transition.SimTick,
                    transition.SimTime,
                    transition.CorrelationSequenceId);

            default:
                return null;
        }
    }

    private static int CompareRows(TrackCustodyRow? left, TrackCustodyRow? right)
    {
        if (left is null && right is null)
        {
            return 0;
        }

        if (left is null)
        {
            return -1;
        }

        if (right is null)
        {
            return 1;
        }

        return string.Compare(left.ContactId, right.ContactId, StringComparison.Ordinal);
    }
}
