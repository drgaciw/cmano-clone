using System.Text.Json;
using ProjectAegis.Data.Platform;
using ProjectAegis.Delegation.Projection;

namespace ProjectAegis.MissionEditor.Cli;

/// <summary>ADR-011 Phase C: read-only browse of platform catalog rows (no edit path).</summary>
public static class CatalogPlatformBrowseCommand
{
    public static int Run(string? dbPath, TextWriter writer, int? maxRecords = null)
    {
        if (!PlatformCatalogExportResolver.TryResolve(dbPath, snapshotId: null, out var data))
        {
            data = PlatformCatalogExportData.Empty;
        }

        var rows = CatalogPlatformBrowseProjection.FromExportData(data);
        if (maxRecords is > 0)
        {
            rows = rows.Take(maxRecords.Value).ToArray();
        }

        var payload = new
        {
            schemaVersion = 2,
            rowCount = rows.Count,
            rows = rows.Select(r => new
            {
                r.PlatformId,
                r.LatDeg,
                r.LonDeg,
                r.CombatRadiusNm,
                r.MaxHp,
                r.MaxSpeedKnots,
                mountCount = r.MountCount,
                sensorCount = r.SensorCount,
            }),
        };

        writer.Write(JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));
        return 0;
    }
}