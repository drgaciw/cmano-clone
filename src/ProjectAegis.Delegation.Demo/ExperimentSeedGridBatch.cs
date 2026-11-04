using System.Globalization;
using System.Reflection;
using System.Text;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Baltic;

namespace ProjectAegis.Delegation.Demo;

/// <summary>One headless seed-grid run plus the score projection for its CSV row.</summary>
public sealed record ExperimentBatchRow(
    ExperimentGridPoint Point,
    BalticReplayHarness.Result Result,
    LossesScoringSnapshot Score);

/// <summary>
/// Seed-grid batch for <c>--batch --experiment</c>. Each point calls
/// <see cref="BalticReplayHarness.Run"/> and does not modify <c>BalticReplayHarness</c>,
/// <c>BalticBatchRunner</c>, <c>SimulationSession</c>, or <c>DelegationBridge</c>.
/// Opaque doctrine, ROE, and force-ratio ids are written to the CSV and are not applied.
/// </summary>
public static class ExperimentSeedGridBatch
{
    /// <summary>
    /// CSV header. Fingerprint columns are <c>fingerprint</c>, <c>fingerprintSha256</c>,
    /// <c>worldHash</c>, and <c>detectionWorldHash</c>.
    /// </summary>
    public const string Header =
        "scenarioId,seed,ticks,side,doctrineId,roeId,forceRatioId,gridMethod,score,kills,missilesFired,denials,fingerprint,fingerprintSha256,worldHash,detectionWorldHash,buildId";

    /// <summary>Assembly identity recorded on every row so a definition plus a build repeats.</summary>
    public static string BuildId { get; } = ResolveBuildId();

    /// <summary>Executes every expanded grid point serially through <see cref="BalticReplayHarness.Run"/>.</summary>
    public static IReadOnlyList<ExperimentBatchRow> Run(ExperimentDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        EnsureKnownScenario(definition);
        var points = ExperimentSeedGrid.Expand(definition);
        var rows = new List<ExperimentBatchRow>(points.Count);
        foreach (var point in points)
        {
            var result = BalticReplayHarness.Run(
                point.Seed,
                definition.ScenarioId,
                definition.Ticks,
                mvpEngagement: definition.MvpEngagement);
            var score = LossesScoringProjection.Project(result.DecisionLog, scoredSide: definition.Side);
            rows.Add(new ExperimentBatchRow(point, result, score));
        }

        return rows;
    }

    /// <summary>Formats <paramref name="rows"/> as a CSV document with a header line.</summary>
    public static string ExportCsv(IReadOnlyList<ExperimentBatchRow> rows, ExperimentDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(rows);
        ArgumentNullException.ThrowIfNull(definition);
        var builder = new StringBuilder();
        builder.AppendLine(Header);
        var gridMethod = GridMethodToken(definition.GridMethod);
        foreach (var row in rows)
        {
            builder.Append(Escape(definition.ScenarioId));
            builder.Append(',');
            builder.Append(row.Point.Seed.ToString(CultureInfo.InvariantCulture));
            builder.Append(',');
            builder.Append(definition.Ticks.ToString(CultureInfo.InvariantCulture));
            builder.Append(',');
            builder.Append(Escape(definition.Side));
            builder.Append(',');
            builder.Append(Escape(row.Point.DoctrineId));
            builder.Append(',');
            builder.Append(Escape(row.Point.RoeId));
            builder.Append(',');
            builder.Append(Escape(row.Point.ForceRatioId));
            builder.Append(',');
            builder.Append(gridMethod);
            builder.Append(',');
            builder.Append(row.Score.Score.ToString(CultureInfo.InvariantCulture));
            builder.Append(',');
            builder.Append(row.Score.HostileKills.ToString(CultureInfo.InvariantCulture));
            builder.Append(',');
            builder.Append(row.Score.MissilesFired.ToString(CultureInfo.InvariantCulture));
            builder.Append(',');
            builder.Append(row.Score.PolicyDenials.ToString(CultureInfo.InvariantCulture));
            builder.Append(',');
            builder.Append(Escape(row.Result.Fingerprint));
            builder.Append(',');
            builder.Append(Escape(row.Result.FingerprintSha256));
            builder.Append(',');
            builder.Append(row.Result.WorldHash.ToString(CultureInfo.InvariantCulture));
            builder.Append(',');
            builder.Append(row.Result.DetectionWorldHash.ToString(CultureInfo.InvariantCulture));
            builder.Append(',');
            builder.Append(Escape(BuildId));
            builder.AppendLine();
        }

        return builder.ToString();
    }

    /// <summary>Runs <paramref name="definition"/> and writes CSV to <paramref name="csvOut"/> or stdout.</summary>
    public static int Execute(ExperimentDefinition definition, string? csvOut)
    {
        var rows = Run(definition);
        var csv = ExportCsv(rows, definition);
        if (csvOut != null)
        {
            File.WriteAllText(csvOut, csv);
            Console.WriteLine($"Wrote {rows.Count.ToString(CultureInfo.InvariantCulture)} rows to {csvOut}");
        }
        else
        {
            Console.Write(csv);
        }

        return 0;
    }

    private static void EnsureKnownScenario(ExperimentDefinition definition)
    {
        var known = BalticBatchRunner.DiscoverScenarioIds();
        foreach (var id in known)
        {
            if (string.Equals(id, definition.ScenarioId, StringComparison.Ordinal))
            {
                return;
            }
        }

        throw new ExperimentDefinitionException("Unknown scenario policy: " + definition.ScenarioId);
    }

    private static string GridMethodToken(ExperimentGridMethod method) => method switch
    {
        ExperimentGridMethod.Cartesian => "cartesian",
        ExperimentGridMethod.LatinHypercube => "latin-hypercube",
        _ => throw new ExperimentDefinitionException("Unsupported grid method."),
    };

    private static string ResolveBuildId()
    {
        var assembly = typeof(BalticReplayHarness).Assembly;
        var informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (!string.IsNullOrWhiteSpace(informational))
        {
            return informational;
        }

        return assembly.GetName().Version?.ToString() ?? "0.0.0";
    }

    private static string Escape(string? value)
    {
        value ??= "";
        value = value.Replace("\r\n", " ", StringComparison.Ordinal)
            .Replace('\n', ' ')
            .Replace('\r', ' ');
        if (value.Contains(',', StringComparison.Ordinal) || value.Contains('"', StringComparison.Ordinal))
        {
            return "\"" + value.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
        }

        return value;
    }
}
