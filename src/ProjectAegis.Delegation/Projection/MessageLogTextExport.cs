namespace ProjectAegis.Delegation.Projection;

using System.Globalization;
using System.Text;

/// <summary>
/// DRG-412: headless plain-text export of projected message-log lines (CMO AALog-style).
/// Deterministic: invariant culture, LF newlines, no wall clock. Never mutates sim state.
/// </summary>
public static class MessageLogTextExport
{
    /// <summary>One export row: sim time, category, subject id, message — tab-separated.</summary>
    public static string FormatLine(MessageLogLine line)
    {
        if (line is null)
        {
            throw new ArgumentNullException(nameof(line));
        }

        var time = line.SimTime.ToString("0.000000", CultureInfo.InvariantCulture);
        var subject = line.UnitId ?? string.Empty;
        return string.Concat(time, "\t", line.Category, "\t", subject, "\t", line.Text);
    }

    /// <summary>
    /// Renders the full export as UTF-8-compatible plain text with LF line endings and a
    /// trailing newline when any lines are present. Empty input yields an empty string.
    /// </summary>
    public static string Render(IReadOnlyList<MessageLogLine> lines)
    {
        if (lines is null)
        {
            throw new ArgumentNullException(nameof(lines));
        }

        if (lines.Count == 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        for (var i = 0; i < lines.Count; i++)
        {
            builder.Append(FormatLine(lines[i]));
            builder.Append('\n');
        }

        return builder.ToString();
    }

    /// <summary>
    /// Writes <see cref="Render"/> output to <paramref name="path"/> (UTF-8, no BOM).
    /// Read-only with respect to sim / decision-log state.
    /// </summary>
    public static void WriteToPath(IReadOnlyList<MessageLogLine> lines, string path)
    {
        if (lines is null)
        {
            throw new ArgumentNullException(nameof(lines));
        }

        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path must be non-empty.", nameof(path));
        }

        File.WriteAllText(path, Render(lines), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }
}
