namespace ProjectAegis.Data.Excel;

using ClosedXML.Excel;
using ProjectAegis.Data.Platform;

/// <summary>
/// Req-21 / ADR-011: production <see cref="IPlatformWorkbookIo"/> backed by ClosedXML, emitting real
/// .xlsx workbooks (one worksheet per <see cref="PlatformWorkbookSheet"/>). Cells are written as text and
/// the column number-format is pinned to text ("@") so numeric-looking values such as "57" round-trip
/// byte-for-byte rather than being coerced to numbers. The round-trip contract proven by
/// <see cref="CanonicalTextWorkbookIo"/> golden tests must hold here too.
/// Phase B UX (S24-11): Emcon <c>Condition</c>/<c>Posture</c> list validation per migration 008 / Req 21.
/// Sheet/PK-column protection (PLE-1.2, doc 21 OQ5) remains deferred.
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

            ApplyPhaseBSheetUx(ws, sheet);
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

    private static void ApplyPhaseBSheetUx(IXLWorksheet ws, PlatformWorkbookSheet sheet)
    {
        if (!string.Equals(sheet.Name, PlatformEmconEnums.EmconSheetName, StringComparison.Ordinal))
        {
            return;
        }

        ApplyListValidation(ws, sheet.Header, PlatformEmconEnums.ConditionColumn, PlatformEmconEnums.Conditions);
        ApplyListValidation(ws, sheet.Header, PlatformEmconEnums.PostureColumn, PlatformEmconEnums.Postures);
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
        validation.List(PlatformEmconEnums.ToExcelList(allowedValues));
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