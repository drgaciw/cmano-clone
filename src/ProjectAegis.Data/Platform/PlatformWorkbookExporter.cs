namespace ProjectAegis.Data.Platform;

using System.Globalization;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Snapshots;
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
    public const string SchemaVersion = "010";

    public PlatformWorkbook Export(
        PlatformCatalogExportData data,
        string snapshotId,
        ICatalogClock clock,
        CatalogExportManifest? manifest = null)
    {
        if (data is null) throw new ArgumentNullException(nameof(data));
        if (clock is null) throw new ArgumentNullException(nameof(clock));

        var resolvedManifest = manifest ?? CatalogExportManifest.DefaultForSnapshot(snapshotId);
        var sheets = new List<PlatformWorkbookSheet>
        {
            BuildPlatforms(data.Platforms, data.Damage),
            BuildSensors(data.Sensors),
            BuildMounts(data.Mounts),
            BuildLoadouts(data.Loadouts),
            BuildMagazines(data.Magazines),
            BuildComms(data.Comms),
            BuildLinkCatalog(data.Links ?? []),
            BuildMobility(data.Mobility ?? []),
            BuildSignatures(data.Signatures ?? []),
            BuildEmcon(data.Emcon ?? []),
        };

        var withoutMeta = new PlatformWorkbook(sheets);
        var hash = PlatformWorkbookHash.Compute(withoutMeta);
        sheets.Add(BuildMeta(snapshotId, clock.UtcTicks, hash, resolvedManifest));
        return new PlatformWorkbook(sheets);
    }

    private static PlatformWorkbookSheet BuildPlatforms(
        IReadOnlyList<CatalogPlatformEntry> platforms,
        IReadOnlyList<CatalogPlatformDamage>? damage = null)
    {
        var damageByPlatform = damage is null
            ? new Dictionary<string, CatalogPlatformDamage>(StringComparer.Ordinal)
            : damage.ToDictionary(d => d.PlatformId, StringComparer.Ordinal);
        var header = new[]
        {
            "PlatformId", "LatDeg", "LonDeg", "CombatRadiusNm",
            "MaxHp", "WithdrawThresholdPct", "CriticalFlags",
        };
        var rows = platforms
            .OrderBy(p => p.PlatformId, StringComparer.Ordinal)
            .Select(p =>
            {
                damageByPlatform.TryGetValue(p.PlatformId, out var d);
                return (IReadOnlyList<string>)new[]
                {
                    p.PlatformId,
                    Num(p.LatDeg),
                    Num(p.LonDeg),
                    Num(p.CombatRadiusNm),
                    Num(d?.MaxHp ?? 100),
                    Num(d?.WithdrawThresholdPct ?? 0),
                    Int(d?.CriticalFlags ?? 0),
                };
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

    private static PlatformWorkbookSheet BuildLinkCatalog(IReadOnlyList<CatalogLinkEntry> links)
    {
        var header = new[] { "LinkId", "DisplayName", "LinkType", "LatencyMsNominal" };
        var rows = links
            .OrderBy(l => l.LinkId, StringComparer.Ordinal)
            .Select(l => (IReadOnlyList<string>)new[]
            {
                l.LinkId,
                l.DisplayName,
                l.LinkType,
                Int(l.LatencyMsNominal),
            })
            .ToArray();
        return new PlatformWorkbookSheet("LinkCatalog", header, rows);
    }

    private static PlatformWorkbookSheet BuildMobility(IReadOnlyList<CatalogMobility> mobility)
    {
        var header = new[]
        {
            "PlatformId", "MaxSpeedKnots", "CruiseSpeedKnots", "MaxAltitudeFt", "MaxDepthM",
            "FuelCapacity", "RangeNm", "EnduranceHr",
        };
        var rows = mobility
            .OrderBy(m => m.PlatformId, StringComparer.Ordinal)
            .Select(m => (IReadOnlyList<string>)new[]
            {
                m.PlatformId,
                Num(m.MaxSpeedKnots),
                Num(m.CruiseSpeedKnots),
                Num(m.MaxAltitudeFt),
                Num(m.MaxDepthM),
                Num(m.FuelCapacity),
                Num(m.RangeNm),
                Num(m.EnduranceHr),
            })
            .ToArray();
        return new PlatformWorkbookSheet("Mobility", header, rows);
    }

    private static PlatformWorkbookSheet BuildSignatures(IReadOnlyList<CatalogSignature> signatures)
    {
        var header = new[] { "PlatformId", "RcsBandDbsm", "IrSignature", "AcousticSignatureDb", "MagneticSignature" };
        var rows = signatures
            .OrderBy(s => s.PlatformId, StringComparer.Ordinal)
            .Select(s => (IReadOnlyList<string>)new[]
            {
                s.PlatformId,
                Num(s.RcsBandDbsm),
                Num(s.IrSignature),
                Num(s.AcousticSignatureDb),
                Num(s.MagneticSignature),
            })
            .ToArray();
        return new PlatformWorkbookSheet("Signatures", header, rows);
    }

    private static PlatformWorkbookSheet BuildEmcon(IReadOnlyList<CatalogEmcon> emcon)
    {
        var header = new[] { "PlatformId", "Condition", "EmitterId", "Posture" };
        var rows = emcon
            .OrderBy(e => e.PlatformId, StringComparer.Ordinal)
            .ThenBy(e => e.Condition, StringComparer.Ordinal)
            .ThenBy(e => e.EmitterId, StringComparer.Ordinal)
            .Select(e => (IReadOnlyList<string>)new[]
            {
                e.PlatformId,
                e.Condition,
                e.EmitterId,
                e.Posture,
            })
            .ToArray();
        return new PlatformWorkbookSheet("Emcon", header, rows);
    }

    private static PlatformWorkbookSheet BuildMeta(
        string snapshotId,
        long exportUtcTicks,
        string workbookHash,
        CatalogExportManifest manifest)
    {
        var header = new[] { "Key", "Value" };
        var rows = new IReadOnlyList<string>[]
        {
            new[] { "SourceSnapshotId", snapshotId },
            new[] { "SchemaVersion", SchemaVersion },
            new[] { "ExportUtcTicks", Int64(exportUtcTicks) },
            new[] { "WorkbookHash", workbookHash },
            new[] { "DbVersion", manifest.DbVersion },
            new[] { "TlTier", manifest.TlTier },
            new[] { "CatalogSchemaVersion", manifest.SchemaVersion },
            new[] { "ContentHash", manifest.ContentHash },
            new[] { "ExportSchemaVersion", manifest.ExportSchemaVersion },
        };
        return new PlatformWorkbookSheet(PlatformWorkbookHash.MetaSheetName, header, rows);
    }

    private static string Num(double value) => value.ToString("R", CultureInfo.InvariantCulture);

    private static string Int(int value) => value.ToString(CultureInfo.InvariantCulture);

    private static string Int64(long value) => value.ToString(CultureInfo.InvariantCulture);

    private static string Bool(bool value) => value ? "true" : "false";

    /// <summary>Serializes an exported workbook through the selected <see cref="IPlatformWorkbookIo"/> adapter.</summary>
    public void WriteToFile(PlatformWorkbook workbook, string path, IPlatformWorkbookIo io)
    {
        if (workbook is null) throw new ArgumentNullException(nameof(workbook));
        if (io is null) throw new ArgumentNullException(nameof(io));
        io.Write(workbook, path);
    }

    /// <summary>Loads a workbook from disk via the selected <see cref="IPlatformWorkbookIo"/> adapter.</summary>
    public PlatformWorkbook ReadFromFile(string path, IPlatformWorkbookIo io)
    {
        if (io is null) throw new ArgumentNullException(nameof(io));
        return io.Read(path);
    }
}
