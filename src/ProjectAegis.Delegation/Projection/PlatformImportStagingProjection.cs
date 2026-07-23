namespace ProjectAegis.Delegation.Projection;

using ProjectAegis.Data.Platform;

/// <summary>Workbook domain section for Platform Import staging filters (PE-UX-W1 / P-PE-04).</summary>
public enum PlatformImportStagingSection
{
    Damage,
    Comms,
    Link,
    Other,
}

/// <summary>Visual change kind for art-bible / P-PE-02 USS classes.</summary>
public enum PlatformImportStagingDiffKind
{
    Added,
    Changed,
    Removed,
    Blocked,
    Info,
}

/// <summary>ADR-011 Phase E: entity-level staging review rows for platform workbook import UI.</summary>
public sealed record PlatformImportStagingRow(
    string EntityKey,
    int ChangeCount,
    string SummaryLine,
    PlatformImportStagingSection Section = PlatformImportStagingSection.Other,
    PlatformImportStagingDiffKind DiffKind = PlatformImportStagingDiffKind.Info,
    string UssClass = "platform-import-diff-row--info");

/// <summary>Bindable staging review state — approve stays disabled until review is acknowledged.</summary>
public sealed record PlatformImportStagingPanelState(
    string StatusLine,
    IReadOnlyList<PlatformImportStagingRow> DiffRows,
    IReadOnlyList<string> BatchIds,
    bool HasPendingBatches,
    bool RequiresHumanApproval,
    bool ReviewAcknowledged,
    bool ApproveEnabled,
    bool RejectEnabled,
    bool IsEmptyDiff,
    bool IsBlocked = false);

public static class PlatformImportStagingProjection
{
    private static readonly string[] DamageWorkbookColumns =
    [
        "MaxHp",
        "WithdrawThresholdPct",
        "CriticalFlags",
    ];

    private static readonly string[] CommsWorkbookColumns =
    [
        "LinkId",
        "Role",
        "SatcomCapable",
    ];

    private static readonly string[] LinkCatalogWorkbookColumns =
    [
        "LinkId",
        "DisplayName",
        "LinkType",
        "LatencyMsNominal",
    ];

    public static string UssClassFor(PlatformImportStagingDiffKind kind) =>
        kind switch
        {
            PlatformImportStagingDiffKind.Added => "platform-import-diff-row--added",
            PlatformImportStagingDiffKind.Changed => "platform-import-diff-row--changed",
            PlatformImportStagingDiffKind.Removed => "platform-import-diff-row--removed",
            PlatformImportStagingDiffKind.Blocked => "platform-import-diff-row--blocked",
            _ => "platform-import-diff-row--info",
        };

    public static PlatformImportStagingDiffKind DiffKindFor(PlatformWorkbookChangeKind kind) =>
        kind switch
        {
            PlatformWorkbookChangeKind.RowAdded or PlatformWorkbookChangeKind.SheetAdded =>
                PlatformImportStagingDiffKind.Added,
            PlatformWorkbookChangeKind.RowRemoved or PlatformWorkbookChangeKind.SheetRemoved =>
                PlatformImportStagingDiffKind.Removed,
            PlatformWorkbookChangeKind.CellChanged or PlatformWorkbookChangeKind.HeaderChanged =>
                PlatformImportStagingDiffKind.Changed,
            _ => PlatformImportStagingDiffKind.Info,
        };

    public static IReadOnlyList<PlatformImportStagingRow> FilterBySection(
        IReadOnlyList<PlatformImportStagingRow> rows,
        PlatformImportStagingSection? section)
    {
        if (rows.Count == 0 || section is null)
        {
            return rows;
        }

        return rows.Where(r => r.Section == section.Value).ToArray();
    }

    public static PlatformImportStagingPanelState Bind(
        PlatformWorkbookWriteResult? proposeResult,
        bool reviewAcknowledged,
        PlatformImportStagingSection? sectionFilter = null)
    {
        if (proposeResult is null)
        {
            return Idle(reviewAcknowledged);
        }

        var plan = proposeResult.Import.Plan;
        if (plan.Blocked)
        {
            var blockedRows = MarkBlocked(BuildDiffRows(plan.Changes));
            var filteredBlocked = FilterBySection(blockedRows, sectionFilter);
            return new PlatformImportStagingPanelState(
                "STAGING: blocked by validation errors — resolve before approve",
                filteredBlocked,
                Array.Empty<string>(),
                false,
                plan.RequiresHumanApproval,
                reviewAcknowledged,
                ApproveEnabled: false,
                RejectEnabled: false,
                IsEmptyDiff: !plan.HasChanges,
                IsBlocked: true);
        }

        if (!plan.HasChanges)
        {
            return new PlatformImportStagingPanelState(
                "STAGING: empty diff — no batches proposed",
                Array.Empty<PlatformImportStagingRow>(),
                Array.Empty<string>(),
                false,
                plan.RequiresHumanApproval,
                reviewAcknowledged,
                ApproveEnabled: false,
                RejectEnabled: false,
                IsEmptyDiff: true,
                IsBlocked: false);
        }

        var batchIds = proposeResult.BatchIds;
        var hasPending = proposeResult.Proposed && batchIds.Count > 0;
        var allDiffRows = BuildDiffRows(plan.Changes);
        var diffRows = FilterBySection(allDiffRows, sectionFilter);
        var approvalHint = plan.RequiresHumanApproval
            ? " | human approval required"
            : string.Empty;

        return new PlatformImportStagingPanelState(
            $"STAGING: {plan.Changes.Count} change(s) across {allDiffRows.Count} entity sheet(s){approvalHint}",
            diffRows,
            batchIds,
            hasPending,
            plan.RequiresHumanApproval,
            reviewAcknowledged,
            ApproveEnabled: hasPending && reviewAcknowledged,
            RejectEnabled: hasPending,
            IsEmptyDiff: false,
            IsBlocked: false);
    }

    public static IReadOnlyList<PlatformImportStagingRow> BuildDiffRows(
        IReadOnlyList<PlatformWorkbookChange> changes)
    {
        if (changes.Count == 0)
        {
            return Array.Empty<PlatformImportStagingRow>();
        }

        var damageRows = ExtractDamageDeltaRows(changes);
        var commsRows = ExtractCommsDeltaRows(changes);
        var linkRows = ExtractLinkCatalogDeltaRows(changes);
        var remaining = changes
            .Where(change => !IsDamageWorkbookChange(change)
                && !IsCommsWorkbookChange(change)
                && !IsLinkCatalogWorkbookChange(change))
            .ToArray();
        return damageRows
            .Concat(commsRows)
            .Concat(linkRows)
            .Concat(GroupChangesByEntity(remaining))
            .ToArray();
    }

    public static IReadOnlyList<PlatformImportStagingRow> ExtractDamageDeltaRows(
        IReadOnlyList<PlatformWorkbookChange> changes) =>
        changes
            .Where(IsDamageWorkbookChange)
            .OrderBy(change => change.RowIndex)
            .ThenBy(change => change.Detail, StringComparer.Ordinal)
            .Select(change => MakeRow(
                "Platforms",
                1,
                $"DAMAGE row={FormatRow(change.RowIndex)}: {Truncate(change.Detail, 72)}",
                PlatformImportStagingSection.Damage,
                DiffKindFor(change.Kind)))
            .ToArray();

    public static IReadOnlyList<PlatformImportStagingRow> ExtractCommsDeltaRows(
        IReadOnlyList<PlatformWorkbookChange> changes) =>
        changes
            .Where(IsCommsWorkbookChange)
            .OrderBy(change => change.RowIndex)
            .ThenBy(change => change.Detail, StringComparer.Ordinal)
            .Select(change => MakeRow(
                "Comms",
                1,
                $"COMMS row={FormatRow(change.RowIndex)}: {Truncate(change.Detail, 72)}",
                PlatformImportStagingSection.Comms,
                DiffKindFor(change.Kind)))
            .ToArray();

    public static IReadOnlyList<PlatformImportStagingRow> ExtractLinkCatalogDeltaRows(
        IReadOnlyList<PlatformWorkbookChange> changes) =>
        changes
            .Where(IsLinkCatalogWorkbookChange)
            .OrderBy(change => change.RowIndex)
            .ThenBy(change => change.Detail, StringComparer.Ordinal)
            .Select(change => MakeRow(
                "LinkCatalog",
                1,
                $"LINK row={FormatRow(change.RowIndex)}: {Truncate(change.Detail, 72)}",
                PlatformImportStagingSection.Link,
                DiffKindFor(change.Kind)))
            .ToArray();

    public static IReadOnlyList<PlatformImportStagingRow> GroupChangesByEntity(
        IReadOnlyList<PlatformWorkbookChange> changes)
    {
        if (changes.Count == 0)
        {
            return Array.Empty<PlatformImportStagingRow>();
        }

        return changes
            .GroupBy(change => change.Sheet, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group =>
            {
                var sample = group.First();
                var detail = Truncate(sample.Detail, 72);
                var kindLabel = group.Count() == 1
                    ? sample.Kind.ToString()
                    : $"{group.Count()} {PluralizeKind(sample.Kind)}";
                var kind = group.Count() == 1
                    ? DiffKindFor(sample.Kind)
                    : PlatformImportStagingDiffKind.Info;
                return MakeRow(
                    group.Key,
                    group.Count(),
                    $"{group.Key}: {kindLabel} row={FormatRow(sample.RowIndex)} — {detail}",
                    PlatformImportStagingSection.Other,
                    kind);
            })
            .ToArray();
    }

    public static IReadOnlyList<string> FormatDiffLines(IReadOnlyList<PlatformWorkbookChange> changes) =>
        BuildDiffRows(changes).Select(row => row.SummaryLine).ToArray();

    public static bool IsDamageWorkbookChange(PlatformWorkbookChange change) =>
        string.Equals(change.Sheet, "Platforms", StringComparison.Ordinal)
        && change.Kind == PlatformWorkbookChangeKind.CellChanged
        && DamageWorkbookColumns.Any(column =>
            change.Detail.StartsWith($"{column}:", StringComparison.Ordinal));

    public static bool IsCommsWorkbookChange(PlatformWorkbookChange change) =>
        string.Equals(change.Sheet, "Comms", StringComparison.Ordinal)
        && (change.Kind == PlatformWorkbookChangeKind.RowAdded
            || change.Kind == PlatformWorkbookChangeKind.RowRemoved
            || (change.Kind == PlatformWorkbookChangeKind.CellChanged
                && CommsWorkbookColumns.Any(column =>
                    change.Detail.StartsWith($"{column}:", StringComparison.Ordinal))));

    public static bool IsLinkCatalogWorkbookChange(PlatformWorkbookChange change) =>
        string.Equals(change.Sheet, "LinkCatalog", StringComparison.Ordinal)
        && (change.Kind == PlatformWorkbookChangeKind.RowAdded
            || change.Kind == PlatformWorkbookChangeKind.RowRemoved
            || (change.Kind == PlatformWorkbookChangeKind.CellChanged
                && LinkCatalogWorkbookColumns.Any(column =>
                    change.Detail.StartsWith($"{column}:", StringComparison.Ordinal))));

    private static PlatformImportStagingRow MakeRow(
        string entityKey,
        int changeCount,
        string summaryLine,
        PlatformImportStagingSection section,
        PlatformImportStagingDiffKind diffKind) =>
        new(entityKey, changeCount, summaryLine, section, diffKind, UssClassFor(diffKind));

    private static IReadOnlyList<PlatformImportStagingRow> MarkBlocked(
        IReadOnlyList<PlatformImportStagingRow> rows) =>
        rows
            .Select(r => r with
            {
                DiffKind = PlatformImportStagingDiffKind.Blocked,
                UssClass = UssClassFor(PlatformImportStagingDiffKind.Blocked),
            })
            .ToArray();

    private static PlatformImportStagingPanelState Idle(bool reviewAcknowledged) =>
        new(
            "STAGING: pick workbook and propose to preview entity-level diff",
            Array.Empty<PlatformImportStagingRow>(),
            Array.Empty<string>(),
            false,
            false,
            reviewAcknowledged,
            ApproveEnabled: false,
            RejectEnabled: false,
            IsEmptyDiff: false,
            IsBlocked: false);

    private static string FormatRow(int rowIndex) =>
        rowIndex < 0 ? "—" : rowIndex.ToString(System.Globalization.CultureInfo.InvariantCulture);

    private static string PluralizeKind(PlatformWorkbookChangeKind kind) =>
        kind switch
        {
            PlatformWorkbookChangeKind.CellChanged => "cell changes",
            PlatformWorkbookChangeKind.RowAdded => "row adds",
            PlatformWorkbookChangeKind.RowRemoved => "row removes",
            PlatformWorkbookChangeKind.SheetAdded => "sheet adds",
            PlatformWorkbookChangeKind.SheetRemoved => "sheet removes",
            PlatformWorkbookChangeKind.HeaderChanged => "header changes",
            _ => "changes",
        };

    private static string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
        {
            return value;
        }

        return value[..(maxLength - 1)] + "…";
    }
}
