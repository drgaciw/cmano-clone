using System.Text.Json;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Platform;
using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.MissionEditor.Cli.Tests;

[TestFixture]
public sealed class CatalogPlatformBrowseCommandTests
{
    [Test]
    public void catalog_platform_browse_emits_json_with_row_count()
    {
        using var writer = new StringWriter();
        Assert.That(CatalogPlatformBrowseCommand.Run(dbPath: null, writer, maxRecords: 5), Is.EqualTo(0));

        var json = writer.ToString();
        Assert.That(json, Does.Contain("\"rowCount\":"));
        Assert.That(json, Does.Contain("\"rows\":"));
    }

    [Test]
    public void catalog_platform_browse_emits_schema_version_2_with_mount_and_sensor_counts()
    {
        using var writer = new StringWriter();
        Assert.That(CatalogPlatformBrowseCommand.Run(dbPath: null, writer, maxRecords: 5), Is.EqualTo(0));

        using var doc = JsonDocument.Parse(writer.ToString());
        var root = doc.RootElement;

        Assert.That(root.GetProperty("schemaVersion").GetInt32(), Is.EqualTo(2));
        Assert.That(root.TryGetProperty("rows", out var rows), Is.True);
        Assert.That(rows.ValueKind, Is.EqualTo(JsonValueKind.Array));

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
        Assert.That(rowDoc.RootElement.GetProperty("mountCount").GetInt32(), Is.EqualTo(1));
        Assert.That(rowDoc.RootElement.GetProperty("sensorCount").GetInt32(), Is.EqualTo(1));
    }

    [Test]
    public void catalog_platform_browse_with_baltic_db_and_null_snapshot_returns_platform_rows()
    {
        // UAT 2026-07-19 P0: browse against real DB with omitted snapshot returned rowCount=0.
        // Default snapshot must resolve to baltic_patrol and emit platforms (surface/air/sub).
        var dbPath = ResolveBalticCatalogDb();
        if (dbPath is null)
        {
            return; // environment without fixture DB — skip silently
        }

        Assert.That(PlatformCatalogExportResolver.TryResolve(dbPath, snapshotId: null, out var data), Is.True);
        Assert.That(data.Platforms.Count >= 50, Is.True, $"expected Baltic platforms, got {data.Platforms.Count}");

        using var writer = new StringWriter();
        Assert.That(CatalogPlatformBrowseCommand.Run(dbPath, writer, maxRecords: 20), Is.EqualTo(0));
        using var doc = JsonDocument.Parse(writer.ToString());
        Assert.That(doc.RootElement.GetProperty("rowCount").GetInt32(), Is.EqualTo(20));
        Assert.That(doc.RootElement.GetProperty("rows").GetArrayLength(), Is.EqualTo(20));
    }

    [Test]
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
            Assert.That(exit, Is.EqualTo(0));
            using var doc = JsonDocument.Parse(writer.ToString());
            Assert.That(doc.RootElement.GetProperty("ok").GetBoolean(), Is.True);
            Assert.That(doc.RootElement.GetProperty("snapshotResolved").GetBoolean(), Is.True);
            Assert.That(doc.RootElement.GetProperty("snapshotId").GetString(), Is.EqualTo("baltic_patrol"));
            Assert.That(doc.RootElement.GetProperty("platformCount").GetInt32() >= 50, Is.True);
            Assert.That(File.Exists(outPath), Is.True);
            Assert.That(new FileInfo(outPath).Length > 10_000, Is.True);
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