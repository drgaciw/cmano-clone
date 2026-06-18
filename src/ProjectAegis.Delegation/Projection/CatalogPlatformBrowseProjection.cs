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
                    hasMobility ? mobility.MaxSpeedKnots : null,
                    mountCountById.GetValueOrDefault(id),
                    sensorCountById.GetValueOrDefault(id));
            })
            .ToArray();
    }
}