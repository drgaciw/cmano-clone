namespace ProjectAegis.Data.Validation;

using System.Security.Cryptography;
using System.Text;

public sealed record ValidationReport(
    bool Passed,
    IReadOnlyList<ValidationFinding> Findings,
    string ReportHash)
{
    public static ValidationReport FromFindings(IReadOnlyList<ValidationFinding> findings)
    {
        var sorted = SortFindings(findings);
        var passed = sorted.All(f => f.Severity < ValidationSeverity.Error);
        return new ValidationReport(passed, sorted, ComputeHash(sorted));
    }

    public bool CanExport(ValidationConfig config) =>
        !Findings.Any(f => f.Severity >= config.ExportBlockSeverityFloor);

    internal static IReadOnlyList<ValidationFinding> SortFindings(IReadOnlyList<ValidationFinding> findings) =>
        findings
            .OrderByDescending(f => f.Severity)
            .ThenBy(f => f.Code, StringComparer.Ordinal)
            .ThenBy(f => f.MissionId ?? "", StringComparer.Ordinal)
            .ThenBy(f => f.UnitId ?? "", StringComparer.Ordinal)
            .ThenBy(f => f.TargetId ?? "", StringComparer.Ordinal)
            .ThenBy(f => f.Message, StringComparer.Ordinal)
            .ToArray();

    private static string ToHexLower(byte[] bytes)
    {
        var chars = new char[bytes.Length * 2];
        for (var i = 0; i < bytes.Length; i++)
        {
            chars[i * 2] = GetHexNibble(bytes[i] >> 4);
            chars[i * 2 + 1] = GetHexNibble(bytes[i] & 0xF);
        }

        return new string(chars);
    }

    private static char GetHexNibble(int value) => (char)(value < 10 ? '0' + value : 'a' + (value - 10));

    private static string ComputeHash(IReadOnlyList<ValidationFinding> findings)
    {
        var sb = new StringBuilder();
        foreach (var f in findings)
        {
            sb.Append((int)f.Severity).Append('|')
                .Append(f.Code).Append('|')
                .Append(f.MissionId).Append('|')
                .Append(f.UnitId).Append('|')
                .Append(f.TargetId).Append('|')
                .Append(f.Message).Append('\n');
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(bytes);
        return ToHexLower(hash);
    }
}