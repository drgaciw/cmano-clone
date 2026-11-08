namespace ProjectAegis.Data.Catalog;

using System.Security.Cryptography;
using System.Text;

/// <summary>S34-08 / DBI-4.5: deterministic canonical lines and hash for link catalog reports.</summary>
public static class LinkCatalogReport
{
    public static string FormatCanonicalLine(CatalogLinkEntry link) =>
        $"{link.LinkId}|{link.DisplayName}|{link.LinkType}|{link.LatencyMsNominal}";

    public static IReadOnlyList<string> BuildCanonicalLines(IEnumerable<CatalogLinkEntry> links) =>
        CatalogSortKeyComparer.SortLinks(links)
            .Select(FormatCanonicalLine)
            .OrderBy(line => line, StringComparer.Ordinal)
            .ToArray();

    public static string ComputeLinksHash(IReadOnlyList<CatalogLinkEntry> links)
    {
        var sb = new StringBuilder();
        foreach (var line in BuildCanonicalLines(links))
        {
            sb.Append(line).Append('\n');
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        using var sha = SHA256.Create();
        return ToHexLower(sha.ComputeHash(bytes));
    }

    private static string ToHexLower(byte[] bytes)
    {
        var chars = new char[bytes.Length * 2];
        for (var i = 0; i < bytes.Length; i++)
        {
            var b = bytes[i];
            chars[i * 2] = GetHexNibble(b >> 4);
            chars[i * 2 + 1] = GetHexNibble(b & 0xF);
        }

        return new string(chars);
    }

    private static char GetHexNibble(int value) => (char)(value < 10 ? '0' + value : 'a' + (value - 10));
}