namespace ProjectAegis.Data.Snapshots;

using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using ProjectAegis.Data.Catalog;

/// <summary>Deterministic SHA-256 fingerprint over sorted catalog sensor rows (P2-3).</summary>
public static class CatalogSnapshotHasher
{
    public static string ComputeSha256Hex(IReadOnlyList<CatalogSensorBinding> bindings)
    {
        var sorted = bindings
            .OrderBy(b => b.PlatformId, StringComparer.Ordinal)
            .ThenBy(b => b.SensorId, StringComparer.Ordinal)
            .ToArray();

        var sb = new StringBuilder(sorted.Length * 64);
        foreach (var row in sorted)
        {
            sb.Append(row.PlatformId);
            sb.Append('\t');
            sb.Append(row.SensorId);
            sb.Append('\t');
            sb.Append(row.BasePd.ToString("R", CultureInfo.InvariantCulture));
            sb.Append('\t');
            sb.Append(row.Confidence.ToString("R", CultureInfo.InvariantCulture));
            sb.Append('\t');
            sb.Append(row.ImportBatchId);
            sb.Append('\t');
            sb.Append(row.SourceFile);
            sb.Append('\n');
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(bytes);
        return ToHexLower(hash);
    }

    private static string ToHexLower(byte[] hash)
    {
        var chars = new char[hash.Length * 2];
        for (var i = 0; i < hash.Length; i++)
        {
            var b = hash[i];
            chars[i * 2] = GetHexNibble(b >> 4);
            chars[i * 2 + 1] = GetHexNibble(b & 0xF);
        }

        return new string(chars);
    }

    private static char GetHexNibble(int value) =>
        (char)(value < 10 ? '0' + value : 'a' + (value - 10));
}