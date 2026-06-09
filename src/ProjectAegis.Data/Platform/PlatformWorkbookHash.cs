namespace ProjectAegis.Data.Platform;

using System.Security.Cryptography;
using System.Text;

/// <summary>
/// Req-21 / ADR-011: deterministic SHA-256 over a workbook's data sheets (excluding <c>_Meta</c>),
/// used for the <c>_Meta.WorkbookHash</c> cell and round-trip golden tests. Locale-independent.
/// </summary>
public static class PlatformWorkbookHash
{
    private static readonly string CellSeparator = ((char)31).ToString();  // US — unit separator
    private static readonly string RowSeparator = ((char)30).ToString();   // RS — record separator
    private static readonly string SheetSeparator = ((char)29).ToString(); // GS — group separator

    public const string MetaSheetName = "_Meta";

    public static string Compute(PlatformWorkbook workbook)
    {
        var sb = new StringBuilder();
        foreach (var sheet in workbook.Sheets)
        {
            if (string.Equals(sheet.Name, MetaSheetName, StringComparison.Ordinal))
            {
                continue; // hash is over content, not the metadata that carries it
            }

            sb.Append(sheet.Name).Append(SheetSeparator);
            sb.Append(string.Join(CellSeparator, sheet.Header)).Append(RowSeparator);
            foreach (var row in sheet.Rows)
            {
                sb.Append(string.Join(CellSeparator, row)).Append(RowSeparator);
            }

            sb.Append(SheetSeparator);
        }

        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));
        return BitConverter.ToString(bytes).Replace("-", string.Empty).ToLowerInvariant();
    }
}
