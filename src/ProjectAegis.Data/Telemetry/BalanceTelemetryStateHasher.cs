namespace ProjectAegis.Data.Telemetry;

using System.Globalization;
using System.Security.Cryptography;
using System.Text;

/// <summary>Deterministic SHA-256 over sorted accumulator rows for CI golden tests.</summary>
internal static class BalanceTelemetryStateHasher
{
    public static string Compute(
        IReadOnlyList<BalanceTelemetryEntitySnapshot> entities,
        IReadOnlyList<BalanceDriftFinding> findings)
    {
        var sb = new StringBuilder();
        foreach (var row in entities
                     .OrderBy(e => e.EntityId, StringComparer.Ordinal)
                     .ThenBy(e => (int)e.EntityKind))
        {
            sb.Append(row.EntityId).Append('\t')
                .Append((int)row.EntityKind).Append('\t')
                .Append(row.Wins).Append('\t')
                .Append(row.TotalRuns).Append('\t')
                .Append(row.ExpectedWinRate.ToString("R", CultureInfo.InvariantCulture))
                .Append('\n');
        }

        sb.Append("--findings--\n");
        foreach (var finding in findings
                     .OrderBy(f => f.EntityId, StringComparer.Ordinal)
                     .ThenBy(f => (int)f.EntityKind)
                     .ThenBy(f => f.Code, StringComparer.Ordinal))
        {
            sb.Append(finding.Code).Append('\t')
                .Append(finding.EntityId).Append('\t')
                .Append((int)finding.EntityKind).Append('\t')
                .Append(finding.SampleRuns).Append('\t')
                .Append(finding.ExpectedWinRate.ToString("R", CultureInfo.InvariantCulture)).Append('\t')
                .Append(finding.ActualWinRate.ToString("R", CultureInfo.InvariantCulture)).Append('\t')
                .Append(finding.DriftDelta.ToString("R", CultureInfo.InvariantCulture)).Append('\t')
                .Append(finding.Message)
                .Append('\n');
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        using var sha = SHA256.Create();
        return ToHexLower(sha.ComputeHash(bytes));
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

internal sealed record BalanceTelemetryEntitySnapshot(
    string EntityId,
    BalanceEntityKind EntityKind,
    int Wins,
    int TotalRuns,
    double ExpectedWinRate);