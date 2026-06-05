using System.Globalization;
using System.Text;
using ProjectAegis.Delegation.Decision;

namespace ProjectAegis.Delegation.Projection;

/// <summary>Doc 17 headless CSV export for agent-vs-agent batch runs.</summary>
public static class LossesScoringCsvExporter
{
    public const string Header = "scenarioId,seed,side,score,kills,missilesFired,denials,fingerprint";

    public static string FormatRow(
        string scenarioId,
        int seed,
        string side,
        DecisionLog log,
        int baseScore = 0)
    {
        var tally = LossesScoringProjection.Project(log, baseScore);
        return string.Join(
            ",",
            Escape(scenarioId),
            seed.ToString(CultureInfo.InvariantCulture),
            Escape(side),
            tally.Score.ToString(CultureInfo.InvariantCulture),
            tally.HostileKills.ToString(CultureInfo.InvariantCulture),
            tally.MissilesFired.ToString(CultureInfo.InvariantCulture),
            tally.PolicyDenials.ToString(CultureInfo.InvariantCulture),
            Escape(log.ComputeFingerprint()));
    }

    public static string FormatBatch(IEnumerable<string> rows)
    {
        var builder = new StringBuilder();
        builder.AppendLine(Header);
        foreach (var row in rows)
        {
            builder.AppendLine(row);
        }

        return builder.ToString();
    }

    private static string Escape(string value)
    {
        value = value.Replace("\r\n", " ", StringComparison.Ordinal)
            .Replace('\n', ' ')
            .Replace('\r', ' ');
        if (value.Contains(',', StringComparison.Ordinal) || value.Contains('"', StringComparison.Ordinal))
        {
            return $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
        }

        return value;
    }
}