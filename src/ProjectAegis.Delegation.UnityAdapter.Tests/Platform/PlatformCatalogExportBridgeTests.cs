using System.Reflection;
using Microsoft.Data.Sqlite;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Platform;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Platform;

/// <summary>S28-07 Phase C: headless Unity bridge for read-only platform workbook export/diff.</summary>
[TestFixture]
public sealed class PlatformCatalogExportBridgeTests
{
    [Test]
    public void Bridge_export_trigger_produces_workbook_artifact_for_baltic_fixture()
    {
        var dbPath = CreateTempDbPath("unity-phase-c-export");
        var outPath = Path.Combine(Path.GetTempPath(), $"aegis-export-{Guid.NewGuid():N}.platform.txt");
        try
        {
            CatalogSeedBootstrap.SeedBalticPatrol(dbPath, overwrite: true);

            var workbook = PlatformCatalogExportBridge.ExportBalticWorkbook(dbPath, clockTicks: 9900);
            Assert.That(workbook.Sheets, Is.Not.Empty);
            Assert.That(workbook.FindSheet("Platforms"), Is.Not.Null);

            PlatformCatalogExportBridge.ExportBalticToFile(dbPath, outPath, clockTicks: 9901);
            Assert.That(File.Exists(outPath), Is.True);
            Assert.That(new FileInfo(outPath).Length, Is.GreaterThan(0));
        }
        finally
        {
            Cleanup(dbPath);
            if (File.Exists(outPath))
            {
                File.Delete(outPath);
            }
        }
    }

    [Test]
    public void Bridge_unedited_round_trip_diff_is_empty_golden()
    {
        var dbPath = CreateTempDbPath("unity-phase-c-diff");
        try
        {
            CatalogSeedBootstrap.SeedBalticPatrol(dbPath, overwrite: true);

            var changes = PlatformCatalogExportBridge.DiffBalticUneditedRoundTrip(dbPath, clockTicks: 9910);
            Assert.That(changes, Is.Empty);
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Test]
    public void Bridge_type_has_no_write_gate_or_direct_sqlite_patterns()
    {
        var bridgeType = typeof(PlatformCatalogExportBridge);
        Assert.That(bridgeType.IsAbstract && bridgeType.IsSealed, Is.True);

        var sourcePath = FindBridgeSourcePath();
        Assert.That(sourcePath, Is.Not.Null);
        var source = File.ReadAllText(sourcePath!);
        Assert.That(source, Does.Not.Contain("SqliteConnection"));
        Assert.That(source, Does.Not.Contain("CatalogWriteGate"));
        Assert.That(source, Does.Not.Contain("Propose"));
        Assert.That(source, Does.Not.Contain("ApproveBatch"));
        Assert.That(source, Does.Contain("PlatformCatalogExportResolver"));
        Assert.That(source, Does.Contain("PlatformWorkbookExporter"));

        foreach (var method in bridgeType.GetMethods(BindingFlags.Public | BindingFlags.Static))
        {
            AssertNoWritePathToken(method.ReturnType.FullName ?? string.Empty, method.Name);
            foreach (var param in method.GetParameters())
            {
                AssertNoWritePathToken(param.ParameterType.FullName ?? string.Empty, method.Name);
            }
        }
    }

    private static void AssertNoWritePathToken(string typeName, string methodName)
    {
        Assert.That(typeName, Does.Not.Contain("CatalogWriteGate"), methodName);
        Assert.That(typeName, Does.Not.Contain("IWriteGate"), methodName);
        Assert.That(typeName, Does.Not.Contain("SqliteConnection"), methodName);
    }

    private static string? FindBridgeSourcePath()
    {
        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 8; i++)
        {
            var candidate = Path.Combine(
                dir,
                "src",
                "ProjectAegis.Delegation.UnityAdapter",
                "Bridge",
                "PlatformCatalogExportBridge.cs");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            if (File.Exists(Path.Combine(dir, "ProjectAegis.sln")))
            {
                return Path.Combine(
                    dir,
                    "src",
                    "ProjectAegis.Delegation.UnityAdapter",
                    "Bridge",
                    "PlatformCatalogExportBridge.cs");
            }

            dir = Directory.GetParent(dir)?.FullName ?? dir;
        }

        return null;
    }

    private static string CreateTempDbPath(string label) =>
        Path.Combine(Path.GetTempPath(), $"aegis-unity-{label}-{Guid.NewGuid():N}.db");

    private static void Cleanup(string dbPath)
    {
        SqliteConnection.ClearAllPools();
        if (File.Exists(dbPath))
        {
            File.Delete(dbPath);
        }
    }
}