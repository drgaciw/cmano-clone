namespace ProjectAegis.Delegation.Projection;

using Data.Platform;
using Data.Validation;

/// <summary>One quarantined workbook row with a human label and the fix that releases it.</summary>
public sealed record PlatformImportQuarantineRow(
    string EntityKind,
    string PlatformId,
    string EntityId,
    string ReasonCode,
    string ReasonLabel,
    string ActionHint,
    string SourceSheet,
    string Detail,
    string DisplayLine,
    string UssClass);

/// <summary>Count of quarantined rows per reason code.</summary>
public sealed record PlatformImportQuarantineReasonCount(string ReasonCode, string ReasonLabel, int Count);

/// <summary>Bindable quarantine panel state for the Platform Import pane.</summary>
public sealed record PlatformImportQuarantinePanelState(
    string StatusLine,
    IReadOnlyList<PlatformImportQuarantineRow> Rows,
    IReadOnlyList<PlatformImportQuarantineReasonCount> ReasonCounts,
    int QuarantinedCount,
    bool HasQuarantine,
    string NeverProposedNote,
    string DropSummaryLine,
    bool HasSilentDrops,
    IReadOnlyList<string> LatLonLines);

/// <summary>
/// S125-08 / AUTH-13: Excel-path quarantine UX. Turns <see cref="PlatformImportQuarantineEntry"/> rows into
/// labelled, actionable lines, pairs them with the silent-drop counts and lat/lon messages, and states
/// plainly that quarantined rows are never proposed. Pure projection; no write-gate access.
/// </summary>
public static class PlatformImportQuarantineProjection
{
    public const string NeverProposedNote =
        "Quarantined rows are never proposed to the write gate; fix the source rows and Propose again.";

    public static PlatformImportQuarantinePanelState FromPlan(PlatformImportPlan plan) =>
        Bind(plan.QuarantineEntries, plan.DropCounts, plan.LatLonFindings);

    public static PlatformImportQuarantinePanelState FromResult(PlatformImportResult result) =>
        Bind(result.QuarantineEntries, result.DropCounts, result.Plan.LatLonFindings);

    public static PlatformImportQuarantinePanelState Bind(
        IReadOnlyList<PlatformImportQuarantineEntry> entries,
        PlatformImportDropCounts? dropCounts = null,
        IReadOnlyList<ValidationFinding>? latLonFindings = null)
    {
        var rows = entries
            .OrderBy(e => e.EntityKind, StringComparer.Ordinal)
            .ThenBy(e => e.PlatformId, StringComparer.Ordinal)
            .ThenBy(e => e.EntityId, StringComparer.Ordinal)
            .ThenBy(e => e.Reason, StringComparer.Ordinal)
            .Select(ToRow)
            .ToArray();
        var counts = rows
            .GroupBy(r => r.ReasonCode, StringComparer.Ordinal)
            .OrderBy(g => g.Key, StringComparer.Ordinal)
            .Select(g => new PlatformImportQuarantineReasonCount(g.Key, g.First().ReasonLabel, g.Count()))
            .ToArray();
        var drops = dropCounts ?? PlatformImportDropCounts.Empty;

        var status = rows.Length == 0
            ? "QUARANTINE: empty"
            : $"QUARANTINE: {rows.Length} row(s) held across {counts.Length} reason(s) — not proposed";

        return new PlatformImportQuarantinePanelState(
            status,
            rows,
            counts,
            rows.Length,
            rows.Length > 0,
            NeverProposedNote,
            drops.Summary,
            drops.HasDrops,
            (latLonFindings ?? Array.Empty<ValidationFinding>()).Select(f => f.Message).ToArray());
    }

    public static string ReasonLabel(string reasonCode) => reasonCode switch
    {
        PlatformWorkbookValidator.PhaseBOrphanPlatform => "Unknown platform",
        PlatformWorkbookValidator.MagazineUnknownMount => "Unknown mount",
        PlatformWorkbookValidator.MagazineUnknownLoadout => "Unknown loadout",
        "trl_below_minimum" => "TRL below import minimum",
        "confidence_below_minimum" => "Confidence below import minimum",
        _ when reasonCode.StartsWith("review_state_", StringComparison.Ordinal) => "Not approved for import",
        _ => "Held for review",
    };

    public static string ActionHint(PlatformImportQuarantineEntry entry)
    {
        var sheet = string.IsNullOrEmpty(entry.SourceSheet) ? "the source sheet" : entry.SourceSheet;
        return entry.Reason switch
        {
            PlatformWorkbookValidator.PhaseBOrphanPlatform =>
                $"Add PlatformId '{entry.PlatformId}' to the Platforms sheet (or bound snapshot), or correct PlatformId on {sheet}.",
            PlatformWorkbookValidator.MagazineUnknownMount =>
                $"Add the mount to the Mounts sheet for '{entry.PlatformId}', or correct Magazines.MountId.",
            PlatformWorkbookValidator.MagazineUnknownLoadout =>
                $"Add the loadout to the Loadouts sheet for '{entry.PlatformId}', or correct Magazines.LoadoutId.",
            "trl_below_minimum" =>
                "Raise TrlLevel to the import minimum with a CitationRef, or leave the row out of this import.",
            "confidence_below_minimum" =>
                "Raise confidence with a cited source, or leave the row out of this import.",
            _ when entry.Reason.StartsWith("review_state_", StringComparison.Ordinal) =>
                "Set ReviewState to 'approved' after curator review, then Propose again.",
            _ => $"Review the row on {sheet}; it was not proposed.",
        };
    }

    private static PlatformImportQuarantineRow ToRow(PlatformImportQuarantineEntry entry)
    {
        var label = ReasonLabel(entry.Reason);
        var target = string.IsNullOrEmpty(entry.EntityId) ? entry.PlatformId : $"{entry.PlatformId}/{entry.EntityId}";
        return new PlatformImportQuarantineRow(
            entry.EntityKind,
            entry.PlatformId,
            entry.EntityId,
            entry.Reason,
            label,
            ActionHint(entry),
            entry.SourceSheet,
            entry.Detail,
            $"QUARANTINE [{entry.EntityKind}] {target} — {label} ({entry.Reason})",
            "platform-import-quarantine-row platform-import-quarantine-row--" + UssToken(entry.EntityKind));
    }

    private static string UssToken(string value) =>
        string.IsNullOrWhiteSpace(value)
            ? "unknown"
            : new string(value.ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray());
}
