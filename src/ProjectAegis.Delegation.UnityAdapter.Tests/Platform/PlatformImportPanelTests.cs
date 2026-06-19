using System.Reflection;
using Microsoft.Data.Sqlite;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Platform;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Platform;

/// <summary>S29-04 Phase E: headless proxy for Unity platform import staging review UI.</summary>
[TestFixture]
public sealed class PlatformImportPanelTests
{
    [Test]
    public void Import_round_trip_propose_acknowledge_approve_readback_baltic_fixture()
    {
        var dbPath = CreateTempDbPath("unity-phase-e-e2e");
        try
        {
            CatalogSeedBootstrap.SeedBalticPatrol(dbPath, overwrite: true);

            var exported = PlatformWorkbookWriteBridge.ExportBalticWorkbook(dbPath, clockTicks: 9900);
            var edited = WithSheetCell(exported, "Sensors", 0, "BasePd", "0.48");

            var propose = PlatformWorkbookWriteBridge.ProposeWorkbook(
                dbPath,
                edited,
                actorType: "unity",
                actorId: "platform-import-host",
                clockTicks: 9901,
                rationale: "unity import panel e2e");
            Assert.That(propose.Proposed, Is.True);
            Assert.That(propose.BatchIds, Is.Not.Empty);

            var beforeAck = PlatformImportStagingProjection.Bind(propose, reviewAcknowledged: false);
            Assert.That(beforeAck.ApproveEnabled, Is.False, "Approve must stay disabled until review acknowledged");
            Assert.That(beforeAck.RejectEnabled, Is.True);
            Assert.That(beforeAck.DiffRows, Is.Not.Empty);
            Assert.That(beforeAck.DiffRows[0].EntityKey, Is.EqualTo("Sensors"));

            var afterAck = PlatformImportStagingProjection.Bind(propose, reviewAcknowledged: true);
            Assert.That(afterAck.ApproveEnabled, Is.True);

            var approve = PlatformWorkbookWriteBridge.ApproveBatches(
                dbPath,
                propose.BatchIds,
                actorType: "human",
                actorId: "curator",
                clockTicks: 9902);
            Assert.That(approve.AllCommitted, Is.True);

            using var reader = new SqliteCatalogReader(dbPath, "unity-phase-e-readback");
            Assert.That(reader.TryGetBasePd("u1", "radar-1", out var basePd), Is.True);
            Assert.That(basePd, Is.EqualTo(0.48).Within(0.000001));
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Test]
    public void Import_unedited_round_trip_produces_empty_diff_golden()
    {
        var dbPath = CreateTempDbPath("unity-phase-e-empty");
        try
        {
            CatalogSeedBootstrap.SeedBalticPatrol(dbPath, overwrite: true);
            var exported = PlatformWorkbookWriteBridge.ExportBalticWorkbook(dbPath, clockTicks: 9910);

            var propose = PlatformWorkbookWriteBridge.ProposeWorkbook(
                dbPath,
                exported,
                actorType: "unity",
                actorId: "platform-import-host",
                clockTicks: 9911);

            var panel = PlatformImportStagingProjection.Bind(propose, reviewAcknowledged: true);
            Assert.That(panel.IsEmptyDiff, Is.True);
            Assert.That(panel.ApproveEnabled, Is.False);
            Assert.That(panel.RejectEnabled, Is.False);
            Assert.That(propose.Import.Plan.HasChanges, Is.False);
            Assert.That(propose.Proposed, Is.False);
            Assert.That(propose.BatchIds, Is.Empty);
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Test]
    public void Import_reject_batch_leaves_live_sensor_unchanged()
    {
        var dbPath = CreateTempDbPath("unity-phase-e-reject");
        try
        {
            CatalogSeedBootstrap.SeedBalticPatrol(dbPath, overwrite: true);
            using var readerBefore = new SqliteCatalogReader(dbPath, "unity-phase-e-reject-before");
            Assert.That(readerBefore.TryGetBasePd("u1", "radar-1", out var originalBasePd), Is.True);

            var exported = PlatformWorkbookWriteBridge.ExportBalticWorkbook(dbPath, clockTicks: 9920);
            var edited = WithSheetCell(exported, "Sensors", 0, "BasePd", "0.12");
            var propose = PlatformWorkbookWriteBridge.ProposeWorkbook(
                dbPath,
                edited,
                actorType: "unity",
                actorId: "platform-import-host",
                clockTicks: 9921);

            var reject = PlatformWorkbookWriteBridge.RejectBatches(
                dbPath,
                propose.BatchIds,
                actorType: "human",
                actorId: "curator",
                clockTicks: 9922,
                rationale: "unity import panel reject");
            Assert.That(reject.AllCommitted, Is.False);

            using var readerAfter = new SqliteCatalogReader(dbPath, "unity-phase-e-reject-after");
            Assert.That(readerAfter.TryGetBasePd("u1", "radar-1", out var unchangedBasePd), Is.True);
            Assert.That(unchangedBasePd, Is.EqualTo(originalBasePd).Within(0.000001));
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Test]
    public void Import_damage_MaxHp_round_trip_propose_acknowledge_approve_readback_baltic_fixture()
    {
        var dbPath = CreateTempDbPath("unity-phase-f-damage-e2e");
        try
        {
            CatalogSeedBootstrap.SeedBalticPatrol(dbPath, overwrite: true);

            var exported = PlatformWorkbookWriteBridge.ExportBalticWorkbook(dbPath, clockTicks: 9930);
            var edited = WithPlatformSheetCell(exported, "u1", "MaxHp", "120");

            var propose = PlatformWorkbookWriteBridge.ProposeWorkbook(
                dbPath,
                edited,
                actorType: "unity",
                actorId: "platform-import-host",
                clockTicks: 9931,
                rationale: "unity import panel damage e2e");
            Assert.That(propose.Proposed, Is.True);
            Assert.That(propose.BatchIds, Is.Not.Empty);

            var damageRows = PlatformImportStagingProjection.ExtractDamageDeltaRows(propose.Import.Plan.Changes);
            Assert.That(damageRows, Is.Not.Empty);
            Assert.That(damageRows[0].SummaryLine, Does.Contain("DAMAGE"));
            Assert.That(damageRows[0].SummaryLine, Does.Contain("MaxHp"));

            var panel = PlatformImportStagingProjection.Bind(propose, reviewAcknowledged: true);
            Assert.That(panel.DiffRows[0].SummaryLine, Does.Contain("MaxHp"));
            Assert.That(panel.ApproveEnabled, Is.True);

            var approve = PlatformWorkbookWriteBridge.ApproveBatches(
                dbPath,
                propose.BatchIds,
                actorType: "human",
                actorId: "curator",
                clockTicks: 9932);
            Assert.That(approve.AllCommitted, Is.True);

            using var reader = new SqliteCatalogReader(dbPath, "unity-phase-f-damage-readback");
            Assert.That(reader.TryGetPlatformDamage("u1", out var damage), Is.True);
            Assert.That(damage.MaxHp, Is.EqualTo(120).Within(0.000001));

            var browseRows = CatalogPlatformBrowseProjection.FromReader(reader);
            var u1 = browseRows.Single(r => r.PlatformId == "u1");
            Assert.That(u1.MaxHp, Is.EqualTo(120));
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Test]
    public void Staging_projection_groups_entity_level_diff_rows_by_sheet()
    {
        var changes = new[]
        {
            new PlatformWorkbookChange("Sensors", PlatformWorkbookChangeKind.CellChanged, 0, "BasePd: '0.85' -> '0.48'"),
            new PlatformWorkbookChange("Sensors", PlatformWorkbookChangeKind.CellChanged, 1, "BasePd: '0.40' -> '0.41'"),
            new PlatformWorkbookChange("Mounts", PlatformWorkbookChangeKind.CellChanged, 0, "ArcDeg: '360' -> '180'"),
        };

        var rows = PlatformImportStagingProjection.GroupChangesByEntity(changes);

        Assert.That(rows, Has.Count.EqualTo(2));
        Assert.That(rows[0].EntityKey, Is.EqualTo("Mounts"));
        Assert.That(rows[0].ChangeCount, Is.EqualTo(1));
        Assert.That(rows[1].EntityKey, Is.EqualTo("Sensors"));
        Assert.That(rows[1].ChangeCount, Is.EqualTo(2));
        Assert.That(rows[1].SummaryLine, Does.Contain("Sensors"));
    }

    [Test]
    public void Platform_import_panel_element_names_are_stable()
    {
        foreach (var name in new[]
                 {
                     "platform-import-root",
                     "platform-import-workbook-path",
                     "platform-import-propose",
                     "platform-import-diff-list",
                     "platform-import-status",
                     "platform-import-acknowledge",
                     "platform-import-approve",
                     "platform-import-reject",
                 })
        {
            Assert.That(name, Does.StartWith("platform-import-"));
        }
    }

    [Test]
    public void Platform_import_panel_uxml_declares_stable_element_names()
    {
        var repoRoot = FindRepoRoot();
        Assert.That(repoRoot, Is.Not.Null);

        var uxmlPath = Path.Combine(
            repoRoot!,
            "unity",
            "ProjectAegis",
            "Assets",
            "UI",
            "PlatformImport",
            "PlatformImportPanel.uxml");
        Assert.That(File.Exists(uxmlPath), Is.True);

        var uxml = File.ReadAllText(uxmlPath);
        foreach (var name in new[]
                 {
                     "platform-import-root",
                     "platform-import-workbook-path",
                     "platform-import-propose",
                     "platform-import-diff-list",
                     "platform-import-status",
                     "platform-import-acknowledge",
                     "platform-import-approve",
                     "platform-import-reject",
                 })
        {
            Assert.That(uxml, Does.Contain($"name=\"{name}\""), $"Missing UXML element: {name}");
        }
    }

    [Test]
    public void Import_host_wires_write_bridge_and_staging_projection()
    {
        var repoRoot = FindRepoRoot();
        Assert.That(repoRoot, Is.Not.Null);

        var hostPath = Path.Combine(
            repoRoot!,
            "unity",
            "ProjectAegis",
            "Assets",
            "Scripts",
            "Runtime",
            "PlatformImportPanelHost.cs");
        Assert.That(File.Exists(hostPath), Is.True);

        var source = File.ReadAllText(hostPath);
        Assert.That(source, Does.Contain("PlatformWorkbookWriteBridge"));
        Assert.That(source, Does.Contain("PlatformImportStagingProjection"));
        Assert.That(source, Does.Contain("platform-import-acknowledge"));
        Assert.That(source, Does.Contain("platform-import-approve"));
        Assert.That(source, Does.Contain("SetEnabled(panelState.ApproveEnabled)"));
    }

    [Test]
    public void Import_host_and_projection_have_no_write_gate_bypass_patterns()
    {
        AssertNoWriteGateTypes(typeof(PlatformImportStagingProjection));

        var repoRoot = FindRepoRoot();
        Assert.That(repoRoot, Is.Not.Null);

        var hostPath = Path.Combine(
            repoRoot!,
            "unity",
            "ProjectAegis",
            "Assets",
            "Scripts",
            "Runtime",
            "PlatformImportPanelHost.cs");
        Assert.That(File.Exists(hostPath), Is.True);

        var source = File.ReadAllText(hostPath);
        foreach (var token in new[] { "IWriteGate", "CatalogWriteGate", "SqliteConnection", "ExecuteNonQuery", "INSERT INTO" })
        {
            Assert.That(source, Does.Not.Contain(token), $"Import host must not reference {token}");
        }

        Assert.That(source, Does.Contain("PlatformWorkbookWriteBridge"));
        Assert.That(source, Does.Not.Contain("PlatformCatalogExportBridge"));
    }

    [Test]
    public void Write_bridge_type_has_no_direct_sqlite_or_gate_bypass_patterns()
    {
        var bridgeType = typeof(PlatformWorkbookWriteBridge);
        var sourcePath = FindBridgeSourcePath();
        Assert.That(sourcePath, Is.Not.Null);

        var source = File.ReadAllText(sourcePath!);
        Assert.That(source, Does.Not.Contain("SqliteConnection"));
        Assert.That(source, Does.Contain("PlatformWorkbookWriteService"));
        Assert.That(source, Does.Contain("ProposeWorkbookFromFile"));

        foreach (var method in bridgeType.GetMethods(BindingFlags.Public | BindingFlags.Static))
        {
            AssertNoBypassToken(method.ReturnType.FullName ?? string.Empty, method.Name);
            foreach (var param in method.GetParameters())
            {
                AssertNoBypassToken(param.ParameterType.FullName ?? string.Empty, method.Name);
            }
        }
    }

    private static void AssertNoWriteGateTypes(Type projectionType)
    {
        var methods = projectionType
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.DeclaringType == projectionType);

        foreach (var method in methods)
        {
            AssertNoWriteGateToken(method.ReturnType.FullName ?? string.Empty, projectionType.Name, method.Name);
            foreach (var param in method.GetParameters())
            {
                AssertNoWriteGateToken(param.ParameterType.FullName ?? string.Empty, projectionType.Name, method.Name);
            }
        }
    }

    private static void AssertNoWriteGateToken(string typeName, string declaringType, string methodName)
    {
        Assert.That(typeName, Does.Not.Contain("IWriteGate"), $"{declaringType}.{methodName}");
        Assert.That(typeName, Does.Not.Contain("CatalogWriteGate"), $"{declaringType}.{methodName}");
    }

    private static void AssertNoBypassToken(string typeName, string methodName)
    {
        Assert.That(typeName, Does.Not.Contain("SqliteConnection"), methodName);
        Assert.That(typeName, Does.Not.Contain("CatalogWriteGate"), methodName);
    }

    private static PlatformWorkbook WithPlatformSheetCell(
        PlatformWorkbook workbook,
        string platformId,
        string columnName,
        string value)
    {
        var sheets = workbook.Sheets.Select(sheet =>
        {
            if (!string.Equals(sheet.Name, "Platforms", StringComparison.Ordinal))
            {
                return sheet;
            }

            var colIndex = Array.IndexOf(sheet.Header.ToArray(), columnName);
            Assert.That(colIndex, Is.GreaterThanOrEqualTo(0), $"Column '{columnName}' missing on Platforms.");

            var rows = sheet.Rows.Select(row =>
            {
                var platformCol = Array.IndexOf(sheet.Header.ToArray(), "PlatformId");
                if (platformCol < 0 || platformCol >= row.Count || !string.Equals(row[platformCol], platformId, StringComparison.Ordinal))
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

    private static string? FindRepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 8; i++)
        {
            if (File.Exists(Path.Combine(dir, "ProjectAegis.sln")))
            {
                return dir;
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