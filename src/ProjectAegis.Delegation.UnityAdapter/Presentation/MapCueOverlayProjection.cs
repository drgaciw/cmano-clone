using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Bridge;

namespace ProjectAegis.Delegation.UnityAdapter.Presentation;

/// <summary>
/// DRG-712 ([C2-17 UI]): pure headless projection folding <see cref="ContactCueProjection"/>
/// (stale / datalink / DRG-417 fixture position claim) and <see cref="UnitOrderQueueState.IsGenuinelyPending"/>
/// into thin UI-ready overlay rows and zero-leak selection banners (ADR-010 §2–3, ADR-007).
/// </summary>
public static class MapCueOverlayProjection
{
    public const string RootElementName = "map-cue-overlay-root";
    public const string SelectedStripElementName = "map-cue-selected-strip";
    public const string SelectedContactBadgeElementName = "map-cue-contact-badge";
    public const string SelectedContactFixtureElementName = "map-cue-contact-fixture";
    public const string SelectedUnitOrderBadgeElementName = "map-cue-unit-order-badge";
    public const string CanvasElementName = "map-cue-canvas";

    public const string StaleTokenText = "STALE";
    public const string DatalinkTokenText = "DL";
    public const string QueuedBadgeText = "QUEUED";

    public const string StaleCssClass = "map-cue-icon--stale";
    public const string DatalinkCssClass = "map-cue-icon--datalink";
    public const string QueuedCssClass = "map-cue-badge--queued";
    public const string FixtureTagCssClass = "map-cue-fixture-tag";

    public static string SymbolElementName(string symbolId) => $"map-cue-symbol-{symbolId}";

    public static MapCueOverlayState Project(
        DelegationBridge? bridge,
        IReadOnlyList<MapSymbolEntry>? symbols,
        ulong currentSimTick,
        string? selectedContactId,
        string? selectedUnitId,
        bool isInspectingHistory = false,
        double? currentSimTime = null,
        IReadOnlyList<ContactOriginFact>? origins = null,
        IReadOnlyList<ContactPositionSample>? positions = null,
        IReadOnlyList<UnitOrderTerminalFact>? terminalOrders = null,
        bool productionPositionFeedWired = ContactCueProjection.ProductionPositionFeedWired,
        int staleThresholdTicks = ContactProvenanceProjection.DefaultStaleThresholdTicks)
    {
        if (isInspectingHistory)
        {
            return new MapCueOverlayState(
                Array.Empty<ContactCueState>(),
                Array.Empty<UnitOrderQueueState>(),
                Array.Empty<MapSymbolCueRow>(),
                SelectedContactCueState.Empty,
                SelectedUnitOrderCueState.Empty,
                IsInspectingHistory: true);
        }

        if (bridge is null)
        {
            return MapCueOverlayState.Empty;
        }

        var log = bridge.Orchestrator.DecisionLog;
        var commsDisplay = bridge.Orchestrator.ScenarioPolicy?.CommsDisplay;
        var catalog = bridge.Session?.CatalogReader;
        var mergedOrigins = MergeDatalinkOrigins(log, origins);

        var cueSnapshot = ContactCueProjection.Project(
            log,
            currentSimTick,
            currentSimTime,
            mergedOrigins,
            positions,
            terminalOrders,
            productionPositionFeedWired,
            catalog,
            commsDisplay,
            staleThresholdTicks);

        var selection = ContactCueProjection.Select(cueSnapshot, selectedContactId, selectedUnitId);
        var selectedContactCue = BuildSelectedContactCue(selection.Contact);
        var selectedUnitOrderCue = BuildSelectedUnitOrderCue(selection.UnitOrder);
        var rows = BuildSymbolRows(
            symbols,
            cueSnapshot,
            selectedContactId,
            selectedUnitId);

        return new MapCueOverlayState(
            cueSnapshot.Contacts,
            cueSnapshot.UnitOrders,
            rows,
            selectedContactCue,
            selectedUnitOrderCue,
            IsInspectingHistory: false);
    }

    private static IReadOnlyList<ContactOriginFact>? MergeDatalinkOrigins(
        DecisionLog log,
        IReadOnlyList<ContactOriginFact>? explicitOrigins)
    {
        var merged = new List<ContactOriginFact>();
        var explicitIds = new HashSet<string>(StringComparer.Ordinal);
        if (explicitOrigins is not null)
        {
            for (var i = 0; i < explicitOrigins.Count; i++)
            {
                var fact = explicitOrigins[i];
                merged.Add(fact);
                if (!string.IsNullOrEmpty(fact.ContactId))
                {
                    explicitIds.Add(fact.ContactId);
                }
            }
        }

        var pictures = ContactPictureProjection.Project(log);
        for (var i = 0; i < pictures.Count; i++)
        {
            var contactId = pictures[i].ContactId;
            if (!string.IsNullOrEmpty(contactId)
                && contactId.StartsWith("dl-", StringComparison.Ordinal)
                && !explicitIds.Contains(contactId))
            {
                merged.Add(new ContactOriginFact(contactId, ContactInformationOrigin.Datalink));
                explicitIds.Add(contactId);
            }
        }

        return merged.Count > 0 ? merged : null;
    }

    private static SelectedContactCueState BuildSelectedContactCue(ContactCueState? contact)
    {
        if (contact is null)
        {
            return SelectedContactCueState.Empty;
        }

        var fixtureText = contact.Position.Kind == ContactPositionClaimKind.FixtureOnly
            ? (string.IsNullOrWhiteSpace(contact.Position.FixtureLabel)
                ? ContactCueProjection.FixtureOnlyLabel
                : contact.Position.FixtureLabel!)
            : string.Empty;

        if (!contact.IsStale && !contact.IsDatalinkSourced && string.IsNullOrEmpty(fixtureText))
        {
            return SelectedContactCueState.Empty;
        }

        var tokens = FormatContactTokens(contact.IsStale, contact.IsDatalinkSourced);
        var text = string.IsNullOrEmpty(tokens)
            ? contact.ContactId
            : $"{contact.ContactId}: {tokens}";

        var classes = new List<string>(3);
        if (contact.IsStale)
        {
            classes.Add(StaleCssClass);
        }

        if (contact.IsDatalinkSourced)
        {
            classes.Add(DatalinkCssClass);
        }

        if (!string.IsNullOrEmpty(fixtureText))
        {
            classes.Add(FixtureTagCssClass);
        }

        return new SelectedContactCueState(
            Visible: true,
            ContactId: contact.ContactId,
            IsStale: contact.IsStale,
            IsDatalinkSourced: contact.IsDatalinkSourced,
            Text: text,
            FixtureDisclosureText: fixtureText,
            CssClasses: classes);
    }

    private static SelectedUnitOrderCueState BuildSelectedUnitOrderCue(UnitOrderQueueState? unitOrder)
    {
        if (unitOrder is null || !unitOrder.IsGenuinelyPending)
        {
            return SelectedUnitOrderCueState.Empty;
        }

        var text = unitOrder.Kind is { } kind
            ? $"{QueuedBadgeText}: {kind.ToString().ToUpperInvariant()} ({unitOrder.UnitId})"
            : $"{QueuedBadgeText} ({unitOrder.UnitId})";

        return new SelectedUnitOrderCueState(
            Visible: true,
            UnitId: unitOrder.UnitId,
            Disposition: unitOrder.Disposition,
            OrderKind: unitOrder.Kind,
            Text: text,
            CssClasses: new[] { QueuedCssClass });
    }

    private static IReadOnlyList<MapSymbolCueRow> BuildSymbolRows(
        IReadOnlyList<MapSymbolEntry>? symbols,
        ContactCueSnapshot snapshot,
        string? selectedContactId,
        string? selectedUnitId)
    {
        if (symbols is null || symbols.Count == 0)
        {
            return Array.Empty<MapSymbolCueRow>();
        }

        var contactsById = IndexUniqueContacts(snapshot.Contacts);
        var unitsById = IndexUniqueUnits(snapshot.UnitOrders);
        var rows = new MapSymbolCueRow[symbols.Count];

        for (var i = 0; i < symbols.Count; i++)
        {
            var symbol = symbols[i];
            contactsById.TryGetValue(symbol.SymbolId, out var contactCue);
            unitsById.TryGetValue(symbol.SymbolId, out var unitCue);

            var isContact = contactCue is not null;
            var isStale = contactCue?.IsStale ?? false;
            var isDatalink = contactCue?.IsDatalinkSourced ?? false;
            var isQueued = unitCue?.IsGenuinelyPending ?? false;
            var origin = contactCue?.Origin ?? ContactInformationOrigin.Unknown;
            var disposition = unitCue?.Disposition ?? UnitOrderDisposition.None;
            var orderKind = unitCue?.Kind;
            var positionKind = contactCue?.Position.Kind ?? ContactPositionClaimKind.Unknown;
            var fixtureLabel = contactCue?.Position.Kind == ContactPositionClaimKind.FixtureOnly
                ? contactCue.Position.FixtureLabel
                : null;
            var isSelected = string.Equals(symbol.SymbolId, selectedContactId, StringComparison.Ordinal)
                || string.Equals(symbol.SymbolId, selectedUnitId, StringComparison.Ordinal);

            var contactTokensText = FormatContactTokens(isStale, isDatalink);
            var unitBadgeText = isQueued
                ? (orderKind is { } k ? $"{QueuedBadgeText}: {k.ToString().ToUpperInvariant()}" : QueuedBadgeText)
                : string.Empty;

            var classes = new List<string>(4);
            if (isStale)
            {
                classes.Add(StaleCssClass);
            }

            if (isDatalink)
            {
                classes.Add(DatalinkCssClass);
            }

            if (isQueued)
            {
                classes.Add(QueuedCssClass);
            }

            if (!string.IsNullOrEmpty(fixtureLabel))
            {
                classes.Add(FixtureTagCssClass);
            }

            var tooltip = BuildTooltip(symbol.SymbolId, contactCue, unitCue, fixtureLabel);

            rows[i] = new MapSymbolCueRow(
                SymbolId: symbol.SymbolId,
                NormalizedX: symbol.NormalizedX,
                NormalizedY: symbol.NormalizedY,
                IsContact: isContact,
                IsStale: isStale,
                IsDatalinkSourced: isDatalink,
                IsQueuedOrder: isQueued,
                Origin: origin,
                OrderDisposition: disposition,
                OrderKind: orderKind,
                PositionKind: positionKind,
                FixtureLabel: fixtureLabel,
                IsSelected: isSelected,
                ContactTokensText: contactTokensText,
                UnitBadgeText: unitBadgeText,
                TooltipText: tooltip,
                CssClasses: classes);
        }

        return rows;
    }

    private static string FormatContactTokens(bool isStale, bool isDatalink)
    {
        if (isStale && isDatalink)
        {
            return $"{StaleTokenText} · {DatalinkTokenText}";
        }

        if (isStale)
        {
            return StaleTokenText;
        }

        if (isDatalink)
        {
            return DatalinkTokenText;
        }

        return string.Empty;
    }

    private static string BuildTooltip(
        string symbolId,
        ContactCueState? contact,
        UnitOrderQueueState? unit,
        string? fixtureLabel)
    {
        var parts = new List<string>(4);
        if (contact is not null)
        {
            if (contact.IsStale)
            {
                parts.Add(contact.SimAgeTicks is { } age ? $"Stale (age {age} ticks)" : "Stale");
            }

            if (contact.IsDatalinkSourced)
            {
                parts.Add("Datalink track");
            }

            if (!string.IsNullOrEmpty(fixtureLabel))
            {
                parts.Add($"Pos: {fixtureLabel}");
            }
        }

        if (unit is not null && unit.IsGenuinelyPending)
        {
            parts.Add(unit.Kind is { } k ? $"Queued order: {k}" : "Queued order");
        }

        return parts.Count > 0 ? $"{symbolId}: {string.Join(" | ", parts)}" : string.Empty;
    }

    private static Dictionary<string, ContactCueState?> IndexUniqueContacts(IReadOnlyList<ContactCueState> contacts)
    {
        var dict = new Dictionary<string, ContactCueState?>(StringComparer.Ordinal);
        for (var i = 0; i < contacts.Count; i++)
        {
            var row = contacts[i];
            if (dict.ContainsKey(row.ContactId))
            {
                dict[row.ContactId] = null;
            }
            else
            {
                dict[row.ContactId] = row;
            }
        }

        return dict;
    }

    private static Dictionary<string, UnitOrderQueueState?> IndexUniqueUnits(IReadOnlyList<UnitOrderQueueState> units)
    {
        var dict = new Dictionary<string, UnitOrderQueueState?>(StringComparer.Ordinal);
        for (var i = 0; i < units.Count; i++)
        {
            var row = units[i];
            if (dict.ContainsKey(row.UnitId))
            {
                dict[row.UnitId] = null;
            }
            else
            {
                dict[row.UnitId] = row;
            }
        }

        return dict;
    }
}

public sealed record SelectedContactCueState(
    bool Visible,
    string? ContactId,
    bool IsStale,
    bool IsDatalinkSourced,
    string Text,
    string FixtureDisclosureText,
    IReadOnlyList<string> CssClasses)
{
    public static SelectedContactCueState Empty { get; } = new(
        Visible: false,
        ContactId: null,
        IsStale: false,
        IsDatalinkSourced: false,
        Text: string.Empty,
        FixtureDisclosureText: string.Empty,
        CssClasses: Array.Empty<string>());
}

public sealed record SelectedUnitOrderCueState(
    bool Visible,
    string? UnitId,
    UnitOrderDisposition Disposition,
    OrderKind? OrderKind,
    string Text,
    IReadOnlyList<string> CssClasses)
{
    public static SelectedUnitOrderCueState Empty { get; } = new(
        Visible: false,
        UnitId: null,
        Disposition: UnitOrderDisposition.None,
        OrderKind: null,
        Text: string.Empty,
        CssClasses: Array.Empty<string>());
}

public sealed record MapSymbolCueRow(
    string SymbolId,
    float NormalizedX,
    float NormalizedY,
    bool IsContact,
    bool IsStale,
    bool IsDatalinkSourced,
    bool IsQueuedOrder,
    ContactInformationOrigin Origin,
    UnitOrderDisposition OrderDisposition,
    OrderKind? OrderKind,
    ContactPositionClaimKind PositionKind,
    string? FixtureLabel,
    bool IsSelected,
    string ContactTokensText,
    string UnitBadgeText,
    string TooltipText,
    IReadOnlyList<string> CssClasses);

public sealed record MapCueOverlayState(
    IReadOnlyList<ContactCueState> Contacts,
    IReadOnlyList<UnitOrderQueueState> UnitOrders,
    IReadOnlyList<MapSymbolCueRow> Symbols,
    SelectedContactCueState SelectedContactCue,
    SelectedUnitOrderCueState SelectedUnitOrderCue,
    bool IsInspectingHistory)
{
    public static MapCueOverlayState Empty { get; } = new(
        Array.Empty<ContactCueState>(),
        Array.Empty<UnitOrderQueueState>(),
        Array.Empty<MapSymbolCueRow>(),
        SelectedContactCueState.Empty,
        SelectedUnitOrderCueState.Empty,
        IsInspectingHistory: false);
}
