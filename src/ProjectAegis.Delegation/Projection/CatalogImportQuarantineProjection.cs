namespace ProjectAegis.Delegation.Projection;

using System.Globalization;
using ProjectAegis.Data.Catalog;

/// <summary>S40-03: read-only quarantine surfacing for CatalogJsonImporter / CatalogImportGate outcomes.</summary>
public sealed record CatalogImportQuarantinePanelState(
    string StatusLine,
    IReadOnlyList<string> QuarantineLines,
    int ApprovedCount,
    int QuarantinedCount,
    bool HasQuarantine);

public static class CatalogImportQuarantineProjection
{
    public static string FormatRow(QuarantinedCatalogBinding row)
    {
        var binding = row.Binding;
        return
            $"QUARANTINE {binding.PlatformId}/{binding.SensorId} " +
            $"reason={row.RejectionReason} batch={FormatToken(binding.ImportBatchId)} " +
            $"review={FormatToken(binding.ReviewState)} trl={binding.TrlLevel} " +
            $"conf={FormatDouble(binding.Confidence)}";
    }

    public static IReadOnlyList<string> FormatRows(IEnumerable<QuarantinedCatalogBinding> rows) =>
        rows
            .OrderBy(r => r.Binding.PlatformId, StringComparer.Ordinal)
            .ThenBy(r => r.Binding.SensorId, StringComparer.Ordinal)
            .Select(FormatRow)
            .ToArray();

    public static CatalogImportQuarantinePanelState BindPartition(
        IReadOnlyList<CatalogSensorBinding> approved,
        IReadOnlyList<QuarantinedCatalogBinding> quarantined)
    {
        var quarantineLines = FormatRows(quarantined);
        var status = quarantined.Count == 0
            ? $"IMPORT: {approved.Count} approved sensor binding(s); quarantine empty"
            : $"IMPORT: {approved.Count} approved, {quarantined.Count} quarantined — review before promote";

        return new CatalogImportQuarantinePanelState(
            status,
            quarantineLines,
            approved.Count,
            quarantined.Count,
            quarantined.Count > 0);
    }

    public static CatalogImportQuarantinePanelState BindPartition(
        IEnumerable<CatalogSensorBinding> bindings) =>
        BindPartition(CatalogImportGate.PartitionForImport(bindings));

    public static CatalogImportQuarantinePanelState BindPartition(
        (CatalogSensorBinding[] Approved, QuarantinedCatalogBinding[] Quarantined) partition) =>
        BindPartition(partition.Approved, partition.Quarantined);

    private static string FormatToken(string value) =>
        string.IsNullOrWhiteSpace(value) ? "—" : value;

    private static string FormatDouble(double value) =>
        value.ToString("G", CultureInfo.InvariantCulture);
}
