namespace ProjectAegis.Delegation.Projection;

using Data.Catalog;
using Data.Scenario.Authoring;
using Comms;
using Core;
using Decision;
using ProjectAegis.Sim.Scenario;

/// <summary>
/// DRG-271 headless cues for stale contacts, datalink-sourced contacts, and queued orders.
/// Reuses <see cref="ContactProvenanceProjection"/> for staleness, sim age, confidence, and
/// comms-quality flags. Does not render icons or badges and does not touch the tick path.
/// </summary>
public static class ContactCueProjection
{
    /// <summary>Label applied when fixture geometry omits one.</summary>
    public const string FixtureOnlyLabel = "fixture-only";

    /// <summary>
    /// Label used when a sample claims the DRG-417 production feed and this slice has not wired it.
    /// </summary>
    public const string ProductionFeedUnavailableLabel = "fixture-only:production-feed-unavailable";

    /// <summary>DRG-417 production position feed is not wired in this headless slice.</summary>
    public const bool ProductionPositionFeedWired = false;

    /// <summary>
    /// Identity confidence never yields a position or a numeric uncertainty ellipse (DRG-411).
    /// Every confidence, including <see cref="ContactProvenanceConfidence.Unknown"/>, returns
    /// <see cref="ContactPositionClaim.Unknown"/> — null coordinates, not a radius of zero.
    /// </summary>
    public static ContactPositionClaim PositionFromIdentityConfidence(ContactProvenanceConfidence confidence)
    {
        _ = confidence;
        return ContactPositionClaim.Unknown;
    }

    public static ContactCueSnapshot Project(
        DecisionLog? log,
        ulong currentSimTick,
        double? currentSimTime = null,
        IReadOnlyList<ContactOriginFact>? origins = null,
        IReadOnlyList<ContactPositionSample>? positions = null,
        IReadOnlyList<UnitOrderTerminalFact>? terminalOrders = null,
        bool productionPositionFeedWired = ProductionPositionFeedWired,
        ICatalogReader? catalog = null,
        ScenarioCommsDisplaySettings? commsDisplay = null,
        int staleThresholdTicks = ContactProvenanceProjection.DefaultStaleThresholdTicks,
        IReadOnlyList<ScenarioOrbatUnitDto>? orbatUnits = null)
    {
        var provenance = ContactProvenanceProjection.Project(
            log,
            currentSimTick,
            catalog,
            commsDisplay,
            staleThresholdTicks,
            orbatUnits);
        var lossTick = ResolveCommsLossTick(log);
        var contacts = ProjectContacts(
            provenance,
            currentSimTick,
            currentSimTime,
            origins,
            positions,
            lossTick,
            productionPositionFeedWired);
        var units = ProjectUnits(log, currentSimTick, terminalOrders);
        if (contacts.Count == 0 && units.Count == 0)
        {
            return ContactCueSnapshot.Empty;
        }

        return new ContactCueSnapshot(contacts, units);
    }

    /// <summary>
    /// Returns only the named contact and unit. Misses and disagreeing duplicates are null.
    /// The projection stores nothing, so a later call cannot reuse a previous selection.
    /// </summary>
    public static ContactCueSelection Select(
        ContactCueSnapshot? snapshot,
        string? contactId,
        string? unitId)
    {
        var contact = FindContact(snapshot, contactId);
        var unit = FindUnit(snapshot, unitId);
        return new ContactCueSelection(contact?.ContactId, unit?.UnitId, contact, unit);
    }

    private static IReadOnlyList<ContactCueState> ProjectContacts(
        ContactProvenanceSnapshot provenance,
        ulong currentSimTick,
        double? currentSimTime,
        IReadOnlyList<ContactOriginFact>? origins,
        IReadOnlyList<ContactPositionSample>? positions,
        ulong? commsLossTick,
        bool productionPositionFeedWired)
    {
        if (provenance.Contacts.Count == 0)
        {
            return Array.Empty<ContactCueState>();
        }

        var originByContact = IndexOrigins(origins);
        var rows = new ContactCueState[provenance.Contacts.Count];
        for (var i = 0; i < provenance.Contacts.Count; i++)
        {
            rows[i] = ProjectContact(
                provenance.Contacts[i],
                currentSimTick,
                currentSimTime,
                originByContact,
                positions,
                commsLossTick,
                productionPositionFeedWired);
        }

        return rows;
    }

    private static ContactCueState ProjectContact(
        ContactProvenanceState row,
        ulong currentSimTick,
        double? currentSimTime,
        IReadOnlyDictionary<string, ContactInformationOrigin?> originByContact,
        IReadOnlyList<ContactPositionSample>? positions,
        ulong? commsLossTick,
        bool productionPositionFeedWired)
    {
        var comms = CommsFromProvenance(row);
        var origin = ResolveOrigin(originByContact, row.ContactId);
        var position = ResolvePosition(
            row.ContactId,
            positions,
            currentSimTick,
            comms == CommsState.Denied ? commsLossTick : null,
            comms,
            productionPositionFeedWired);
        return new ContactCueState(
            row.ContactId,
            row.Confidence,
            row.Freshness,
            SimAgeTicks(currentSimTick, row.LastKnown.LastSimTick),
            AgeSimTime(currentSimTime, row.LastKnown.LastSimTime),
            comms,
            row.OutOfCommsUnknown,
            row.QualityState,
            origin,
            origin == ContactInformationOrigin.Datalink,
            row.Freshness == ContactProvenanceFreshness.Stale,
            position);
    }

    private static ulong? SimAgeTicks(ulong currentSimTick, ulong lastSimTick) =>
        currentSimTick >= lastSimTick ? currentSimTick - lastSimTick : null;

    private static double? AgeSimTime(double? currentSimTime, double lastSimTime)
    {
        if (currentSimTime is not { } now || !double.IsFinite(now) || !double.IsFinite(lastSimTime))
        {
            return null;
        }

        if (now < lastSimTime)
        {
            return null;
        }

        return now - lastSimTime;
    }

    private static CommsState CommsFromProvenance(ContactProvenanceState row)
    {
        if (row.OutOfCommsUnknown)
        {
            return CommsState.Denied;
        }

        if (row.QualityState.HasFlag(ContactProvenanceQualityState.SilentComms))
        {
            return CommsState.Degraded;
        }

        return CommsState.Nominal;
    }

    private static ulong? ResolveCommsLossTick(DecisionLog? log)
    {
        if (log is null)
        {
            return null;
        }

        ulong? lossTick = null;
        foreach (var change in log.CommsStateChanges.OrderBy(change => change.SequenceId))
        {
            if (change.NewState == CommsState.Denied)
            {
                lossTick = change.SimTick;
            }
            else
            {
                lossTick = null;
            }
        }

        return lossTick;
    }

    private static Dictionary<string, ContactInformationOrigin?> IndexOrigins(
        IReadOnlyList<ContactOriginFact>? origins)
    {
        var map = new Dictionary<string, ContactInformationOrigin?>(StringComparer.Ordinal);
        if (origins is null)
        {
            return map;
        }

        for (var i = 0; i < origins.Count; i++)
        {
            var fact = origins[i];
            if (string.IsNullOrEmpty(fact.ContactId))
            {
                continue;
            }

            if (!map.TryGetValue(fact.ContactId, out var existing))
            {
                map[fact.ContactId] = fact.Origin;
                continue;
            }

            if (existing != fact.Origin)
            {
                map[fact.ContactId] = null;
            }
        }

        return map;
    }

    private static ContactInformationOrigin ResolveOrigin(
        IReadOnlyDictionary<string, ContactInformationOrigin?> originByContact,
        string contactId)
    {
        if (!originByContact.TryGetValue(contactId, out var origin) || origin is null)
        {
            return ContactInformationOrigin.Unknown;
        }

        return origin.Value;
    }

    private static ContactPositionClaim ResolvePosition(
        string contactId,
        IReadOnlyList<ContactPositionSample>? samples,
        ulong currentSimTick,
        ulong? commsLossTick,
        CommsState comms,
        bool productionPositionFeedWired)
    {
        if (samples is null || samples.Count == 0)
        {
            return ContactPositionClaim.Unknown;
        }

        ContactPositionSample? chosen = null;
        var conflict = false;
        for (var i = 0; i < samples.Count; i++)
        {
            var sample = samples[i];
            if (!string.Equals(sample.ContactId, contactId, StringComparison.Ordinal)
                || sample.ReportedSimTick > currentSimTick)
            {
                continue;
            }

            if (commsLossTick is { } loss && sample.ReportedSimTick > loss)
            {
                continue;
            }

            if (chosen is null || sample.ReportedSimTick > chosen.ReportedSimTick)
            {
                chosen = sample;
                conflict = false;
                continue;
            }

            if (sample.ReportedSimTick == chosen.ReportedSimTick && !SameFix(sample, chosen))
            {
                conflict = true;
            }
        }

        if (chosen is null || conflict || !TryCoordinates(chosen, out var east, out var north))
        {
            return ContactPositionClaim.Unknown;
        }

        if (chosen.IsProductionFeed && productionPositionFeedWired)
        {
            return new ContactPositionClaim(
                ContactPositionClaimKind.LastReportedHeld,
                east,
                north,
                chosen.ReportedSimTick,
                null,
                comms == CommsState.Nominal && chosen.ReportedSimTick == currentSimTick);
        }

        var label = chosen.IsProductionFeed
            ? ProductionFeedUnavailableLabel
            : string.IsNullOrWhiteSpace(chosen.FixtureLabel) ? FixtureOnlyLabel : chosen.FixtureLabel.Trim();
        return new ContactPositionClaim(
            ContactPositionClaimKind.FixtureOnly,
            east,
            north,
            chosen.ReportedSimTick,
            label,
            false);
    }

    private static bool SameFix(ContactPositionSample left, ContactPositionSample right) =>
        Nullable.Equals(left.EastMeters, right.EastMeters)
        && Nullable.Equals(left.NorthMeters, right.NorthMeters)
        && left.IsProductionFeed == right.IsProductionFeed
        && string.Equals(left.FixtureLabel, right.FixtureLabel, StringComparison.Ordinal);

    private static bool TryCoordinates(ContactPositionSample sample, out double east, out double north)
    {
        east = 0;
        north = 0;
        if (sample.EastMeters is not { } eastValue
            || sample.NorthMeters is not { } northValue
            || !double.IsFinite(eastValue)
            || !double.IsFinite(northValue))
        {
            return false;
        }

        east = eastValue;
        north = northValue;
        return true;
    }

    private static IReadOnlyList<UnitOrderQueueState> ProjectUnits(
        DecisionLog? log,
        ulong currentSimTick,
        IReadOnlyList<UnitOrderTerminalFact>? terminalOrders)
    {
        var playerOrders = log?.PlayerOrders ?? Array.Empty<PlayerOrderRecord>();
        var denials = log?.PolicyDenials ?? Array.Empty<PolicyDenialRecord>();
        var engagements = log?.Engagements ?? Array.Empty<EngagementRecord>();
        var playerUnits = new HashSet<string>(StringComparer.Ordinal);
        var unitIds = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        void AddUnit(string? unitId)
        {
            if (string.IsNullOrEmpty(unitId) || !seen.Add(unitId))
            {
                return;
            }

            unitIds.Add(unitId);
        }

        for (var i = 0; i < playerOrders.Count; i++)
        {
            var order = playerOrders[i];
            if (order.SimTick > currentSimTick || string.IsNullOrEmpty(order.UnitId.Value))
            {
                continue;
            }

            playerUnits.Add(order.UnitId.Value);
            AddUnit(order.UnitId.Value);
        }

        if (terminalOrders is not null)
        {
            for (var i = 0; i < terminalOrders.Count; i++)
            {
                var fact = terminalOrders[i];
                if (fact.SimTick > currentSimTick || !IsTerminal(fact.Disposition))
                {
                    continue;
                }

                AddUnit(fact.UnitId);
            }
        }

        for (var i = 0; i < denials.Count; i++)
        {
            var denial = denials[i];
            if (denial.SimTick > currentSimTick)
            {
                continue;
            }

            AddUnit(ResolveDenialUnit(denial, playerUnits));
        }

        for (var i = 0; i < engagements.Count; i++)
        {
            var engagement = engagements[i];
            if (!engagement.Launched || engagement.SimTick > currentSimTick)
            {
                continue;
            }

            AddUnit(engagement.ShooterTargetId.Value);
        }

        if (unitIds.Count == 0)
        {
            return Array.Empty<UnitOrderQueueState>();
        }

        unitIds.Sort(StringComparer.Ordinal);
        var rows = new UnitOrderQueueState[unitIds.Count];
        for (var i = 0; i < unitIds.Count; i++)
        {
            rows[i] = ResolveUnit(
                unitIds[i],
                currentSimTick,
                playerOrders,
                denials,
                engagements,
                terminalOrders,
                playerUnits);
        }

        return rows;
    }

    private static UnitOrderQueueState ResolveUnit(
        string unitId,
        ulong currentSimTick,
        IReadOnlyList<PlayerOrderRecord> playerOrders,
        IReadOnlyList<PolicyDenialRecord> denials,
        IReadOnlyList<EngagementRecord> engagements,
        IReadOnlyList<UnitOrderTerminalFact>? terminalOrders,
        HashSet<string> playerUnits)
    {
        var order = LatestPlayerOrder(unitId, currentSimTick, playerOrders);
        if (order is null)
        {
            return ResolveWithoutPlayerOrder(
                unitId,
                currentSimTick,
                denials,
                engagements,
                terminalOrders,
                playerUnits);
        }

        TerminalMark? terminal = null;
        for (var i = 0; i < denials.Count; i++)
        {
            var denial = denials[i];
            if (!DenialMatches(denial, unitId, order.Kind, order.SimTick, currentSimTick))
            {
                continue;
            }

            terminal = Prefer(terminal, denial.SimTick, denial.SequenceId, UnitOrderDisposition.Rejected);
        }

        if (order.Kind == OrderKind.Engage)
        {
            for (var i = 0; i < engagements.Count; i++)
            {
                var engagement = engagements[i];
                if (!engagement.Launched
                    || engagement.SimTick < order.SimTick
                    || engagement.SimTick > currentSimTick
                    || !string.Equals(engagement.ShooterTargetId.Value, unitId, StringComparison.Ordinal))
                {
                    continue;
                }

                terminal = Prefer(
                    terminal,
                    engagement.SimTick,
                    engagement.SequenceId,
                    UnitOrderDisposition.Executed);
            }
        }

        if (terminalOrders is not null)
        {
            for (var i = 0; i < terminalOrders.Count; i++)
            {
                var fact = terminalOrders[i];
                if (!IsTerminal(fact.Disposition)
                    || fact.SimTick < order.SimTick
                    || fact.SimTick > currentSimTick
                    || !string.Equals(fact.UnitId, unitId, StringComparison.Ordinal)
                    || (fact.Kind is { } kind && kind != order.Kind))
                {
                    continue;
                }

                terminal = Prefer(terminal, fact.SimTick, FactTie(i), fact.Disposition);
            }
        }

        var disposition = terminal is { } mark
            ? mark.Disposition
            : order.ResolvedExecuteSimTick > currentSimTick
                ? UnitOrderDisposition.Delayed
                : UnitOrderDisposition.Queued;
        return new UnitOrderQueueState(
            unitId,
            disposition,
            order.Kind,
            order.SimTick,
            order.ResolvedExecuteSimTick,
            disposition == UnitOrderDisposition.Queued);
    }

    private static UnitOrderQueueState ResolveWithoutPlayerOrder(
        string unitId,
        ulong currentSimTick,
        IReadOnlyList<PolicyDenialRecord> denials,
        IReadOnlyList<EngagementRecord> engagements,
        IReadOnlyList<UnitOrderTerminalFact>? terminalOrders,
        HashSet<string> playerUnits)
    {
        TerminalMark? terminal = null;
        OrderKind? kind = null;
        ulong? issued = null;

        for (var i = 0; i < denials.Count; i++)
        {
            var denial = denials[i];
            if (denial.SimTick > currentSimTick
                || !string.Equals(ResolveDenialUnit(denial, playerUnits), unitId, StringComparison.Ordinal))
            {
                continue;
            }

            var next = Prefer(terminal, denial.SimTick, denial.SequenceId, UnitOrderDisposition.Rejected);
            if (!SameMark(terminal, next))
            {
                terminal = next;
                kind = denial.AttemptedKind;
                issued = denial.SimTick;
            }
        }

        for (var i = 0; i < engagements.Count; i++)
        {
            var engagement = engagements[i];
            if (!engagement.Launched
                || engagement.SimTick > currentSimTick
                || !string.Equals(engagement.ShooterTargetId.Value, unitId, StringComparison.Ordinal))
            {
                continue;
            }

            var next = Prefer(terminal, engagement.SimTick, engagement.SequenceId, UnitOrderDisposition.Executed);
            if (!SameMark(terminal, next))
            {
                terminal = next;
                kind = OrderKind.Engage;
                issued = engagement.SimTick;
            }
        }

        if (terminalOrders is not null)
        {
            for (var i = 0; i < terminalOrders.Count; i++)
            {
                var fact = terminalOrders[i];
                if (!IsTerminal(fact.Disposition)
                    || fact.SimTick > currentSimTick
                    || !string.Equals(fact.UnitId, unitId, StringComparison.Ordinal))
                {
                    continue;
                }

                var next = Prefer(terminal, fact.SimTick, FactTie(i), fact.Disposition);
                if (!SameMark(terminal, next))
                {
                    terminal = next;
                    kind = fact.Kind;
                    issued = fact.SimTick;
                }
            }
        }

        var disposition = terminal?.Disposition ?? UnitOrderDisposition.None;
        return new UnitOrderQueueState(
            unitId,
            disposition,
            kind,
            issued,
            null,
            false);
    }

    private static PlayerOrderRecord? LatestPlayerOrder(
        string unitId,
        ulong currentSimTick,
        IReadOnlyList<PlayerOrderRecord> playerOrders)
    {
        PlayerOrderRecord? latest = null;
        for (var i = 0; i < playerOrders.Count; i++)
        {
            var order = playerOrders[i];
            if (order.SimTick > currentSimTick
                || !string.Equals(order.UnitId.Value, unitId, StringComparison.Ordinal))
            {
                continue;
            }

            if (latest is null
                || order.SimTick > latest.SimTick
                || (order.SimTick == latest.SimTick && order.SequenceId > latest.SequenceId))
            {
                latest = order;
            }
        }

        return latest;
    }

    private static bool DenialMatches(
        PolicyDenialRecord denial,
        string unitId,
        OrderKind kind,
        ulong orderTick,
        ulong currentSimTick)
    {
        if (denial.AttemptedKind != kind
            || denial.SimTick < orderTick
            || denial.SimTick > currentSimTick)
        {
            return false;
        }

        return string.Equals(denial.TargetId.Value, unitId, StringComparison.Ordinal)
            || string.Equals(denial.AgentId.Value, unitId, StringComparison.Ordinal);
    }

    private static string ResolveDenialUnit(PolicyDenialRecord denial, HashSet<string> playerUnits)
    {
        var target = denial.TargetId.Value ?? string.Empty;
        var agent = denial.AgentId.Value ?? string.Empty;
        if (target.Length > 0 && playerUnits.Contains(target))
        {
            return target;
        }

        if (agent.Length > 0 && playerUnits.Contains(agent))
        {
            return agent;
        }

        return target.Length > 0 ? target : agent;
    }

    private static bool IsTerminal(UnitOrderDisposition disposition) =>
        disposition is UnitOrderDisposition.Rejected
            or UnitOrderDisposition.Expired
            or UnitOrderDisposition.Executed;

    private static ulong FactTie(int index) => (1UL << 32) + (ulong)index;

    private readonly record struct TerminalMark(ulong Tick, ulong Tie, UnitOrderDisposition Disposition);

    private static TerminalMark Prefer(
        TerminalMark? current,
        ulong tick,
        ulong tie,
        UnitOrderDisposition disposition)
    {
        if (current is not { } mark || tick > mark.Tick || (tick == mark.Tick && tie > mark.Tie))
        {
            return new TerminalMark(tick, tie, disposition);
        }

        return mark;
    }

    private static bool SameMark(TerminalMark? current, TerminalMark next) =>
        current is { } mark
        && mark.Tick == next.Tick
        && mark.Tie == next.Tie
        && mark.Disposition == next.Disposition;

    private static ContactCueState? FindContact(ContactCueSnapshot? snapshot, string? contactId)
    {
        if (snapshot is null || string.IsNullOrEmpty(contactId))
        {
            return null;
        }

        ContactCueState? found = null;
        for (var i = 0; i < snapshot.Contacts.Count; i++)
        {
            var row = snapshot.Contacts[i];
            if (!string.Equals(row.ContactId, contactId, StringComparison.Ordinal))
            {
                continue;
            }

            if (found is null)
            {
                found = row;
                continue;
            }

            if (!found.Equals(row))
            {
                return null;
            }
        }

        return found;
    }

    private static UnitOrderQueueState? FindUnit(ContactCueSnapshot? snapshot, string? unitId)
    {
        if (snapshot is null || string.IsNullOrEmpty(unitId))
        {
            return null;
        }

        UnitOrderQueueState? found = null;
        for (var i = 0; i < snapshot.UnitOrders.Count; i++)
        {
            var row = snapshot.UnitOrders[i];
            if (!string.Equals(row.UnitId, unitId, StringComparison.Ordinal))
            {
                continue;
            }

            if (found is null)
            {
                found = row;
                continue;
            }

            if (!found.Equals(row))
            {
                return null;
            }
        }

        return found;
    }
}
