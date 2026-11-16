using System.Globalization;
using System.Text;

namespace ProjectAegis.Delegation.Demo;

/// <summary>
/// Deterministic experiment-run report over <see cref="ExperimentSeedGridBatch"/> rows.
/// Doctrine, ROE, and force-ratio IDs label rows only and do not change simulated behaviour.
/// </summary>
public static class ExperimentBatchReport
{
    /// <summary>Plain-language disclaimer required on every report.</summary>
    public const string LabelDisclaimer =
        "Doctrine, ROE, and force-ratio IDs label rows without changing simulated behaviour.";

    /// <summary>
    /// Builds a plain-text report describing observed run differences.
    /// Does not invoke providers and does not mutate batch fingerprints.
    /// </summary>
    public static string Build(IReadOnlyList<ExperimentBatchRow> rows, ExperimentDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(rows);
        ArgumentNullException.ThrowIfNull(definition);

        var sb = new StringBuilder();
        sb.AppendLine("ExperimentSeedGridBatch report");
        sb.AppendLine("scenarioId=" + definition.ScenarioId);
        sb.AppendLine("ticks=" + definition.Ticks.ToString(CultureInfo.InvariantCulture));
        sb.AppendLine("side=" + definition.Side);
        sb.AppendLine("rows=" + rows.Count.ToString(CultureInfo.InvariantCulture));
        sb.AppendLine(LabelDisclaimer);
        sb.AppendLine("processIsolation=required-until-static-state-audited");

        if (rows.Count == 0)
        {
            sb.AppendLine("observedDifferences=none");
            return sb.ToString();
        }

        var fingerprints = rows.Select(r => r.Result.FingerprintSha256).Distinct(StringComparer.Ordinal).ToArray();
        sb.AppendLine("distinctFingerprints=" + fingerprints.Length.ToString(CultureInfo.InvariantCulture));
        foreach (var fp in fingerprints)
        {
            sb.AppendLine("fingerprint=" + fp);
        }

        var byDoctrine = rows.GroupBy(r => r.Point.DoctrineId, StringComparer.Ordinal).ToArray();
        foreach (var group in byDoctrine)
        {
            var scores = group.Select(r => r.Score.Score).Distinct().OrderBy(s => s).ToArray();
            sb.AppendLine(
                "label.doctrineId=" + group.Key
                + "; rows=" + group.Count().ToString(CultureInfo.InvariantCulture)
                + "; scores=" + string.Join('|', scores.Select(s => s.ToString(CultureInfo.InvariantCulture))));
        }

        var byRoe = rows.GroupBy(r => r.Point.RoeId, StringComparer.Ordinal).ToArray();
        foreach (var group in byRoe)
        {
            sb.AppendLine(
                "label.roeId=" + group.Key
                + "; rows=" + group.Count().ToString(CultureInfo.InvariantCulture));
        }

        var byForce = rows.GroupBy(r => r.Point.ForceRatioId, StringComparer.Ordinal).ToArray();
        foreach (var group in byForce)
        {
            sb.AppendLine(
                "label.forceRatioId=" + group.Key
                + "; rows=" + group.Count().ToString(CultureInfo.InvariantCulture));
        }

        return sb.ToString();
    }
}
