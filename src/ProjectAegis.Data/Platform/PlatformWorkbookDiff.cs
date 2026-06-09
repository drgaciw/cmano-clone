namespace ProjectAegis.Data.Platform;

/// <summary>Req-21 / ADR-011 §1: a single difference between a source and an edited workbook.</summary>
public sealed record PlatformWorkbookChange(
    string Sheet,
    PlatformWorkbookChangeKind Kind,
    int RowIndex,
    string Detail);

public enum PlatformWorkbookChangeKind
{
    SheetAdded,
    SheetRemoved,
    HeaderChanged,
    RowAdded,
    RowRemoved,
    CellChanged,
}

/// <summary>
/// Req-21 / ADR-011 §1: structural diff between two exporter-ordered workbooks. A workbook diffed
/// against itself (or a faithful round-trip of itself) yields <see cref="IsEmpty"/> — the property the
/// importer relies on so an unedited round-trip stages no changes (PLE-2.1). The <c>_Meta</c> sheet is
/// excluded because its hash/timestamp are derived, not authored.
/// </summary>
public static class PlatformWorkbookDiff
{
    public static IReadOnlyList<PlatformWorkbookChange> Compare(PlatformWorkbook source, PlatformWorkbook edited)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));
        if (edited is null) throw new ArgumentNullException(nameof(edited));

        var changes = new List<PlatformWorkbookChange>();
        var sourceSheets = DataSheets(source);
        var editedSheets = DataSheets(edited);

        foreach (var name in sourceSheets.Keys.Where(k => !editedSheets.ContainsKey(k)))
        {
            changes.Add(new PlatformWorkbookChange(name, PlatformWorkbookChangeKind.SheetRemoved, -1, "sheet missing from edited workbook"));
        }

        foreach (var name in editedSheets.Keys.Where(k => !sourceSheets.ContainsKey(k)))
        {
            changes.Add(new PlatformWorkbookChange(name, PlatformWorkbookChangeKind.SheetAdded, -1, "sheet not present in source workbook"));
        }

        foreach (var name in sourceSheets.Keys.Where(editedSheets.ContainsKey))
        {
            CompareSheet(sourceSheets[name], editedSheets[name], changes);
        }

        return changes;
    }

    public static bool IsEmpty(PlatformWorkbook source, PlatformWorkbook edited) =>
        Compare(source, edited).Count == 0;

    private static void CompareSheet(PlatformWorkbookSheet src, PlatformWorkbookSheet edt, List<PlatformWorkbookChange> changes)
    {
        if (!src.Header.SequenceEqual(edt.Header, StringComparer.Ordinal))
        {
            changes.Add(new PlatformWorkbookChange(src.Name, PlatformWorkbookChangeKind.HeaderChanged, -1,
                $"[{string.Join(",", src.Header)}] -> [{string.Join(",", edt.Header)}]"));
            return; // header mismatch makes cell-level comparison meaningless
        }

        var max = Math.Max(src.Rows.Count, edt.Rows.Count);
        for (var i = 0; i < max; i++)
        {
            if (i >= edt.Rows.Count)
            {
                changes.Add(new PlatformWorkbookChange(src.Name, PlatformWorkbookChangeKind.RowRemoved, i, JoinRow(src.Rows[i])));
                continue;
            }

            if (i >= src.Rows.Count)
            {
                changes.Add(new PlatformWorkbookChange(src.Name, PlatformWorkbookChangeKind.RowAdded, i, JoinRow(edt.Rows[i])));
                continue;
            }

            var a = src.Rows[i];
            var b = edt.Rows[i];
            for (var c = 0; c < src.Header.Count; c++)
            {
                var av = c < a.Count ? a[c] : string.Empty;
                var bv = c < b.Count ? b[c] : string.Empty;
                if (!string.Equals(av, bv, StringComparison.Ordinal))
                {
                    changes.Add(new PlatformWorkbookChange(src.Name, PlatformWorkbookChangeKind.CellChanged, i,
                        $"{src.Header[c]}: '{av}' -> '{bv}'"));
                }
            }
        }
    }

    private static Dictionary<string, PlatformWorkbookSheet> DataSheets(PlatformWorkbook wb) =>
        wb.Sheets
            .Where(s => !string.Equals(s.Name, PlatformWorkbookHash.MetaSheetName, StringComparison.Ordinal))
            .ToDictionary(s => s.Name, s => s, StringComparer.Ordinal);

    private static string JoinRow(IReadOnlyList<string> row) => string.Join(",", row);
}
