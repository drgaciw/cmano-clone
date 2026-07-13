namespace ProjectAegis.Delegation.Projection;

using ProjectAegis.Data.Catalog;

/// <summary>S40-03: read-only mount/loadout quarantine triage surfacing for Platform Editor (projection-side).</summary>
public sealed record MountLoadoutQuarantinePanelState(
    string StatusLine,
    IReadOnlyList<string> DomainSummaryLines,
    IReadOnlyList<string> QuarantineDetailLines,
    int TotalQuarantined,
    int Repairable,
    int OutOfEnvelope,
    bool DryRun);

public static class MountLoadoutQuarantineProjection
{
    public static string FormatDomainSummary(MountLoadoutDomainQuarantineCounts counts) =>
        $"DOMAIN {counts.Domain} mount={counts.MountQuarantined} loadout={counts.LoadoutQuarantined} " +
        $"fitting={counts.FittingQuarantined} repairable={counts.Repairable} out_of_envelope={counts.OutOfEnvelope}";

    public static string FormatQuarantineRow(MountLoadoutQuarantineRow row)
    {
        var repair = string.IsNullOrWhiteSpace(row.RepairRule) ? "none" : row.RepairRule;
        return
            $"QUARANTINE {row.Domain}/{row.ChildKind} platform={row.PlatformId} child={row.ChildId} " +
            $"batch={row.BatchId} reason={row.Reason} repair={repair}";
    }

    public static IReadOnlyList<string> FormatDomainSummaries(
        IEnumerable<MountLoadoutDomainQuarantineCounts> counts) =>
        counts
            .OrderBy(c => c.Domain, StringComparer.Ordinal)
            .Select(FormatDomainSummary)
            .ToArray();

    public static IReadOnlyList<string> FormatQuarantineRows(
        IEnumerable<MountLoadoutQuarantineRow> rows) =>
        rows
            .OrderBy(r => r.Domain, StringComparer.Ordinal)
            .ThenBy(r => r.ChildKind, StringComparer.Ordinal)
            .ThenBy(r => r.PlatformId, StringComparer.Ordinal)
            .ThenBy(r => r.ChildId, StringComparer.Ordinal)
            .Select(FormatQuarantineRow)
            .ToArray();

    public static MountLoadoutQuarantinePanelState BindFromAudit(
        IReadOnlyList<MountLoadoutDomainQuarantineCounts> audit,
        bool dryRun = true)
    {
        var domainLines = FormatDomainSummaries(audit);
        var total = audit.Sum(c => c.MountQuarantined + c.LoadoutQuarantined + c.FittingQuarantined);
        var repairable = audit.Sum(c => c.Repairable);
        var outOfEnvelope = audit.Sum(c => c.OutOfEnvelope);
        var mode = dryRun ? "dry-run audit" : "post-apply audit";
        var status = total == 0
            ? $"MOUNT/LOADOUT: no quarantined child rows ({mode})"
            : $"MOUNT/LOADOUT: {total} quarantined row(s); repairable={repairable}; out_of_envelope={outOfEnvelope} ({mode})";

        return new MountLoadoutQuarantinePanelState(
            status,
            domainLines,
            Array.Empty<string>(),
            total,
            repairable,
            outOfEnvelope,
            dryRun);
    }

    public static MountLoadoutQuarantinePanelState BindFromTriage(MountLoadoutQuarantineTriageResult result)
    {
        var domainLines = FormatDomainSummaries(result.Before);
        var detailLines = FormatQuarantineRows(result.RemainingQuarantine);
        var total = result.Before.Sum(c => c.MountQuarantined + c.LoadoutQuarantined + c.FittingQuarantined);
        var repairable = result.Before.Sum(c => c.Repairable);
        var outOfEnvelope = result.Before.Sum(c => c.OutOfEnvelope);
        var mode = result.DryRun ? "dry-run triage" : "applied triage";
        var status = total == 0
            ? $"MOUNT/LOADOUT: triage ok — no remaining quarantine ({mode})"
            : $"MOUNT/LOADOUT: {detailLines.Count} remaining row(s); repairable={repairable}; out_of_envelope={outOfEnvelope} ({mode})";

        if (result.AdvisoryNotes.Count > 0)
        {
            status += $" | notes={result.AdvisoryNotes.Count}";
        }

        return new MountLoadoutQuarantinePanelState(
            status,
            domainLines,
            detailLines,
            total,
            repairable,
            outOfEnvelope,
            result.DryRun);
    }
}
