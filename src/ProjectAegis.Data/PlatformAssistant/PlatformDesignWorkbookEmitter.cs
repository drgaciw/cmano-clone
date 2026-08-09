namespace ProjectAegis.Data.PlatformAssistant;

using System.Globalization;
using ProjectAegis.Data.Platform;
using ProjectAegis.Data.WriteGate;

/// <summary>
/// workbook-emit skill — pure function that appends proposal rows onto an exported workbook
/// for Excel handoff. Does not touch CatalogWriteGate.
/// </summary>
public static class PlatformDesignWorkbookEmitter
{
    public static PlatformWorkbook Emit(
        PlatformCatalogExportData export,
        PlatformDesignProposal proposal,
        string snapshotId,
        ICatalogClock clock)
    {
        if (export is null) throw new ArgumentNullException(nameof(export));
        if (proposal is null) throw new ArgumentNullException(nameof(proposal));
        if (clock is null) throw new ArgumentNullException(nameof(clock));

        var baseBook = new PlatformWorkbookExporter().Export(export, snapshotId, clock);
        return AppendProposal(baseBook, proposal);
    }

    public static PlatformWorkbook AppendProposal(PlatformWorkbook source, PlatformDesignProposal proposal)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));
        if (proposal is null) throw new ArgumentNullException(nameof(proposal));

        var sheets = new List<PlatformWorkbookSheet>(source.Sheets.Count);
        foreach (var sheet in source.Sheets)
        {
            if (string.Equals(sheet.Name, "Platforms", StringComparison.Ordinal))
            {
                sheets.Add(AppendRow(sheet,
                [
                    proposal.Binding.PlatformId,
                    Num(proposal.LatDeg),
                    Num(proposal.LonDeg),
                    Num(proposal.CombatRadiusNm),
                    Num(proposal.Damage.MaxHp),
                    Num(proposal.Damage.WithdrawThresholdPct),
                    Int(proposal.Damage.CriticalFlags),
                ]));
                continue;
            }

            if (string.Equals(sheet.Name, "Mobility", StringComparison.Ordinal))
            {
                sheets.Add(AppendRow(sheet,
                [
                    proposal.Mobility.PlatformId,
                    Num(proposal.Mobility.MaxSpeedKnots),
                    Num(proposal.Mobility.CruiseSpeedKnots),
                    Num(proposal.Mobility.MaxAltitudeFt),
                    Num(proposal.Mobility.MaxDepthM),
                    Num(proposal.Mobility.FuelCapacity),
                    Num(proposal.Mobility.RangeNm),
                    Num(proposal.Mobility.EnduranceHr),
                ]));
                continue;
            }

            // Drop _Meta so re-export/hash can be recomputed by caller if needed; keep other sheets.
            if (string.Equals(sheet.Name, "_Meta", StringComparison.Ordinal))
            {
                continue;
            }

            sheets.Add(sheet);
        }

        return new PlatformWorkbook(sheets);
    }

    private static PlatformWorkbookSheet AppendRow(PlatformWorkbookSheet sheet, IReadOnlyList<string> row)
    {
        var rows = sheet.Rows.Concat([row]).ToArray();
        return new PlatformWorkbookSheet(sheet.Name, sheet.Header, rows);
    }

    private static string Num(double v) => v.ToString(CultureInfo.InvariantCulture);

    private static string Int(int v) => v.ToString(CultureInfo.InvariantCulture);
}
