namespace ProjectAegis.Data.Platform;

using System.Globalization;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.WriteGate;

/// <summary>
/// Req-21 / ADR-011: pure, deterministic export of catalog rows to a <see cref="PlatformWorkbook"/>.
/// One sheet per entity domain, rows sorted by canonical keys, all cells formatted with
/// <see cref="CultureInfo.InvariantCulture"/>. The trailing <c>_Meta</c> sheet binds the workbook to its
/// source snapshot and carries a content hash so re-import can detect tampering and compute a diff.
/// No spreadsheet library, no wall clock (time injected via <see cref="ICatalogClock"/>).
/// </summary>
public sealed class PlatformWorkbookExporter
{
    public const string SchemaVersion = "007";

    public PlatformWorkbook Export(PlatformCatalogExportData data, string snapshotId, ICatalogClock clock)
    {
        if (data is null) throw new ArgumentNullException(nameof(data));
        if (clock is null) throw new ArgumentNullException(nameof(clock));

        var sheets = new List<PlatformWorkbookSheet>
        {
            BuildPlatforms(data.Platforms),
            BuildSensors(data.Sensors),
            BuildMounts(data.Mounts),
            BuildLoadouts(data.Loadouts),
            BuildMagazines(data.Magazines),
            BuildComms(data.Comms),
        };

        var withoutMeta = new PlatformWorkbook(sheets);
        var hash = PlatformWorkbookHash.Compute(withoutMeta);
        sheets.Add(BuildMeta(snapshotId, clock.UtcTicks, hash));
        return new PlatformWorkbook(sheets);
    }

    private static PlatformWorkbookSheet BuildPlatforms(IReadOnlyList<CatalogPlatformEntry> platforms)
    {
        var header = new[] { "PlatformId", "LatDeg", "LonDeg", "CombatRadiusNm" };
        var rows = platforms
            .OrderBy(p => p.PlatformId, StringComparer.Ordinal)
            .Select(p => (IReadOnlyList<string>)new[]
            {
                p.PlatformId,
                Num(p.LatDeg),
                Num(p.LonDeg),
                Num(p.CombatRadiusNm),
            })
            .ToArray();
        return new PlatformWorkbookSheet("Platforms", header, rows);
    }

    private static PlatformWorkbookSheet BuildSensors(IReadOnlyList<CatalogSensorBinding> sensors)
    {
        var header = new[] { "PlatformId", "SensorId", "BasePd", "ReviewState", "TrlLevel", "ValueTier", "CitationRef" };
        var rows = sensors
            .OrderBy(s => s.PlatformId, StringComparer.Ordinal)
            .ThenBy(s => s.SensorId, StringComparer.Ordinal)
            .Select(s => (IReadOnlyList<string>)new[]
            {
                s.PlatformId,
                s.SensorId,
                Num(s.BasePd),
                s.ReviewState,
                Int(s.TrlLevel),
                s.ValueTier,
                s.CitationRef,
            })
            .ToArray();
        return new PlatformWorkbookSheet("Sensors", header, rows);
    }

    private static PlatformWorkbookSheet BuildMounts(IReadOnlyList<CatalogMount> mounts)
    {
        var header = new[] { "PlatformId", "MountId", "MountType", "ArcDeg", "Capacity", "ReviewState" };
        var rows = mounts
            .OrderBy(m => m.PlatformId, StringComparer.Ordinal)
            .ThenBy(m => m.MountId, StringComparer.Ordinal)
            .Select(m => (IReadOnlyList<string>)new[]
            {
                m.PlatformId,
                m.MountId,
                m.MountType,
                Num(m.ArcDeg),
                Int(m.Capacity),
                m.ReviewState,
            })
            .ToArray();
        return new PlatformWorkbookSheet("Mounts", header, rows);
    }

    private static PlatformWorkbookSheet BuildLoadouts(IReadOnlyList<CatalogLoadout> loadouts)
    {
        var header = new[] { "PlatformId", "LoadoutId", "LoadoutName", "Role", "IsDefault" };
        var rows = loadouts
            .OrderBy(l => l.PlatformId, StringComparer.Ordinal)
            .ThenBy(l => l.LoadoutId, StringComparer.Ordinal)
            .Select(l => (IReadOnlyList<string>)new[]
            {
                l.PlatformId,
                l.LoadoutId,
                l.LoadoutName,
                l.Role,
                Bool(l.IsDefault),
            })
            .ToArray();
        return new PlatformWorkbookSheet("Loadouts", header, rows);
    }

    private static PlatformWorkbookSheet BuildMagazines(IReadOnlyList<CatalogMagazineEntry> magazines)
    {
        var header = new[] { "PlatformId", "LoadoutId", "MountId", "WeaponId", "Quantity", "ReloadTimeSec", "Depth" };
        var rows = magazines
            .OrderBy(m => m.PlatformId, StringComparer.Ordinal)
            .ThenBy(m => m.LoadoutId, StringComparer.Ordinal)
            .ThenBy(m => m.MountId, StringComparer.Ordinal)
            .ThenBy(m => m.WeaponId, StringComparer.Ordinal)
            .Select(m => (IReadOnlyList<string>)new[]
            {
                m.PlatformId,
                m.LoadoutId,
                m.MountId,
                m.WeaponId,
                Int(m.Quantity),
                Int(m.ReloadTimeSec),
                Int(m.Depth),
            })
            .ToArray();
        return new PlatformWorkbookSheet("Magazines", header, rows);
    }

    private static PlatformWorkbookSheet BuildComms(IReadOnlyList<CatalogCommsBinding> comms)
    {
        var header = new[] { "PlatformId", "LinkId", "Role", "SatcomCapable", "ReviewState", "TrlLevel", "ValueTier", "CitationRef" };
        var rows = comms
            .OrderBy(c => c.PlatformId, StringComparer.Ordinal)
            .ThenBy(c => c.LinkId, StringComparer.Ordinal)
            .Select(c => (IReadOnlyList<string>)new[]
            {
                c.PlatformId,
                c.LinkId,
                c.Role,
                Bool(c.SatcomCapable),
                c.ReviewState,
                Int(c.TrlLevel),
                c.ValueTier,
                c.CitationRef,
            })
            .ToArray();
        return new PlatformWorkbookSheet("Comms", header, rows);
    }

    private static PlatformWorkbookSheet BuildMeta(string snapshotId, long exportUtcTicks, string workbookHash)
    {
        var header = new[] { "Key", "Value" };
        var rows = new IReadOnlyList<string>[]
        {
            new[] { "SourceSnapshotId", snapshotId },
            new[] { "SchemaVersion", SchemaVersion },
            new[] { "ExportUtcTicks", Int64(exportUtcTicks) },
            new[] { "WorkbookHash", workbookHash },
        };
        return new PlatformWorkbookSheet(PlatformWorkbookHash.MetaSheetName, header, rows);
    }

    private static string Num(double value) => value.ToString("R", CultureInfo.InvariantCulture);

    private static string Int(int value) => value.ToString(CultureInfo.InvariantCulture);

    private static string Int64(long value) => value.ToString(CultureInfo.InvariantCulture);

    private static string Bool(bool value) => value ? "true" : "false";
}
