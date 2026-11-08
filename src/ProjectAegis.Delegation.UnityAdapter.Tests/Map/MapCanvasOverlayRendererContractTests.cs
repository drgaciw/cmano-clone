using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Map;

/// <summary>
/// Source contract: canvas overlay renderer uses pixel layout (DRG-163).
/// </summary>
public sealed class MapCanvasOverlayRendererContractTests
{
    [Test]
    public void MapCanvasOverlayRenderer_uses_aspect_correct_pixel_layout()
    {
        var root = FindRepoRoot();
        Assert.That(root, Is.Not.Null);

        var rendererPath = Path.Combine(
            root!,
            "unity",
            "ProjectAegis",
            "Assets",
            "Scripts",
            "Runtime",
            "MapCanvasOverlayRenderer.cs");
        Assert.That(File.Exists(rendererPath), Is.True, rendererPath);

        var text = File.ReadAllText(rendererPath);
        Assert.That(text, Does.Contain("LayoutRingPixels"));
        Assert.That(text, Does.Contain("LayoutEdgePixels"));
        Assert.That(text, Does.Contain("GeometryChangedEvent"));
        Assert.That(text, Does.Not.Contain("MapPlaceholderPanelHost"));
        Assert.That(text, Does.Not.Contain("GlobeMapProductHost"));
        Assert.That(text, Does.Not.Contain("DelegationBridge.Tick"));
        Assert.That(text, Does.Not.Contain("CatalogWriteGate"));
        Assert.That(text, Does.Not.Contain("SimulationSession"));
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
