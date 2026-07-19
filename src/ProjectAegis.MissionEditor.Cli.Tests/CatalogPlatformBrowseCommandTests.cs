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

    [Fact]
    public void catalog_platform_browse_with_baltic_db_and_null_snapshot_returns_platform_rows()
    {
        // UAT 2026-07-19 P0: browse against real DB with omitted snapshot returned rowCount=0.
        // Default snapshot must resolve to baltic_patrol and emit platforms (surface/air/sub).
        var dbPath = ResolveBalticCatalogDb();
        if (dbPath is null)
        {
            return; // environment without fixture DB — skip silently
        }

        Assert.True(PlatformCatalogExportResolver.TryResolve(dbPath, snapshotId: null, out var data));
        Assert.True(data.Platforms.Count >= 50, $"expected Baltic platforms, got {data.Platforms.Count}");

        using var writer = new StringWriter();
        Assert.Equal(0, CatalogPlatformBrowseCommand.Run(dbPath, writer, maxRecords: 20));
        using var doc = JsonDocument.Parse(writer.ToString());
        Assert.Equal(20, doc.RootElement.GetProperty("rowCount").GetInt32());
        Assert.Equal(20, doc.RootElement.GetProperty("rows").GetArrayLength());
    }

    [Fact]
    public void platform_export_xlsx_without_snapshot_resolves_baltic_and_is_nonempty()
    {
        var dbPath = ResolveBalticCatalogDb();
        if (dbPath is null)
        {
            return;
        }

        var outPath = Path.Combine(Path.GetTempPath(), $"aegis-export-{Guid.NewGuid():N}.xlsx");
        try
        {
            using var writer = new StringWriter();
            // Empty snapshot id → default Baltic (not cli-s22-export silent empty).
            var exit = PlatformExportXlsxCommand.Run(
                dbPath,
                outPath,
                snapshotId: string.Empty,
                ioFlag: "canonical",
                writer);
            Assert.Equal(0, exit);
            using var doc = JsonDocument.Parse(writer.ToString());
            Assert.True(doc.RootElement.GetProperty("ok").GetBoolean());
            Assert.True(doc.RootElement.GetProperty("snapshotResolved").GetBoolean());
            Assert.Equal("baltic_patrol", doc.RootElement.GetProperty("snapshotId").GetString());
            Assert.True(doc.RootElement.GetProperty("platformCount").GetInt32() >= 50);
            Assert.True(File.Exists(outPath));
            Assert.True(new FileInfo(outPath).Length > 10_000);
        }
        finally
        {
            if (File.Exists(outPath))
            {
                File.Delete(outPath);
            }
        }
    }

    private static string? ResolveBalticCatalogDb()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "assets", "data", "catalog", "baltic_patrol.db");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        return null;
    }
}