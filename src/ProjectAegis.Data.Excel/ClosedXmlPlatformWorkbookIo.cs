namespace ProjectAegis.Data.Excel;

using ClosedXML.Excel;
using ProjectAegis.Data.Platform;

/// <summary>
/// Req-21 / ADR-011: production <see cref="IPlatformWorkbookIo"/> backed by ClosedXML, emitting real
/// .xlsx workbooks (one worksheet per <see cref="PlatformWorkbookSheet"/>). Cells are written as text and
/// the column number-format is pinned to text ("@") so numeric-looking values such as "57" round-trip
/// byte-for-byte rather than being coerced to numbers. The round-trip contract proven by
/// <see cref="CanonicalTextWorkbookIo"/> golden tests must hold here too.
/// <para>
/// PLE-1.2: Excel list data-validation on known enum columns via
/// <see cref="PlatformWorkbookEnumCatalog"/> (Emcon Condition/Posture plus ReviewState, ValueTier,
/// MountType, LinkType, roles, TRL, booleans). Validation is <b>export-time UX only</b> — the importer
/// does not reject rows solely because these lists are incomplete.
/// </para>
/// <para>
/// OQ5 best-effort protection: the <c>_Meta</c> sheet is worksheet-protected; primary-key columns
/// (<see cref="PlatformWorkbookEnumCatalog.ProtectedPrimaryKeyColumns"/>) are locked and non-PK cells
/// unlocked before sheet protect so editors can still change data cells. Limitations: protection is
/// passwordless (Excel "Unprotect Sheet" removes it), ZIP/XML edits bypass it, and ClosedXML read
/// ignores protection by design. Soft UX guard only — not cryptographic integrity.
/// </para>
/// </summary>
public sealed class ClosedXmlPlatformWorkbookIo : IPlatformWorkbookIo
{
    public void Write(PlatformWorkbook workbook, string path)
    {
        if (workbook is null) throw new ArgumentNullException(nameof(workbook));

        using var wb = new XLWorkbook();
        foreach (var sheet in workbook.Sheets)
        {
            var ws = wb.AddWorksheet(SafeSheetName(sheet.Name));
            ws.Columns().Style.NumberFormat.Format = "@"; // text — prevents numeric coercion on round-trip

            for (var c = 0; c < sheet.Header.Count; c++)
            {
                var cell = ws.Cell(1, c + 1);
                cell.Value = sheet.Header[c];
                cell.Style.Font.Bold = true;
            }

            for (var r = 0; r < sheet.Rows.Count; r++)
            {
                var row = sheet.Rows[r];
                for (var c = 0; c < row.Count; c++)
                {
                    ws.Cell(r + 2, c + 1).Value = row[c];
                }
            }

            ApplySheetUx(ws, sheet);
        }

        wb.SaveAs(path);
    }

    public PlatformWorkbook Read(string path)
    {
        using var wb = new XLWorkbook(path);
        var sheets = new List<PlatformWorkbookSheet>();

        foreach (var ws in wb.Worksheets)
        {
            var range = ws.RangeUsed();
            if (range is null)
            {
                sheets.Add(new PlatformWorkbookSheet(ws.Name, [], []));
                continue;
            }

            var columnCount = range.ColumnCount();
            var rowCount = range.RowCount();

            var header = new string[columnCount];
            for (var c = 1; c <= columnCount; c++)
            {
                header[c - 1] = range.Cell(1, c).GetString();
            }

            var dataRows = new List<IReadOnlyList<string>>();
            for (var r = 2; r <= rowCount; r++)
            {
                var cells = new string[columnCount];
                for (var c = 1; c <= columnCount; c++)
                {
                    cells[c - 1] = range.Cell(r, c).GetString();
                }

                dataRows.Add(cells);
            }

            sheets.Add(new PlatformWorkbookSheet(ws.Name, header, dataRows));
        }

        return new PlatformWorkbook(sheets);
    }

    private const int EnumValidationLastRow = 1000;

    /// <summary>
    /// Applies enum list validation and OQ5 best-effort sheet/PK protection for a written worksheet.
    /// </summary>
    private static void ApplySheetUx(IXLWorksheet ws, PlatformWorkbookSheet sheet)
    {
        ApplyEnumListValidations(ws, sheet);
        ApplySheetProtection(ws, sheet);
    }

    private static void ApplyEnumListValidations(IXLWorksheet ws, PlatformWorkbookSheet sheet)
    {
        foreach (var column in PlatformWorkbookEnumCatalog.ForSheet(sheet.Name))
        {
            ApplyListValidation(ws, sheet.Header, column.ColumnName, column.AllowedValues);
        }
    }

    private static void ApplySheetProtection(IXLWorksheet ws, PlatformWorkbookSheet sheet)
    {
        if (PlatformWorkbookEnumCatalog.IsMetaSheet(sheet.Name))
        {
            // Default cell Locked=true; protect entire _Meta sheet (read-only UX).
            ws.Protect();
            return;
        }

        var lastRow = Math.Max(EnumValidationLastRow, ws.LastRowUsed()?.RowNumber() ?? 1);
        var lastCol = Math.Max(1, sheet.Header.Count);
        var used = ws.Range(1, 1, lastRow, lastCol);
        used.Style.Protection.Locked = false;

        var lockedAnyPk = false;
        for (var i = 0; i < sheet.Header.Count; i++)
        {
            if (!IsProtectedPkColumn(sheet.Header[i]))
            {
                continue;
            }

            lockedAnyPk = true;
            var excelColumn = i + 1;
            ws.Range(1, excelColumn, lastRow, excelColumn).Style.Protection.Locked = true;
        }

        // Protect only when there is something meaningful to lock (PK columns present).
        if (lockedAnyPk)
        {
            ws.Protect();
        }
    }

    private static bool IsProtectedPkColumn(string headerName)
    {
        foreach (var pk in PlatformWorkbookEnumCatalog.ProtectedPrimaryKeyColumns)
        {
            if (string.Equals(pk, headerName, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static void ApplyListValidation(
        IXLWorksheet ws,
        IReadOnlyList<string> header,
        string columnName,
        IReadOnlyList<string> allowedValues)
    {
        var columnIndex = IndexOfHeader(header, columnName);
        if (columnIndex < 0)
        {
            return;
        }

        var excelColumn = columnIndex + 1;
        var firstDataRow = 2;
        var lastDataRow = Math.Max(firstDataRow, ws.LastRowUsed()?.RowNumber() ?? firstDataRow);
        lastDataRow = Math.Max(lastDataRow, EnumValidationLastRow);

        var range = ws.Range(firstDataRow, excelColumn, lastDataRow, excelColumn);
        var validation = range.CreateDataValidation();
        validation.List(PlatformWorkbookEnumCatalog.ToExcelList(allowedValues));
        validation.InCellDropdown = true;
        validation.IgnoreBlanks = true;
    }

    private static int IndexOfHeader(IReadOnlyList<string> header, string columnName)
    {
        for (var i = 0; i < header.Count; i++)
        {
            if (string.Equals(header[i], columnName, StringComparison.Ordinal))
            {
                return i;
            }
        }

        return -1;
    }

    // Excel worksheet names are capped at 31 chars and forbid : \ / ? * [ ]. Our names are short/clean,
    // but guard anyway so an extended schema can't produce an invalid book.
    private static string SafeSheetName(string name)
    {
        var cleaned = name;
        foreach (var bad in new[] { ':', '\\', '/', '?', '*', '[', ']' })
        {
            cleaned = cleaned.Replace(bad, '_');
        }

        return cleaned.Length <= 31 ? cleaned : cleaned.Substring(0, 31);
    }
}
