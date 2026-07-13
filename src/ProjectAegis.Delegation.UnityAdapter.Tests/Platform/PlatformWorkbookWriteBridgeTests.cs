using System.Reflection;
using Microsoft.Data.Sqlite;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Platform;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Platform;

/// <summary>S28-04 Phase D: headless Unity bridge for platform workbook write-gate propose→approve.</summary>
[TestFixture]
public sealed class PlatformWorkbookWriteBridgeTests
{
    [Test]
    public void Bridge_E2E_export_edit_propose_approve_readback_baltic_fixture()
    {
        var dbPath = CreateTempDbPath("unity-phase-d-e2e");
        try
        {
            CatalogSeedBootstrap.SeedBalticPatrol(dbPath, overwrite: true);

            var exported = PlatformWorkbookWriteBridge.ExportBalticWorkbook(dbPath, clockTicks: 9800);
            var edited = WithSheetCell(exported, "Sensors", 0, "BasePd", "0.48");

            var propose = PlatformWorkbookWriteBridge.ProposeWorkbook(
                dbPath,
                edited,
                actorType: "unity",
                actorId: "phase-d-host",
                clockTicks: 9801,
                rationale: "unity bridge e2e");
            Assert.That(propose.Proposed, Is.True);
            var batchId = propose.BatchIds.Single();

            var approve = PlatformWorkbookWriteBridge.ApproveBatches(
                dbPath,
                [batchId],
                actorType: "human",
                actorId: "qa-reviewer",
                clockTicks: 9802);
            Assert.That(approve.AllCommitted, Is.True);

            using var reader = new SqliteCatalogReader(dbPath, "unity-phase-d-readback");
            Assert.That(reader.TryGetBasePd("u1", "radar-1", out var basePd), Is.True);
            Assert.That(basePd, Is.EqualTo(0.48).Within(0.000001));
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Test]
    public void Bridge_reject_batch_leaves_live_sensor_unchanged()
    {
        var dbPath = CreateTempDbPath("unity-phase-d-reject");
        try
        {
            CatalogSeedBootstrap.SeedBalticPatrol(dbPath, overwrite: true);
            using var readerBefore = new SqliteCatalogReader(dbPath, "unity-phase-d-reject-before");
            Assert.That(readerBefore.TryGetBasePd("u1", "radar-1", out var originalBasePd), Is.True);

            var exported = PlatformWorkbookWriteBridge.ExportBalticWorkbook(dbPath, clockTicks: 9810);
            var edited = WithSheetCell(exported, "Sensors", 0, "BasePd", "0.12");
            var propose = PlatformWorkbookWriteBridge.ProposeWorkbook(
                dbPath,
                edited,
                actorType: "unity",
                actorId: "phase-d-host",
                clockTicks: 9811);

            var reject = PlatformWorkbookWriteBridge.RejectBatches(
                dbPath,
                propose.BatchIds,
                actorType: "human",
                actorId: "qa-reviewer",
                clockTicks: 9812,
                rationale: "reject unity bridge batch");
            Assert.That(reject.AllCommitted, Is.False);

            using var readerAfter = new SqliteCatalogReader(dbPath, "unity-phase-d-reject-after");
            Assert.That(readerAfter.TryGetBasePd("u1", "radar-1", out var unchangedBasePd), Is.True);
            Assert.That(unchangedBasePd, Is.EqualTo(originalBasePd).Within(0.000001));
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Test]
    public void Bridge_type_has_no_direct_sqlite_or_gate_bypass_patterns()
    {
        var bridgeType = typeof(PlatformWorkbookWriteBridge);
        Assert.That(bridgeType.IsAbstract && bridgeType.IsSealed, Is.True);

        var sourcePath = FindBridgeSourcePath();
        Assert.That(sourcePath, Is.Not.Null);
        var source = File.ReadAllText(sourcePath!);
        Assert.That(source, Does.Not.Contain("SqliteConnection"));
        Assert.That(source, Does.Not.Contain("ExecuteNonQuery"));
        Assert.That(source, Does.Not.Contain("INSERT INTO"));
        Assert.That(source, Does.Contain("PlatformWorkbookWriteService"));

        foreach (var method in bridgeType.GetMethods(BindingFlags.Public | BindingFlags.Static))
        {
            AssertNoBypassToken(method.ReturnType.FullName ?? string.Empty, method.Name);
            foreach (var param in method.GetParameters())
            {
                AssertNoBypassToken(param.ParameterType.FullName ?? string.Empty, method.Name);
            }
        }
    }

    [Test]
    public void Bridge_unedited_round_trip_produces_empty_diff_golden()
    {
        var dbPath = CreateTempDbPath("unity-phase-d-empty");
        try
        {
            CatalogSeedBootstrap.SeedBalticPatrol(dbPath, overwrite: true);
            var exported = PlatformWorkbookWriteBridge.ExportBalticWorkbook(dbPath, clockTicks: 9820);

            var propose = PlatformWorkbookWriteBridge.ProposeWorkbook(
                dbPath,
                exported,
                actorType: "unity",
                actorId: "phase-d-host",
                clockTicks: 9821);

            Assert.That(propose.Import.Plan.HasChanges, Is.False);
            Assert.That(propose.Proposed, Is.False);
            Assert.That(propose.BatchIds, Is.Empty);
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    private static void AssertNoBypassToken(string typeName, string methodName)
    {
        Assert.That(typeName, Does.Not.Contain("SqliteConnection"), methodName);
        Assert.That(typeName, Does.Not.Contain("CatalogWriteGate"), methodName);
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
                "PlatformWorkbookWriteBridge.cs");
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
                    "PlatformWorkbookWriteBridge.cs");
            }

            dir = Directory.GetParent(dir)?.FullName ?? dir;
        }

        return null;
    }

    private static PlatformWorkbook WithSheetCell(
        PlatformWorkbook workbook,
        string sheetName,
        int rowIndex,
        string columnName,
        string value)
    {
        var sheets = workbook.Sheets.Select(sheet =>
        {
            if (!string.Equals(sheet.Name, sheetName, StringComparison.Ordinal))
            {
                return sheet;
            }

            var colIndex = Array.IndexOf(sheet.Header.ToArray(), columnName);
            Assert.That(colIndex, Is.GreaterThanOrEqualTo(0), $"Column '{columnName}' missing on '{sheetName}'.");

            var rows = sheet.Rows.Select((row, i) =>
            {
                if (i != rowIndex)
                {
                    return row;
                }

                var cells = row.ToList();
                while (cells.Count <= colIndex)
                {
                    cells.Add(string.Empty);
                }

                cells[colIndex] = value;
                return (IReadOnlyList<string>)cells;
            }).ToArray();

            return sheet with { Rows = rows };
        }).ToArray();

        return workbook with { Sheets = sheets };
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