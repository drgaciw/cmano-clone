using System.Reflection;
using Microsoft.Data.Sqlite;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Platform;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Platform;

/// <summary>S33-06 Phase G: headless proxy for comms/datalink catalog viewer + import staging.</summary>
[TestFixture]
public sealed class PlatformCommsTests
{
    private static IReadOnlyList<CatalogCommsBinding> BalticCommsFixture() =>
    [
        new CatalogCommsBinding("u1", "NATO_TADIL_J", Role: "txrx", SatcomCapable: false),
        new CatalogCommsBinding("u1", "SATCOM_B", Role: "relay", SatcomCapable: true),
    ];

    [Test]
    public void PlatformComms_list_projection_formats_link_role_and_satcom()
    {
        var lines = PlatformCommsListProjection.FormatRows(BalticCommsFixture());

        Assert.That(lines, Has.Count.EqualTo(2));
        Assert.That(lines[0], Is.EqualTo("NATO_TADIL_J role=txrx satcom=false"));
        Assert.That(lines[1], Is.EqualTo("SATCOM_B role=relay satcom=true"));
    }

    [Test]
    public void PlatformComms_projection_filters_sorted_fittings_for_platform()
    {
        var reader = new InMemoryCatalogReader(
            [new CatalogSensorBinding("u1", "radar-1", 1.0, "fixture")],
            comms: BalticCommsFixture());

        var u1 = CatalogPlatformCommsProjection.ForPlatform(reader, "u1");
        Assert.That(u1, Has.Count.EqualTo(2));
        Assert.That(u1.Select(c => c.LinkId), Is.EqualTo(new[] { "NATO_TADIL_J", "SATCOM_B" }));

        Assert.That(CatalogPlatformCommsProjection.ForPlatform(reader, "missing"), Is.Empty);
    }

    [Test]
    public void PlatformComms_panel_uxml_declares_comms_list_elements()
    {
        var repoRoot = FindRepoRoot();
        Assert.That(repoRoot, Is.Not.Null);

        var uxmlPath = Path.Combine(
            repoRoot!,
            "unity",
            "ProjectAegis",
            "Assets",
            "UI",
            "PlatformCatalog",
            "PlatformCatalogPanel.uxml");
        var uxml = File.ReadAllText(uxmlPath);

        Assert.That(uxml, Does.Contain("name=\"platform-catalog-comms\""));
        Assert.That(uxml, Does.Contain("name=\"platform-catalog-comms-list\""));
        Assert.That(uxml, Does.Contain("selection-type=\"None\""));
    }

    [Test]
    public void PlatformComms_viewer_host_wires_comms_list_projection()
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
            "PlatformCatalogViewerHost.cs");
        var source = File.ReadAllText(hostPath);

        Assert.That(source, Does.Contain("platform-catalog-comms-list"));
        Assert.That(source, Does.Contain("CatalogPlatformCommsProjection.ForPlatform"));
        Assert.That(source, Does.Contain("PlatformCommsListProjection"));
        Assert.That(source, Does.Contain("FormatRows(fittings"));
        Assert.That(source, Does.Contain("GetSortedComms"));
    }

    [Test]
    public void PlatformComms_staging_projection_surfaces_comms_field_deltas()
    {
        var changes = new[]
        {
            new PlatformWorkbookChange("Comms", PlatformWorkbookChangeKind.CellChanged, 1, "Role: 'txrx' -> 'relay'"),
            new PlatformWorkbookChange("Comms", PlatformWorkbookChangeKind.CellChanged, 1, "SatcomCapable: 'false' -> 'true'"),
            new PlatformWorkbookChange("Sensors", PlatformWorkbookChangeKind.CellChanged, 0, "BasePd: '0.85' -> '0.48'"),
        };

        var commsRows = PlatformImportStagingProjection.ExtractCommsDeltaRows(changes);
        Assert.That(commsRows, Has.Count.EqualTo(2));
        Assert.That(commsRows[0].SummaryLine, Does.Contain("COMMS"));
        Assert.That(commsRows[0].SummaryLine, Does.Contain("Role"));
        Assert.That(commsRows[1].SummaryLine, Does.Contain("SatcomCapable"));

        var panelRows = PlatformImportStagingProjection.BuildDiffRows(changes);
        Assert.That(panelRows[0].SummaryLine, Does.Contain("COMMS"));
        Assert.That(panelRows[1].SummaryLine, Does.Contain("COMMS"));
        Assert.That(panelRows[^1].EntityKey, Is.EqualTo("Sensors"));
    }

    [Test]
    public void PlatformComms_import_round_trip_propose_acknowledge_approve_readback_baltic_fixture()
    {
        var dbPath = CreateTempDbPath("unity-phase-g-comms-e2e");
        try
        {
            CatalogSeedBootstrap.SeedBalticPatrol(dbPath, overwrite: true);

            var exported = PlatformWorkbookWriteBridge.ExportBalticWorkbook(dbPath, clockTicks: 9940);
            var edited = WithCommsSheetRow(
                exported,
                platformId: "u1",
                linkId: "NATO_TADIL_J",
                role: "relay",
                satcomCapable: false);

            var propose = PlatformWorkbookWriteBridge.ProposeWorkbook(
                dbPath,
                edited,
                actorType: "unity",
                actorId: "platform-import-host",
                clockTicks: 9941,
                rationale: "unity import panel comms e2e");
            Assert.That(propose.Proposed, Is.True);
            Assert.That(propose.BatchIds, Is.Not.Empty);

            var commsRows = PlatformImportStagingProjection.ExtractCommsDeltaRows(propose.Import.Plan.Changes);
            Assert.That(commsRows, Is.Not.Empty);
            Assert.That(commsRows.Any(r => r.SummaryLine.Contains("COMMS", StringComparison.Ordinal)), Is.True);
            Assert.That(
                commsRows.Any(r => r.SummaryLine.Contains("COMMS", StringComparison.Ordinal)),
                Is.True);

            var panel = PlatformImportStagingProjection.Bind(propose, reviewAcknowledged: true);
            Assert.That(panel.ApproveEnabled, Is.True);

            var approve = PlatformWorkbookWriteBridge.ApproveBatches(
                dbPath,
                propose.BatchIds,
                actorType: "human",
                actorId: "curator",
                clockTicks: 9942);
            Assert.That(approve.AllCommitted, Is.True);

            using var reader = new SqliteCatalogReader(dbPath, "unity-phase-g-comms-readback");
            var comms = CatalogPlatformCommsProjection.ForPlatform(reader, "u1");
            var binding = comms.Single(c => c.LinkId == "NATO_TADIL_J");
            Assert.That(binding.Role, Is.EqualTo("relay"));
            Assert.That(binding.SatcomCapable, Is.False);

            var listLine = PlatformCommsListProjection.FormatRow(binding);
            Assert.That(listLine, Does.Contain("NATO_TADIL_J"));
            Assert.That(listLine, Does.Contain("role=relay"));
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Test]
    public void PlatformComms_baltic_fixture_comms_surfaces_workbook_values_in_list_projection()
    {
        var reader = new InMemoryCatalogReader(
            InMemoryCatalogReader.BalticPatrolFixture().GetSortedSensorBindings(),
            "p0-baltic-comms-fixture",
            CatalogValidationDefaults.BalticPlatforms(),
            comms: BalticCommsFixture());

        var u1 = CatalogPlatformCommsProjection.ForPlatform(reader, "u1");
        Assert.That(u1, Has.Count.EqualTo(2));
        Assert.That(u1[0].LinkId, Is.EqualTo("NATO_TADIL_J"));
        Assert.That(u1[0].Role, Is.EqualTo("txrx"));
        Assert.That(u1[0].SatcomCapable, Is.False);

        var lines = PlatformCommsListProjection.FormatRows(u1);
        Assert.That(lines[0], Does.Contain("NATO_TADIL_J"));
        Assert.That(lines[0], Does.Contain("role=txrx"));
        Assert.That(lines[0], Does.Contain("satcom=false"));
        Assert.That(lines[1], Does.Contain("SATCOM_B"));
        Assert.That(lines[1], Does.Contain("satcom=true"));
    }

    [Test]
    public void PlatformComms_delegation_smoke_scene_builder_includes_comms_viewer_wiring()
    {
        var repoRoot = FindRepoRoot();
        Assert.That(repoRoot, Is.Not.Null);

        var builderPath = Path.Combine(
            repoRoot!,
            "unity",
            "ProjectAegis",
            "Assets",
            "Editor",
            "DelegationSmokeSceneBuilder.cs");
        var builder = File.ReadAllText(builderPath);

        Assert.That(builder, Does.Contain("PlatformCatalogViewerHost"));
        Assert.That(builder, Does.Contain("\"PlatformCatalog\""));
        Assert.That(builder, Does.Contain("Assets/UI/PlatformCatalog/PlatformCatalogPanel.uxml"));
        Assert.That(builder, Does.Contain("Assets/UI/PlatformCatalog/PlatformCatalogPanel.uss"));

        var uxmlPath = Path.Combine(
            repoRoot!,
            "unity",
            "ProjectAegis",
            "Assets",
            "UI",
            "PlatformCatalog",
            "PlatformCatalogPanel.uxml");
        var uxml = File.ReadAllText(uxmlPath);
        Assert.That(uxml, Does.Contain("name=\"platform-catalog-comms\""));
        Assert.That(uxml, Does.Contain("name=\"platform-catalog-comms-list\""));
    }

    [Test]
    public void PlatformComms_import_panel_uxml_declares_entity_diff_for_comms_staging()
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
        var uxml = File.ReadAllText(uxmlPath);

        Assert.That(uxml, Does.Contain("name=\"platform-import-diff-list\""));
        Assert.That(uxml, Does.Contain("selection-type=\"None\""));
        Assert.That(uxml, Does.Contain("name=\"platform-import-acknowledge\""));
        Assert.That(uxml, Does.Contain("name=\"platform-import-approve\""));

        var hostPath = Path.Combine(
            repoRoot!,
            "unity",
            "ProjectAegis",
            "Assets",
            "Scripts",
            "Runtime",
            "PlatformImportPanelHost.cs");
        var hostSource = File.ReadAllText(hostPath);
        Assert.That(hostSource, Does.Contain("platform-import-diff-list"));
        Assert.That(hostSource, Does.Contain("PlatformImportStagingProjection.Bind"));
        Assert.That(hostSource, Does.Contain("PlatformWorkbookWriteBridge"));
    }

    [Test]
    public void PlatformComms_viewer_host_binds_comms_list_on_platform_selection()
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
            "PlatformCatalogViewerHost.cs");
        var source = File.ReadAllText(hostPath);

        Assert.That(source, Does.Contain("selectionChanged += OnSelectionChanged"));
        Assert.That(source, Does.Contain("private void OnSelectionChanged"));
        Assert.That(source, Does.Contain("BindComms(row?.PlatformId)"));
        Assert.That(source, Does.Contain("private void BindComms"));
        Assert.That(source, Does.Contain("CatalogPlatformCommsProjection.ForPlatform(_allComms, platformId)"));
        Assert.That(source, Does.Contain("_commsList.itemsSource = _commsDisplayItems"));
        Assert.That(source, Does.Contain("_linkDisplayNames"));
        Assert.That(source, Does.Contain("reader.GetSortedComms()"));
    }

    [Test]
    public void PlatformComms_staging_diff_surfaces_added_comms_row()
    {
        var changes = new[]
        {
            new PlatformWorkbookChange(
                "Comms",
                PlatformWorkbookChangeKind.RowAdded,
                2,
                "PlatformId=u1 LinkId=HF_DATALINK Role=rx SatcomCapable=false"),
        };

        var commsRows = PlatformImportStagingProjection.ExtractCommsDeltaRows(changes);
        Assert.That(commsRows, Has.Count.EqualTo(1));
        Assert.That(commsRows[0].EntityKey, Is.EqualTo("Comms"));
        Assert.That(commsRows[0].SummaryLine, Does.Contain("COMMS"));
        Assert.That(commsRows[0].SummaryLine, Does.Contain("row=2"));

        var panelRows = PlatformImportStagingProjection.BuildDiffRows(changes);
        Assert.That(panelRows, Has.Count.EqualTo(1));
        Assert.That(panelRows[0].SummaryLine, Does.StartWith("COMMS row=2:"));
    }

    [Test]
    public void PlatformComms_projection_path_has_no_write_gate_types()
    {
        AssertNoWriteGateTypes(typeof(CatalogPlatformCommsProjection));
        AssertNoWriteGateTypes(typeof(PlatformCommsListProjection));
        AssertNoWriteGateTypes(typeof(PlatformImportStagingProjection));
    }

    private static PlatformWorkbook WithCommsSheetRow(
        PlatformWorkbook workbook,
        string platformId,
        string linkId,
        string role,
        bool satcomCapable)
    {
        var sheets = workbook.Sheets.Select(sheet =>
        {
            if (!string.Equals(sheet.Name, "Comms", StringComparison.Ordinal))
            {
                return sheet;
            }

            var header = sheet.Header.ToArray();
            var row = header
                .Select(column => column switch
                {
                    "PlatformId" => platformId,
                    "LinkId" => linkId,
                    "Role" => role,
                    "SatcomCapable" => satcomCapable ? "true" : "false",
                    "ReviewState" => CatalogReviewStates.Provisional,
                    "TrlLevel" => "9",
                    "ValueTier" => CatalogProvenanceTier.GameplayAbstraction,
                    "CitationRef" => "unity-phase-g",
                    _ => string.Empty,
                })
                .ToArray();

            var rows = sheet.Rows.Append((IReadOnlyList<string>)row).ToArray();
            return sheet with { Rows = rows };
        }).ToArray();

        return workbook with { Sheets = sheets };
    }

    private static PlatformWorkbook WithCommsSheetCell(
        PlatformWorkbook workbook,
        int rowIndex,
        string columnName,
        string value)
    {
        var sheets = workbook.Sheets.Select(sheet =>
        {
            if (!string.Equals(sheet.Name, "Comms", StringComparison.Ordinal))
            {
                return sheet;
            }

            var colIndex = Array.IndexOf(sheet.Header.ToArray(), columnName);
            Assert.That(colIndex, Is.GreaterThanOrEqualTo(0), $"Column '{columnName}' missing on Comms.");

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
        Assert.That(typeName, Does.Not.Contain("Propose"), $"{declaringType}.{methodName}");
        Assert.That(typeName, Does.Not.Contain("ApproveBatch"), $"{declaringType}.{methodName}");
    }

    private static string CreateTempDbPath(string label) =>
        Path.Combine(Path.GetTempPath(), $"aegis-{label}-{Guid.NewGuid():N}.db");

    private static void Cleanup(string dbPath)
    {
        SqliteConnection.ClearAllPools();
        if (File.Exists(dbPath))
        {
            File.Delete(dbPath);
        }
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
}