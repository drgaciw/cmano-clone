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
        var reader = new InMemoryCatalogReader(bindings, "p0-baltic-fixture", platforms);
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
        foreach (var name in new[] { "platform-catalog-root", "platform-catalog-search", "platform-catalog-list" })
        {
            Assert.That(uxml, Does.Contain($"name=\"{name}\""), $"Missing UXML element: {name}");
        }
    }

    [Test]
    public void Viewer_projection_path_has_no_write_gate_types()
    {
        AssertNoWriteGateTypes(typeof(CatalogPlatformBrowseProjection));
        AssertNoWriteGateTypes(typeof(PlatformCatalogFilterProjection));

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
        foreach (var token in new[] { "IWriteGate", "Propose", "ApproveBatch" })
        {
            Assert.That(source, Does.Not.Contain(token), $"Viewer host must not reference {token}");
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