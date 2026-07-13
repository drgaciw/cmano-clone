namespace ProjectAegis.Delegation.Projection;

using System.Globalization;

/// <summary>ADR-011 Phase F: read-only list line labels for platform catalog browse rows.</summary>
public static class PlatformCatalogListProjection
{
    private const string Missing = "—";

    public static string FormatRow(CatalogPlatformBrowseRow row) =>
        $"{row.PlatformId} hp={FormatOptional(row.MaxHp)} res={FormatOptional(row.Resilience)} " +
        $"withdraw={FormatOptional(row.WithdrawThresholdPct)} flags={FormatOptionalInt(row.CriticalFlags)} " +
        $"speed={FormatOptional(row.MaxSpeedKnots)} mounts={row.MountCount} sensors={row.SensorCount}";

    private static string FormatOptional(double? value) =>
        value.HasValue ? value.Value.ToString("G", CultureInfo.InvariantCulture) : Missing;

    private static string FormatOptionalInt(int? value) =>
        value.HasValue ? value.Value.ToString(CultureInfo.InvariantCulture) : Missing;

    // S42-03 B1 Req16/21: magazine-augmented row formatting (projection-side loadout surfacing; uses external count lookup for deterministic display; cite boundary + S41 ack).
    public static string FormatRowWithMagazine(CatalogPlatformBrowseRow row, IReadOnlyDictionary<string, int> magazineCountsByPlatform) =>
        $"{FormatRow(row)} mags={magazineCountsByPlatform.GetValueOrDefault(row.PlatformId)}";
}