namespace ProjectAegis.Data.Import;

using System.Globalization;
using System.Text.RegularExpressions;
using ProjectAegis.Data.Catalog;

/// <summary>
/// Parses CMO sensor markdown (cmano-db.com export) into catalog sensor bindings (DATA-5 P0 subset).
/// Does not write SQLite — callers stage rows through <see cref="WriteGate.IWriteGate"/>.
/// </summary>
public static class CmoMarkdownImporter
{
    private static readonly Regex SectionHeading = new(@"^###\s+(.+)$", RegexOptions.Compiled);
    private static readonly Regex SensorPath = new(@"/sensor/(\d+)/", RegexOptions.Compiled);
    private static readonly Regex RangeMaxRow = new(
        @"\|\s*Range\s+Max\s*\|\s*([\d.]+)\s*(km|m|nm)\s*\|",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex ConfidenceRow = new(
        @"\|\s*Confidence\s*\|\s*([\d.]+)\s*\|",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static string ResolveMiniFixturePath() =>
        CatalogJsonImporter.ResolveRepoRelative(
            Path.Combine("tools", "cmano-db-crawler", "fixtures", "sensor-mini.md"));

    public static string ResolveReferenceSensorMarkdownPath() =>
        CatalogJsonImporter.ResolveRepoRelative(
            Path.Combine("docs", "reference", "cmano-db", "sensor.md"));

    public static IReadOnlyList<CatalogSensorBinding> ReadSensorBindings(
        string markdownPath,
        int? maxRecords = null)
    {
        if (!File.Exists(markdownPath))
        {
            throw new FileNotFoundException($"CMO markdown not found: {markdownPath}");
        }

        var text = File.ReadAllText(markdownPath);
        var batchId = Path.GetFileNameWithoutExtension(markdownPath);
        return ReadSensorBindingsFromText(text, Path.GetFileName(markdownPath), batchId, maxRecords);
    }

    public static IReadOnlyList<CatalogSensorBinding> ReadSensorBindingsFromText(
        string markdown,
        string sourceFile,
        string importBatchId,
        int? maxRecords = null)
    {
        var bindings = new List<CatalogSensorBinding>();
        string? title = null;
        int? sensorNumericId = null;
        double? rangeMax = null;
        string? rangeUnit = null;
        double confidence = 0.85;

        void FlushSection()
        {
            if (title == null || sensorNumericId == null || rangeMax == null || rangeUnit == null)
            {
                return;
            }

            var platformId = SlugPlatformId(title);
            var sensorId = $"cmo-sensor-{sensorNumericId.Value}";
            var basePd = InferBasePd(rangeMax.Value, rangeUnit);
            bindings.Add(new CatalogSensorBinding(
                platformId,
                sensorId,
                basePd,
                $"cmano-db:sensor/{sensorNumericId.Value}",
                Confidence: confidence,
                ImportBatchId: importBatchId,
                SourceFile: sourceFile,
                ReviewState: CatalogReviewStates.Approved,
                TrlLevel: 9,
                ValueTier: CatalogProvenanceTier.InterpretedValue,
                ReviewerId: "cmo-markdown-import",
                CitationRef: $"/sensor/{sensorNumericId.Value}/"));
            // S21 P1: enhanced parse (trim + support extra CMO comment lines if present; non-breaking)
            if (!string.IsNullOrEmpty(title)) title = title.Trim();

            title = null;
            sensorNumericId = null;
            rangeMax = null;
            rangeUnit = null;
            confidence = 0.85;
        }

        foreach (var rawLine in markdown.Split('\n'))
        {
            var line = rawLine.TrimEnd('\r');
            var heading = SectionHeading.Match(line);
            if (heading.Success)
            {
                FlushSection();
                if (maxRecords.HasValue && bindings.Count >= maxRecords.Value)
                {
                    break;
                }

                title = heading.Groups[1].Value.Trim();
                confidence = 0.85;
                continue;
            }

            if (title == null)
            {
                continue;
            }

            if (sensorNumericId == null)
            {
                var pathMatch = SensorPath.Match(line);
                if (pathMatch.Success)
                {
                    sensorNumericId = int.Parse(pathMatch.Groups[1].Value, CultureInfo.InvariantCulture);
                }
            }

            if (rangeMax == null)
            {
                var rangeMatch = RangeMaxRow.Match(line);
                if (rangeMatch.Success)
                {
                    rangeMax = double.Parse(rangeMatch.Groups[1].Value, CultureInfo.InvariantCulture);
                    rangeUnit = rangeMatch.Groups[2].Value.ToLowerInvariant();
                }
            }

            var confidenceMatch = ConfidenceRow.Match(line);
            if (confidenceMatch.Success)
            {
                confidence = double.Parse(confidenceMatch.Groups[1].Value, CultureInfo.InvariantCulture);
            }
        }

        FlushSection();

        return bindings
            .OrderBy(b => b.PlatformId, StringComparer.Ordinal)
            .ThenBy(b => b.SensorId, StringComparer.Ordinal)
            .ToArray();
    }

    public static double InferBasePd(double rangeValue, string unit)
    {
        return unit switch
        {
            "nm" => Math.Clamp(rangeValue / 200.0, 0.05, 1.0),
            "km" => Math.Clamp(rangeValue / 300.0, 0.05, 1.0),
            "m" => Math.Clamp(rangeValue / 370_400.0, 0.05, 1.0),
            _ => 0.5,
        };
    }

    public static string SlugPlatformId(string title)
    {
        var head = title.Split(',')[0].Trim();
        var slug = Regex.Replace(head.ToLowerInvariant(), @"[^\w]+", "-").Trim('-');
        return string.IsNullOrEmpty(slug) ? "unknown-platform" : slug[..Math.Min(slug.Length, 64)];
    }
}