namespace ProjectAegis.Delegation.Projection;

using System.Globalization;
using ProjectAegis.Data.Catalog;

/// <summary>S40-03: read-only provenance labels for Catalog JSON import bindings (projection-side; no write path).</summary>
public sealed record CatalogImportProvenanceRow(string SummaryLine);

public static class CatalogImportProvenanceProjection
{
    public static string FormatBinding(CatalogSensorBinding binding) =>
        $"{binding.PlatformId}/{binding.SensorId} " +
        $"batch={FormatToken(binding.ImportBatchId)} file={FormatToken(binding.SourceFile)} " +
        $"source={FormatToken(binding.SourceFactId)} conf={FormatDouble(binding.Confidence)} " +
        $"trl={binding.TrlLevel} review={FormatToken(binding.ReviewState)}";

    public static IReadOnlyList<string> FormatBindings(IEnumerable<CatalogSensorBinding> bindings) =>
        bindings
            .OrderBy(b => b.PlatformId, StringComparer.Ordinal)
            .ThenBy(b => b.SensorId, StringComparer.Ordinal)
            .Select(FormatBinding)
            .ToArray();

    public static IReadOnlyList<string> ForPlatform(
        IEnumerable<CatalogSensorBinding> bindings,
        string platformId) =>
        bindings
            .Where(b => string.Equals(b.PlatformId, platformId, StringComparison.Ordinal))
            .OrderBy(b => b.SensorId, StringComparer.Ordinal)
            .Select(FormatBinding)
            .ToArray();

    private static string FormatToken(string value) =>
        string.IsNullOrWhiteSpace(value) ? "—" : value;

    private static string FormatDouble(double value) =>
        value.ToString("G", CultureInfo.InvariantCulture);
}
