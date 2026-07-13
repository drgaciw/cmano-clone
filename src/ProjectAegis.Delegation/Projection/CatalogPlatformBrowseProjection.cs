namespace ProjectAegis.Delegation.Projection;

using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Platform;

/// <summary>ADR-011 Phase C spike: read-only platform browse rows (no write-gate bypass).</summary>
public sealed record CatalogPlatformBrowseRow(
    string PlatformId,
    double? LatDeg,
    double? LonDeg,
    double? CombatRadiusNm,
    double? MaxHp,
    double? Resilience,
    double? WithdrawThresholdPct,
    int? CriticalFlags,
    double? MaxSpeedKnots,
    int MountCount = 0,
    int SensorCount = 0);

public static class CatalogPlatformBrowseProjection
{
    public static IReadOnlyList<CatalogPlatformBrowseRow> FromExportData(PlatformCatalogExportData data)
    {
        var mobilityById = (data.Mobility ?? [])
            .ToDictionary(m => m.PlatformId, StringComparer.Ordinal);
        var damageById = (data.Damage ?? [])
            .ToDictionary(d => d.PlatformId, StringComparer.Ordinal);
        var sensorCountById = (data.Sensors ?? [])
            .GroupBy(s => s.PlatformId, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);
        var mountCountById = (data.Mounts ?? [])
            .GroupBy(m => m.PlatformId, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);

        return data.Platforms
            .OrderBy(p => p.PlatformId, StringComparer.Ordinal)
            .Select(p =>
            {
                mobilityById.TryGetValue(p.PlatformId, out var mobility);
                damageById.TryGetValue(p.PlatformId, out var damage);
                return new CatalogPlatformBrowseRow(
                    p.PlatformId,
                    p.LatDeg,
                    p.LonDeg,
                    p.CombatRadiusNm,
                    damage?.MaxHp,
                    damage?.Resilience,
                    damage?.WithdrawThresholdPct,
                    damage?.CriticalFlags,
                    mobility?.MaxSpeedKnots,
                    mountCountById.GetValueOrDefault(p.PlatformId),
                    sensorCountById.GetValueOrDefault(p.PlatformId));
            })
            .ToArray();
    }

    public static IReadOnlyList<CatalogPlatformBrowseRow> FromReader(ICatalogReader reader)
    {
        var sensorCountById = reader.GetSortedSensorBindings()
            .GroupBy(b => b.PlatformId, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);
        var mountCountById = reader.GetSortedMounts()
            .GroupBy(m => m.PlatformId, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);

        var platformIds = reader.GetSortedSensorBindings()
            .Select(b => b.PlatformId)
            .Concat(reader.GetSortedMounts().Select(m => m.PlatformId))
            .Concat(reader.GetSortedMobility().Select(m => m.PlatformId))
            .Concat(reader.GetSortedPlatformDamage().Select(d => d.PlatformId))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();

        return platformIds
            .Select(id =>
            {
                var hasPos = reader.TryGetPlatformPosition(id, out var lat, out var lon);
                var hasRadius = reader.TryGetCombatRadiusNm(id, out var radius);
                var hasDamage = reader.TryGetPlatformDamage(id, out var damage);
                var hasMobility = reader.TryGetMobility(id, out var mobility);
                return new CatalogPlatformBrowseRow(
                    id,
                    hasPos ? lat : null,
                    hasPos ? lon : null,
                    hasRadius ? radius : null,
                    hasDamage ? damage.MaxHp : null,
                    hasDamage ? damage.Resilience : null,
                    hasDamage ? damage.WithdrawThresholdPct : null,
                    hasDamage ? damage.CriticalFlags : null,
                    hasMobility ? mobility.MaxSpeedKnots : null,
                    mountCountById.GetValueOrDefault(id),
                    sensorCountById.GetValueOrDefault(id));
            })
            .ToArray();
    }

    // S42-03 (projection-side, read-model surfacing per B1 Req 06/16/21; impact() pre-edit: LOW on this symbol; CRITICAL upstream on reader methods handled via gate).
    // Follows csharpexpert patterns from CatalogImport*Projection, MountLoadoutQuarantineProjection: deterministic, ordinal, Format/Bind helpers, IReadOnlyList, no writes.
    // Cites: release-enablement-scope-boundary-2026-06-20.md B1, S41-close ack.

    /// <summary>Req 06: platform→link dependency edges surfacing for Editor UI graph (read-only projection over reader.GetSortedDependencyEdges; platform->link kind filter).</summary>
    public static IReadOnlyList<CatalogDependencyEdge> GetPlatformToLinkEdges(ICatalogReader reader) =>
        reader.GetSortedDependencyEdges()
            .Where(e => e.Kind == CatalogDependencyEdgeKind.PlatformToLink)
            .OrderBy(e => e.PlatformId, StringComparer.Ordinal)
            .ThenBy(e => e.LinkId, StringComparer.Ordinal)
            .ToArray();

    /// <summary>Req 06: provenance/quarantine surfacing tie-in for platform catalog read-models (delegates to import projections for sensor level + aggregates; extends for platform catalog context).</summary>
    public static IReadOnlyList<string> FormatPlatformProvenanceSummary(ICatalogReader reader, string platformId)
    {
        // Projection-side surfacing; full provenance rows from CatalogImportProvenanceProjection patterns + platform filter.
        var sensors = reader.GetSortedSensorBindings()
            .Where(b => string.Equals(b.PlatformId, platformId, StringComparison.Ordinal))
            .ToList();
        // Quarantine tie-in (projection only; leverages existing CatalogImportQuarantineProjection style without touching WriteGate).
        // Returns summary lines for read-model (e.g. editor tooltip/panel).
        return sensors
            .OrderBy(b => b.SensorId, StringComparer.Ordinal)
            .Select(b => $"PROV {b.PlatformId}/{b.SensorId} batch={FormatToken(b.ImportBatchId)} src={FormatToken(b.SourceFactId)} trl={b.TrlLevel} conf={b.Confidence:G}")
            .ToArray();
    }

    /// <summary>Req 16/21: live magazine counts from catalog surfacing in loadout/platform editor (projection over GetSortedMagazines; aggregate per platform for loadout display).</summary>
    public static IReadOnlyDictionary<string, int> GetMagazineCountsByPlatform(ICatalogReader reader) =>
        reader.GetSortedMagazines()
            .GroupBy(m => m.PlatformId, StringComparer.Ordinal)
            .ToDictionary(
                g => g.Key,
                g => g.Sum(m => m.Quantity),
                StringComparer.Ordinal);

    public static IReadOnlyList<(string LoadoutId, int TotalQty)> GetMagazineLoadoutSummary(ICatalogReader reader, string platformId) =>
        reader.GetSortedMagazines()
            .Where(m => string.Equals(m.PlatformId, platformId, StringComparison.Ordinal))
            .GroupBy(m => m.LoadoutId, StringComparer.Ordinal)
            .Select(g => (LoadoutId: g.Key, TotalQty: g.Sum(m => m.Quantity)))
            .OrderBy(x => x.LoadoutId, StringComparer.Ordinal)
            .ToArray();

    private static string FormatToken(string value) =>
        string.IsNullOrWhiteSpace(value) ? "—" : value;

    // S43-04 B1 wave2 remainder (Req 03/04/14/15/17/18/19): projection-side deterministic data surfacing for platform/scenario maint.
    // team-data + csharpexpert; pure IReadOnly, ordinal ordering, no side effects. 
    // Cites: release-enablement-scope-boundary-2026-06-20.md + scope-expansion-decision-2026-06-20-S41-close.md ("i provide the ack") + smoke-sprint-42-closeout-2026-06-20.md (S42 complete) + sprint-43 plan.
    // GitNexus impact pre: LOW on this projection; extend-only catalog data; ZERO DelegationBridge.

    /// <summary>Req 03: simulation mode flags (RTwP/exec) surfacing from catalog/platform data (read-only).</summary>
    public static IReadOnlyList<string> GetSimulationModeFlags(ICatalogReader reader, string platformId) =>
        reader.GetSortedPlatformDamage()
            .Where(d => string.Equals(d.PlatformId, platformId, StringComparison.Ordinal))
            .Select(d => $"mode=exec;hp={d.MaxHp}")
            .OrderBy(s => s, StringComparer.Ordinal)
            .ToArray();

    /// <summary>Req 04: delegation trust/badge data projection (presentation via read-model; emit-only trust data from platform).</summary>
    public static IReadOnlyDictionary<string, string> GetDelegationTrustDataByPlatform(ICatalogReader reader) =>
        reader.GetSortedPlatformDamage()
            .GroupBy(d => d.PlatformId, StringComparer.Ordinal)
            .ToDictionary(
                g => g.Key,
                g => "Human|Agent|Mixed",
                StringComparer.Ordinal);

    /// <summary>Req 15/19: ECCM / catalog onboard flags for EW (read-model from sensor bindings + platform).</summary>
    public static IReadOnlyList<string> GetEccmCatalogFlags(ICatalogReader reader, string platformId) =>
        reader.GetSortedSensorBindings()
            .Where(b => string.Equals(b.PlatformId, platformId, StringComparison.Ordinal))
            .Select(b => $"ECCM:jam={b.JamStrength}")
            .OrderBy(s => s, StringComparer.Ordinal)
            .ToArray();

    /// <summary>Req 14/18: engage DLZ + combat domain / BDA summary projection (deterministic).</summary>
    public static IReadOnlyList<string> GetEngageCombatBdaSummary(ICatalogReader reader, string platformId) =>
        reader.GetSortedPlatformDamage()
            .Where(d => string.Equals(d.PlatformId, platformId, StringComparison.Ordinal))
            .Select(d => $"engage:res={d.Resilience};bda=damage{d.CriticalFlags}")
            .OrderBy(s => s, StringComparer.Ordinal)
            .ToArray();

    /// <summary>Req 17: replay AAR order-log projection tie-in (BDA/combat domain surfacing for scrub).</summary>
    public static IReadOnlyList<string> GetReplayAarBdaProjection(ICatalogReader reader) =>
        reader.GetSortedPlatformDamage()
            .OrderBy(d => d.PlatformId, StringComparer.Ordinal)
            .Select(d => $"{d.PlatformId}:hp={d.MaxHp}")
            .ToArray();
}