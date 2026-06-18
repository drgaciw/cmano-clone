namespace ProjectAegis.Data.Platform;

using ProjectAegis.Data.Catalog;

/// <summary>
/// Req-21 Phase A/B: the catalog rows an exporter turns into a workbook. Decoupled from
/// <see cref="ICatalogReader"/> so the exporter is a pure function of its input and does not require
/// widening reader interfaces consumed by Sim/Delegation (run gitnexus_impact before doing that).
/// A later SQLite-backed provider populates this from the bound snapshot.
/// </summary>
public sealed record PlatformCatalogExportData(
    IReadOnlyList<CatalogPlatformEntry> Platforms,
    IReadOnlyList<CatalogSensorBinding> Sensors,
    IReadOnlyList<CatalogMount> Mounts,
    IReadOnlyList<CatalogLoadout> Loadouts,
    IReadOnlyList<CatalogMagazineEntry> Magazines,
    IReadOnlyList<CatalogCommsBinding> Comms,
    IReadOnlyList<CatalogMobility>? Mobility = null,
    IReadOnlyList<CatalogSignature>? Signatures = null,
    IReadOnlyList<CatalogEmcon>? Emcon = null)
{
    public static PlatformCatalogExportData Empty { get; } = new(
        [],
        [],
        [],
        [],
        [],
        [],
        [],
        [],
        []);
}
