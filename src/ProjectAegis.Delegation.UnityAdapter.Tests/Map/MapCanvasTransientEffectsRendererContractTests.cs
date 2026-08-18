using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Map;

/// <summary>
/// Source contract: Track C transient VFX stays off static rings and off sim authority.
/// </summary>
public sealed class MapCanvasTransientEffectsRendererContractTests
{
    [Test]
    public void Transient_renderer_is_a_separate_layer_from_static_rings()
    {
        var root = FindRepoRoot();
        Assert.That(root, Is.Not.Null);

        var transientPath = Path.Combine(
            root!,
            "unity",
            "ProjectAegis",
            "Assets",
            "Scripts",
            "Runtime",
            "MapCanvasTransientEffectsRenderer.cs");
        var overlayPath = Path.Combine(
            root!,
            "unity",
            "ProjectAegis",
            "Assets",
            "Scripts",
            "Runtime",
            "MapCanvasOverlayRenderer.cs");

        Assert.That(File.Exists(transientPath), Is.True, transientPath);
        Assert.That(File.Exists(overlayPath), Is.True, overlayPath);

        var transient = File.ReadAllText(transientPath);
        var overlay = File.ReadAllText(overlayPath);

        Assert.That(transient, Does.Contain("map-combat-vfx-line-layer"));
        Assert.That(transient, Does.Contain("map-combat-vfx-marker-layer"));
        Assert.That(transient, Does.Contain("CombatVfxFrame"));
        Assert.That(transient, Does.Contain("LayoutEdgePixels"));
        Assert.That(transient, Does.Not.Contain("Bridge.Tick("));
        Assert.That(transient, Does.Not.Contain("CatalogWriteGate"));
        Assert.That(transient, Does.Not.Contain("Random.Shared"));

        Assert.That(overlay, Does.Not.Contain("CombatVfx"));
        Assert.That(overlay, Does.Not.Contain("map-combat-vfx"));
        Assert.That(overlay, Does.Not.Contain("MapCanvasTransientEffectsRenderer"));
    }

    [Test]
    public void Host_wires_projection_after_tick_not_inside_bridge()
    {
        var root = FindRepoRoot();
        Assert.That(root, Is.Not.Null);

        var hostPath = Path.Combine(
            root!,
            "unity",
            "ProjectAegis",
            "Assets",
            "Scripts",
            "Runtime",
            "DelegationBridgeHost.cs");
        var mapPath = Path.Combine(
            root!,
            "unity",
            "ProjectAegis",
            "Assets",
            "Scripts",
            "Runtime",
            "MapPlaceholderPanelHost.cs");
        var projectionPath = Path.Combine(
            root!,
            "src",
            "ProjectAegis.Delegation",
            "Projection",
            "CombatVfxProjection.cs");

        var host = File.ReadAllText(hostPath);
        var map = File.ReadAllText(mapPath);
        var projection = File.ReadAllText(projectionPath);

        Assert.That(host, Does.Contain("LastCombatVfx = CombatVfxProjection.Project"));
        Assert.That(host, Does.Contain("LastMapSymbols"));
        Assert.That(map, Does.Contain("MapCanvasTransientEffectsRenderer"));
        Assert.That(map, Does.Contain("LastCombatVfx"));
        Assert.That(projection, Does.Contain("PkDraw"));
        Assert.That(projection, Does.Contain("ignored"));
        Assert.That(projection, Does.Not.Contain("Random"));
    }

    private static string? FindRepoRoot()
    {
        var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (dir != null)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, "unity", "ProjectAegis")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        return null;
    }
}
