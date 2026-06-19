using System.Reflection;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Platform;

/// <summary>S27-08 headless proxy: Phase C platform browse + filter projection (Unity host read-only).</summary>
[TestFixture]
public sealed class PlatformCatalogViewerTests
{
    private static IReadOnlyList<CatalogPlatformBrowseRow> BalticBrowseRows()
    {
        var platforms = CatalogValidationDefaults.BalticPlatforms();
        var bindings = platforms
            .Select(p => new CatalogSensorBinding(p.PlatformId, "radar-1", 1.0, $"baltic-fixture-{p.PlatformId}"))
            .ToArray();
        var damage = new[]
        {
            new CatalogPlatformDamage("u1", MaxHp: 100, WithdrawThresholdPct: 25, CriticalFlags: 0, Resilience: 1.0),
        };
        var reader = new InMemoryCatalogReader(bindings, "p0-baltic-fixture", platforms, damage: damage);
        return CatalogPlatformBrowseProjection.FromReader(reader);
    }

    [Test]
    public void Baltic_fixture_produces_sorted_browse_rows_without_write_gate()
    {
        var reader = InMemoryCatalogReader.BalticPatrolFixture();
        var rows = CatalogPlatformBrowseProjection.FromReader(reader);

        Assert.That(rows, Is.Not.Empty);
        Assert.That(
            rows.Select(r => r.PlatformId),
            Is.EqualTo(rows.OrderBy(r => r.PlatformId, StringComparer.Ordinal).Select(r => r.PlatformId)));
    }

    [Test]
    public void Filter_narrows_baltic_fixture_preserving_stable_order()
    {
        var rows = BalticBrowseRows();
        Assert.That(rows.Count, Is.GreaterThanOrEqualTo(3));
        var filtered = PlatformCatalogFilterProjection.Apply(rows, "hostile");

        Assert.That(
            filtered.Select(r => r.PlatformId).ToArray(),
            Is.EqualTo(new[] { "hostile-1", "hostile-far" }));
    }

    [Test]
    public void Empty_filter_shows_all_baltic_platforms()
    {
        var rows = BalticBrowseRows();

        Assert.That(PlatformCatalogFilterProjection.Apply(rows, string.Empty), Is.EqualTo(rows));
        Assert.That(PlatformCatalogFilterProjection.Apply(rows, "   "), Is.EqualTo(rows));
    }

    [Test]
    public void No_match_filter_shows_empty_list()
    {
        var rows = BalticBrowseRows();

        Assert.That(PlatformCatalogFilterProjection.Apply(rows, "zzz-no-match"), Is.Empty);
    }

    [Test]
    public void Platform_catalog_viewer_host_element_names_are_stable()
    {
        Assert.That("platform-catalog-root", Is.EqualTo("platform-catalog-root"));
        Assert.That("platform-catalog-search", Is.EqualTo("platform-catalog-search"));
        Assert.That("platform-catalog-list", Is.EqualTo("platform-catalog-list"));
        Assert.That("platform-catalog-detail", Is.EqualTo("platform-catalog-detail"));
        Assert.That("platform-catalog-detail-lat", Is.EqualTo("platform-catalog-detail-lat"));
        Assert.That("platform-catalog-detail-lon", Is.EqualTo("platform-catalog-detail-lon"));
        Assert.That("platform-catalog-detail-radius", Is.EqualTo("platform-catalog-detail-radius"));
        Assert.That("platform-catalog-detail-hp", Is.EqualTo("platform-catalog-detail-hp"));
        Assert.That("platform-catalog-detail-resilience", Is.EqualTo("platform-catalog-detail-resilience"));
        Assert.That("platform-catalog-detail-withdraw", Is.EqualTo("platform-catalog-detail-withdraw"));
        Assert.That("platform-catalog-detail-flags", Is.EqualTo("platform-catalog-detail-flags"));
        Assert.That("platform-catalog-detail-speed", Is.EqualTo("platform-catalog-detail-speed"));
        Assert.That("platform-catalog-export", Is.EqualTo("platform-catalog-export"));
        Assert.That("platform-catalog-diff", Is.EqualTo("platform-catalog-diff"));
    }

    [Test]
    public void Platform_catalog_panel_uxml_declares_stable_element_names()
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
        Assert.That(File.Exists(uxmlPath), Is.True);

        var uxml = File.ReadAllText(uxmlPath);
        foreach (var name in new[]
                 {
                     "platform-catalog-root",
                     "platform-catalog-search",
                     "platform-catalog-list",
                     "platform-catalog-detail",
                     "platform-catalog-detail-lat",
                     "platform-catalog-detail-lon",
                     "platform-catalog-detail-radius",
                     "platform-catalog-detail-hp",
                     "platform-catalog-detail-resilience",
                     "platform-catalog-detail-withdraw",
                     "platform-catalog-detail-flags",
                     "platform-catalog-detail-speed",
                     "platform-catalog-export",
                     "platform-catalog-diff",
                 })
        {
            Assert.That(uxml, Does.Contain($"name=\"{name}\""), $"Missing UXML element: {name}");
        }

        Assert.That(uxml, Does.Contain("selection-type=\"Single\""));
    }

    [Test]
    public void Selected_row_detail_projection_matches_browse_row_values()
    {
        var rows = BalticBrowseRows();
        var selected = rows.Single(r => r.PlatformId == "hostile-1");

        var detail = PlatformCatalogDetailProjection.Format(selected);

        Assert.That(detail.LatLabel, Does.Contain(selected.LatDeg!.Value.ToString()));
        Assert.That(detail.LonLabel, Does.Contain(selected.LonDeg!.Value.ToString()));
        Assert.That(detail.CombatRadiusLabel, Does.Contain(selected.CombatRadiusNm!.Value.ToString()));
        Assert.That(detail.MaxHpLabel, Is.EqualTo("HP: —"));
        Assert.That(detail.ResilienceLabel, Is.EqualTo("RESILIENCE: —"));
        Assert.That(detail.WithdrawThresholdLabel, Is.EqualTo("WITHDRAW: —"));
        Assert.That(detail.CriticalFlagsLabel, Is.EqualTo("FLAGS: —"));
        Assert.That(detail.MaxSpeedLabel, Is.EqualTo("SPEED: —"));
    }

    [Test]
    public void Baltic_fixture_damage_row_surfaces_workbook_values_in_list_and_detail()
    {
        var rows = BalticBrowseRows();
        var selected = rows.Single(r => r.PlatformId == "u1");

        Assert.That(selected.MaxHp, Is.EqualTo(100));
        Assert.That(selected.Resilience, Is.EqualTo(1.0));
        Assert.That(selected.WithdrawThresholdPct, Is.EqualTo(25));
        Assert.That(selected.CriticalFlags, Is.EqualTo(0));

        var listLine = PlatformCatalogListProjection.FormatRow(selected);
        Assert.That(listLine, Does.Contain("hp=100"));
        Assert.That(listLine, Does.Contain("withdraw=25"));

        var detail = PlatformCatalogDetailProjection.Format(selected);
        Assert.That(detail.MaxHpLabel, Is.EqualTo("HP: 100"));
        Assert.That(detail.WithdrawThresholdLabel, Is.EqualTo("WITHDRAW: 25%"));
    }

    [Test]
    public void Viewer_host_detail_bind_path_uses_browse_row_projection()
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
        Assert.That(File.Exists(hostPath), Is.True);

        var source = File.ReadAllText(hostPath);
        Assert.That(source, Does.Contain("PlatformCatalogDetailProjection.Format"));
        Assert.That(source, Does.Contain("CatalogPlatformBrowseRow"));
        Assert.That(source, Does.Contain("platform-catalog-detail-lat"));
        Assert.That(source, Does.Contain("platform-catalog-detail-resilience"));
        Assert.That(source, Does.Contain("platform-catalog-detail-withdraw"));
        Assert.That(source, Does.Contain("platform-catalog-detail-flags"));
        Assert.That(source, Does.Contain("platform-catalog-detail-speed"));
        Assert.That(source, Does.Contain("PlatformCatalogListProjection.FormatRow"));
        Assert.That(source, Does.Contain("platform-catalog-export"));
        Assert.That(source, Does.Contain("PlatformCatalogExportBridge"));
    }

    [Test]
    public void Export_trigger_with_baltic_fixture_produces_workbook_via_read_only_bridge()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-viewer-export-{Guid.NewGuid():N}.db");
        var outPath = Path.Combine(Path.GetTempPath(), $"aegis-viewer-export-{Guid.NewGuid():N}.platform.txt");
        try
        {
            CatalogSeedBootstrap.SeedBalticPatrol(dbPath, overwrite: true);

            var workbook = ProjectAegis.Delegation.UnityAdapter.Bridge.PlatformCatalogExportBridge.ExportBalticWorkbook(
                dbPath,
                clockTicks: 9920);
            Assert.That(workbook.Sheets, Is.Not.Empty);

            ProjectAegis.Delegation.UnityAdapter.Bridge.PlatformCatalogExportBridge.ExportBalticToFile(
                dbPath,
                outPath,
                clockTicks: 9921);
            Assert.That(File.Exists(outPath), Is.True);
        }
        finally
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            if (File.Exists(dbPath))
            {
                File.Delete(dbPath);
            }

            if (File.Exists(outPath))
            {
                File.Delete(outPath);
            }
        }
    }

    [Test]
    public void Viewer_projection_path_has_no_write_gate_types()
    {
        AssertNoWriteGateTypes(typeof(CatalogPlatformBrowseProjection));
        AssertNoWriteGateTypes(typeof(PlatformCatalogFilterProjection));
        AssertNoWriteGateTypes(typeof(PlatformCatalogDetailProjection));

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
        Assert.That(File.Exists(hostPath), Is.True);

        var source = File.ReadAllText(hostPath);
        foreach (var token in new[] { "IWriteGate", "CatalogWriteGate", "Propose", "ApproveBatch", "SqliteConnection" })
        {
            Assert.That(source, Does.Not.Contain(token), $"Viewer host must not reference {token}");
        }

        Assert.That(source, Does.Contain("PlatformCatalogExportBridge"));
        Assert.That(source, Does.Not.Contain("PlatformWorkbookWriteBridge"));
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