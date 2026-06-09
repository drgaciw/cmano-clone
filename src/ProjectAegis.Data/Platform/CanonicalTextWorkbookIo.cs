namespace ProjectAegis.Data.Platform;

using System.Text;

/// <summary>
/// Req-21 / ADR-011: dependency-free reference implementation of <see cref="IPlatformWorkbookIo"/>.
/// Serializes a workbook to a deterministic text form using ASCII control delimiters (no spreadsheet
/// library). It exists so the round-trip contract and golden tests can run in CI without ClosedXML, and
/// it doubles as the executable spec the production .xlsx adapter must satisfy:
/// <c>Read(Write(wb))</c> must reconstruct <paramref name="workbook"/> exactly.
/// </summary>
public sealed class CanonicalTextWorkbookIo : IPlatformWorkbookIo
{
    private static readonly char FieldDelimiter = (char)31; // US — unit separator
    private const string SheetTag = "#SHEET";
    private const string HeaderTag = "#HEADER";
    private const string RowTag = "#ROW";

    public void Write(PlatformWorkbook workbook, string path)
    {
        if (workbook is null) throw new ArgumentNullException(nameof(workbook));
        File.WriteAllText(path, Serialize(workbook), new UTF8Encoding(false));
    }

    public PlatformWorkbook Read(string path) => Deserialize(File.ReadAllText(path, new UTF8Encoding(false)));

    public static string Serialize(PlatformWorkbook workbook)
    {
        if (workbook is null) throw new ArgumentNullException(nameof(workbook));

        var sb = new StringBuilder();
        foreach (var sheet in workbook.Sheets)
        {
            sb.Append(SheetTag).Append(FieldDelimiter).Append(sheet.Name).Append('\n');
            sb.Append(HeaderTag).Append(FieldDelimiter).Append(string.Join(FieldDelimiter.ToString(), sheet.Header)).Append('\n');
            foreach (var row in sheet.Rows)
            {
                sb.Append(RowTag).Append(FieldDelimiter).Append(string.Join(FieldDelimiter.ToString(), row)).Append('\n');
            }
        }

        return sb.ToString();
    }

    public static PlatformWorkbook Deserialize(string text)
    {
        var sheets = new List<PlatformWorkbookSheet>();
        string? name = null;
        string[]? header = null;
        var rows = new List<IReadOnlyList<string>>();

        foreach (var line in text.Split('\n'))
        {
            if (line.Length == 0)
            {
                continue;
            }

            var parts = line.Split(FieldDelimiter);
            switch (parts[0])
            {
                case SheetTag:
                    Flush(sheets, ref name, ref header, rows);
                    name = parts.Length > 1 ? parts[1] : string.Empty;
                    header = null;
                    break;
                case HeaderTag:
                    header = parts.Skip(1).ToArray();
                    break;
                case RowTag:
                    rows.Add(parts.Skip(1).ToArray());
                    break;
            }
        }

        Flush(sheets, ref name, ref header, rows);
        return new PlatformWorkbook(sheets);
    }

    private static void Flush(List<PlatformWorkbookSheet> sheets, ref string? name, ref string[]? header, List<IReadOnlyList<string>> rows)
    {
        if (name is null)
        {
            return;
        }

        sheets.Add(new PlatformWorkbookSheet(name, header ?? [], rows.ToArray()));
        name = null;
        header = null;
        rows.Clear();
    }
}
