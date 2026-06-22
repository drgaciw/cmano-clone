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
    public void catalog_platform_browse_via_resolver_with_real_db_file_and_null_snapshotId_exposes_CS8625_nullability_defect()
    {
        // TDD-RED (per tdd-red.agent): write failing test FIRST to expose the issue.
        // The defect is in CatalogPlatformBrowseCommand.cs:12 : snapshotId: null passed to TryResolve(string snapshotId /*non-nullable*/)
        // This produces build warning CS8625: "Cannot convert null literal to non-nullable reference type."
        // When dbPath exists (non-null), resolver proceeds to reader.TryResolveDbRef(null) -> NRE in CatalogValidationDefaults.TryResolveBalticDbRef (dbRef.Contains on null).
        // Existing tests hide it (use dbPath: null, short-circuit return before snapshot usage).
        // ADR-011 Phase C: CatalogPlatformBrowseCommand (read-only browse).
        // Test must fail for right reason (nullability / NRE logic) not syntax.
        // Do NOT fix production code (resolver signature or command) yet.
        // Use full path: /home/username01/cmano-clone/cmano-clone/src/ProjectAegis.MissionEditor.Cli.Tests/CatalogPlatformBrowseCommandTests.cs

        var tempDb = Path.GetTempFileName();
        try
        {
            // direct resolver call (exercises same bad null path as the browse command)
            // note: also triggers CS8625 at this callsite in test during build
            var resolved = PlatformCatalogExportResolver.TryResolve(tempDb, snapshotId: null, out var data);
            Assert.False(resolved); // desired: should not crash, treat as unresolvable (but currently NREs)
            Assert.Same(PlatformCatalogExportData.Empty, data);
        }
        finally
        {
            if (File.Exists(tempDb)) File.Delete(tempDb);
        }
    }
}