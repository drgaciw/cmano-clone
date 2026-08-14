using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Map;

/// <summary>
/// Source contract: GlobeMapProductHost binds globe-only overlay projection (DRG-161).
/// </summary>
public sealed class GlobeMapProductHostContractTests
{
    [Test]
    public void GlobeMapProductHost_wires_globe_overlay_projection_and_visual_layer()
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
            "GlobeMapProductHost.cs");
        Assert.That(File.Exists(hostPath), Is.True, hostPath);

        var text = File.ReadAllText(hostPath);

        Assert.That(text, Does.Contain("GlobeOverlayProjection"));
        Assert.That(text, Does.Contain("GlobeOverlayVisualLayer"));
        Assert.That(text, Does.Contain("TacticalOverlayProjection"));
        Assert.That(text, Does.Contain("DatalinkUnitPairFeed"));
        Assert.That(text, Does.Contain("CatalogEnvelopeRangeResolver"));
        Assert.That(text, Does.Contain("CommsStateProjection"));
        Assert.That(text, Does.Not.Contain("MapPlaceholderPanelHost"));
        Assert.That(text, Does.Not.Contain("MapCanvasOverlayRenderer"));
        Assert.That(text, Does.Not.Contain("MapCanvasOverlayGeometry"));
    }

    [Test]
    public void GlobeOverlayVisualLayer_is_globe_only_and_uses_screen_projection()
    {
        var root = FindRepoRoot();
        Assert.That(root, Is.Not.Null);

        var layerPath = Path.Combine(
            root!,
            "unity",
            "ProjectAegis",
            "Assets",
            "Scripts",
            "Runtime",
            "GlobeOverlayVisualLayer.cs");
        Assert.That(File.Exists(layerPath), Is.True, layerPath);

        var text = File.ReadAllText(layerPath);
        Assert.That(text, Does.Contain("GlobeOverlayScreenProjection"));
        Assert.That(text, Does.Contain("generateVisualContent"));
        Assert.That(text, Does.Not.Contain("MapCanvasOverlay"));
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
