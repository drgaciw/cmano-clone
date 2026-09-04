namespace ProjectAegis.Delegation.TrackCustody;

using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Scenario.Authoring;
using ProjectAegis.Delegation.Comms;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Sim.Catalog;
using ProjectAegis.Sim.Engage;
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
            rows[i] = BuildRow(contact, prov, comms.State, activePictureIds, log);
        }

        Array.Sort(rows, CompareRows);
        var entries = BuildLedgerEntries(
            killChain.Transitions,
            comms.State,
            provenanceByContact,
            activePictureIds,
            log);
        return new TrackCustodySnapshot(rows, entries);
    }

    private static TrackCustodyRow BuildRow(
        KillChainContactState contact,
        ContactProvenanceState? provenance,
        CommsState commsState,
        HashSet<string> activePictureIds,
        DecisionLog? log)
    {
        var custody = ResolveCustody(contact);
        var cause = ResolveCause(contact, provenance, commsState, custody, activePictureIds);
        var breakdown = ResolveBreakdown(contact, provenance, commsState, custody, cause, log);
        return new TrackCustodyRow(
            contact.ContactId,
            contact.TargetId,
            contact.ObserverId,
            custody,
            cause,
            contact.LastSimTick,
            contact.LastSimTime,
            contact.CorrelationSequenceId,
            breakdown);
    }

    /// <summary>
    /// AEGIS-305 (DRG-235): Explicitly attributes why track custody was lost or degraded.
    /// </summary>
    public static TrackCustodyBreakdown ResolveBreakdown(
        KillChainContactState contact,
        ContactProvenanceState? provenance,
        CommsState commsState,
        TrackCustodyState custody,
        TrackCustodyCause cause,
        DecisionLog? log = null)
    {
        if (IsPlatformDestroyed(contact, log))
        {
            return TrackCustodyBreakdown.PlatformDestroyed;
        }

        if (IsJammingDegraded(contact, provenance, commsState, cause, log))
        {
            return TrackCustodyBreakdown.JammingDegraded;
        }

        if (custody == TrackCustodyState.Dropped
            || cause is TrackCustodyCause.LostSensor or TrackCustodyCause.Stale or TrackCustodyCause.ExplicitDrop
            || contact.Loss is KillChainLossKind.Lost or KillChainLossKind.Stale
            || !contact.DetectionCaptured
            || !contact.TrackContinuous)
        {
            return TrackCustodyBreakdown.LineOfSightLoss;
        }

        return TrackCustodyBreakdown.None;
    }

    private static bool IsPlatformDestroyed(KillChainContactState contact, DecisionLog? log)
    {
        if (log is not null)
        {
            if (log.PlatformDamageChanges.Any(d =>
                (string.Equals(d.UnitId.Value, contact.ObserverId, StringComparison.Ordinal) ||
                 string.Equals(d.UnitId.Value, contact.TargetId, StringComparison.Ordinal)) &&
                (d.NewHpPct <= 0 ||
                 string.Equals(d.ReasonCode, PlatformDamageChangeReasonCodes.Kill, StringComparison.Ordinal) ||
                 string.Equals(d.ReasonCode, "Kill", StringComparison.OrdinalIgnoreCase))))
            {
                return true;
            }

            if (log.EngagementOutcomes.Any(o =>
                (string.Equals(o.VictimTargetId.Value, contact.ObserverId, StringComparison.Ordinal) ||
                 string.Equals(o.VictimTargetId.Value, contact.TargetId, StringComparison.Ordinal)) &&
                string.Equals(o.OutcomeCode, EngagementOutcomeCodes.Kill, StringComparison.Ordinal)))
            {
                return true;
            }
        }

        if (contact.SourceRefs != null && contact.SourceRefs.Any(s =>
            s.Contains("destroy", StringComparison.OrdinalIgnoreCase) ||
            s.Contains("kill", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        return false;
    }

    private static bool IsJammingDegraded(
        KillChainContactState contact,
        ContactProvenanceState? provenance,
        CommsState commsState,
        TrackCustodyCause cause,
        DecisionLog? log)
    {
        if (cause == TrackCustodyCause.CommsDenied)
        {
            return true;
        }

        if (HasCommsDeniedBreak(provenance, commsState))
        {
            return true;
        }

        if (commsState == CommsState.Denied)
        {
            return true;
        }

        if (provenance is not null &&
            (provenance.OutOfCommsUnknown ||
             provenance.QualityState.HasFlag(ContactProvenanceQualityState.SilentComms)))
        {
            return true;
        }

        if (log is not null)
        {
            if (log.CommsStateChanges.Any(c =>
                c.NewState != CommsState.Nominal &&
                (!string.IsNullOrEmpty(c.Reason) &&
                 (c.Reason.Contains("jam", StringComparison.OrdinalIgnoreCase) ||
                  c.Reason.Contains("sj", StringComparison.OrdinalIgnoreCase) ||
                  c.Reason.Contains("denied", StringComparison.OrdinalIgnoreCase)))))
            {
                return true;
            }

            if (log.EventFired.Any(e =>
                (!string.IsNullOrEmpty(e.EventCode) && e.EventCode.Contains("jam", StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(e.EventId) && e.EventId.Contains("jam", StringComparison.OrdinalIgnoreCase))))
            {
                return true;
            }
        }

        if (contact.SourceRefs != null && contact.SourceRefs.Any(s =>
            s.Contains("jam", StringComparison.OrdinalIgnoreCase) ||
            s.Contains("sj", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        return false;
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
        HashSet<string> activePictureIds,
        DecisionLog? log)
    {
        if (transitions.Count == 0)
        {
            return Array.Empty<TrackCustodyLedgerEntry>();
        }

        var entries = new List<TrackCustodyLedgerEntry>(transitions.Count);
        for (var i = 0; i < transitions.Count; i++)
        {
            var transition = transitions[i];
            var entry = MapTransition(transition, commsState, provenanceByContact, activePictureIds, log);
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
        HashSet<string> activePictureIds,
        DecisionLog? log)
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
                var breakdown = ResolveBreakdown(contact, prov, commsState, TrackCustodyState.Dropped, cause, log);
                return new TrackCustodyLedgerEntry(
                    transition.ContactId,
                    transition.TargetId,
                    transition.ObserverId,
                    TrackCustodyState.Dropped,
                    cause,
                    transition.SimTick,
                    transition.SimTime,
                    transition.CorrelationSequenceId,
                    breakdown);
            }

            case KillChainTransitionKind.Degraded when transition.Loss == KillChainLossKind.Stale:
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
                var breakdown = ResolveBreakdown(contact, prov, commsState, TrackCustodyState.Held, TrackCustodyCause.Stale, log);
                return new TrackCustodyLedgerEntry(
                    transition.ContactId,
                    transition.TargetId,
                    transition.ObserverId,
                    TrackCustodyState.Held,
                    TrackCustodyCause.Stale,
                    transition.SimTick,
                    transition.SimTime,
                    transition.CorrelationSequenceId,
                    breakdown);
            }

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
