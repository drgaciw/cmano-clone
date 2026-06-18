using System.Text.Json;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Platform;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.MissionEditor.Cli;
using Xunit;

namespace ProjectAegis.MissionEditor.Cli.Tests;

public sealed class CatalogPlatformBrowseCommandTests
{
    [Fact]
    public void catalog_platform_browse_emits_json_with_row_count()
    {
        using var writer = new StringWriter();
        Assert.Equal(0, CatalogPlatformBrowseCommand.Run(dbPath: null, writer, maxRecords: 5));

        var json = writer.ToString();
        Assert.Contains("\"rowCount\":", json);
        Assert.Contains("\"rows\":", json);
    }

    [Fact]
    public void catalog_platform_browse_emits_schema_version_2_with_mount_and_sensor_counts()
    {
        using var writer = new StringWriter();
        Assert.Equal(0, CatalogPlatformBrowseCommand.Run(dbPath: null, writer, maxRecords: 5));

        using var doc = JsonDocument.Parse(writer.ToString());
        var root = doc.RootElement;

        Assert.Equal(2, root.GetProperty("schemaVersion").GetInt32());
        Assert.True(root.TryGetProperty("rows", out var rows));
        Assert.Equal(JsonValueKind.Array, rows.ValueKind);

        var browseRow = CatalogPlatformBrowseProjection.FromExportData(new PlatformCatalogExportData(
            Platforms: [new CatalogPlatformEntry("a-platform", 54.0, 17.0, 80)],
            Sensors: [new CatalogSensorBinding("a-platform", "radar-1", 0.5)],
            Mounts: [new CatalogMount("a-platform", "mount-1")],
            Loadouts: [],
            Magazines: [],
            Comms: [])).Single();

        var rowJson = JsonSerializer.Serialize(new
        {
            browseRow.PlatformId,
            browseRow.LatDeg,
            browseRow.LonDeg,
            browseRow.CombatRadiusNm,
            browseRow.MaxHp,
            browseRow.MaxSpeedKnots,
            mountCount = browseRow.MountCount,
            sensorCount = browseRow.SensorCount,
        });

        using var rowDoc = JsonDocument.Parse(rowJson);
        Assert.Equal(1, rowDoc.RootElement.GetProperty("mountCount").GetInt32());
        Assert.Equal(1, rowDoc.RootElement.GetProperty("sensorCount").GetInt32());
    }
}