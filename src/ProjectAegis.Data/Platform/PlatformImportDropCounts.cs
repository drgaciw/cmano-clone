namespace ProjectAegis.Data.Platform;

/// <summary>
/// S125-06 / AUTH-08: counts of workbook rows and fields the importer will <b>not</b> stage, so nothing is
/// silently dropped. Rows and fields are tallied separately (a header-drifted sheet contributes both).
/// </summary>
public sealed record PlatformImportDropCounts(
    int UnknownSheets,
    int UnknownSheetRows,
    int MissingSheets,
    int HeaderDriftRows,
    int RemovedRowsNotStaged,
    int QuarantinedRows,
    int UnknownColumnFields,
    int OverflowFields,
    int UnstageableEdits)
{
    public static PlatformImportDropCounts Empty { get; } = new(0, 0, 0, 0, 0, 0, 0, 0, 0);

    public int IgnoredRows => UnknownSheetRows + HeaderDriftRows + RemovedRowsNotStaged + QuarantinedRows;

    public int IgnoredFields => UnknownColumnFields + OverflowFields + UnstageableEdits;

    public bool HasDrops => IgnoredRows > 0 || IgnoredFields > 0 || MissingSheets > 0;

    /// <summary>Single status line; lists only non-zero buckets.</summary>
    public string Summary
    {
        get
        {
            if (!HasDrops)
            {
                return "Silent-drop guard: 0 ignored rows, 0 ignored fields.";
            }

            var parts = new List<string>();
            Add(parts, UnknownSheetRows, $"row(s) in {UnknownSheets} unknown sheet(s)");
            Add(parts, MissingSheets, "documented sheet(s) missing");
            Add(parts, HeaderDriftRows, "row(s) in header-drifted sheets (edits not evaluated)");
            Add(parts, RemovedRowsNotStaged, "removed row(s) (deletes are not stageable)");
            Add(parts, QuarantinedRows, "quarantined row(s)");
            Add(parts, UnknownColumnFields, "unknown-column field(s)");
            Add(parts, OverflowFields, "field(s) beyond the header");
            Add(parts, UnstageableEdits, "edit(s) to non-stageable columns");
            return $"Silent-drop guard: {IgnoredRows} ignored row(s), {IgnoredFields} ignored field(s) — {string.Join("; ", parts)}.";
        }
    }

    private static void Add(List<string> parts, int count, string label)
    {
        if (count > 0)
        {
            parts.Add($"{count} {label}");
        }
    }
}

/// <summary>Pure counter over an edited workbook and its plan-time change classification.</summary>
public static class PlatformImportDropCounter
{
    public static PlatformImportDropCounts Count(
        PlatformWorkbook edited,
        IReadOnlyList<PlatformWorkbookChange> unsupportedChanges,
        IReadOnlyList<PlatformWorkbookChange> allChanges,
        int quarantinedRows)
    {
        if (edited is null) throw new ArgumentNullException(nameof(edited));

        int unknownSheets = 0, unknownSheetRows = 0, headerDriftRows = 0, unknownColumnFields = 0, overflowFields = 0;
        foreach (var sheet in edited.Sheets)
        {
            if (string.Equals(sheet.Name, PlatformWorkbookHash.MetaSheetName, StringComparison.Ordinal))
            {
                continue;
            }

            var contract = PlatformWorkbookContract.Find(sheet.Name);
            if (contract is null)
            {
                unknownSheets++;
                unknownSheetRows += sheet.Rows.Count;
                continue;
            }

            if (!sheet.Header.SequenceEqual(contract.Columns, StringComparer.Ordinal))
            {
                headerDriftRows += sheet.Rows.Count;
            }

            var unknownColumnIndices = sheet.Header
                .Select((name, index) => (name, index))
                .Where(c => !contract.Columns.Contains(c.name, StringComparer.Ordinal))
                .Select(c => c.index)
                .ToArray();
            foreach (var row in sheet.Rows)
            {
                unknownColumnFields += unknownColumnIndices.Count(i => i < row.Count && row[i].Length > 0);
                for (var c = sheet.Header.Count; c < row.Count; c++)
                {
                    if (row[c].Length > 0)
                    {
                        overflowFields++;
                    }
                }
            }
        }

        var missingSheets = PlatformWorkbookContract.Sheets.Count(s => edited.FindSheet(s.Name) is null);
        var removedRows = allChanges.Count(c => c.Kind == PlatformWorkbookChangeKind.RowRemoved);
        var unstageableEdits = unsupportedChanges.Count(c =>
            c.Kind is PlatformWorkbookChangeKind.CellChanged or PlatformWorkbookChangeKind.RowAdded);

        return new PlatformImportDropCounts(
            unknownSheets,
            unknownSheetRows,
            missingSheets,
            headerDriftRows,
            removedRows,
            quarantinedRows,
            unknownColumnFields,
            overflowFields,
            unstageableEdits);
    }
}
