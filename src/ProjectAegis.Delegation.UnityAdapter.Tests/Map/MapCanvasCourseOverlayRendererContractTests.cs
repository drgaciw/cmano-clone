using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Map;

/// <summary>
/// Source contract: Track B course polylines stay off Track C VFX and off sim authority.
/// </summary>
public sealed class MapCanvasCourseOverlayRendererContractTests
{
    [Test]
    public void Course_renderer_is_a_separate_layer_from_rings_and_vfx()
    {
        var root = FindRepoRoot();
        Assert.That(root, Is.Not.Null);

        var coursePath = Path.Combine(
            root!,
            "unity",
            "ProjectAegis",
            "Assets",
            "Scripts",
            "Runtime",
            "MapCanvasCourseOverlayRenderer.cs");
        var overlayPath = Path.Combine(
            root!,
            "unity",
            "ProjectAegis",
            "Assets",
            "Scripts",
            "Runtime",
            "MapCanvasOverlayRenderer.cs");
        var transientPath = Path.Combine(
            root!,
            "unity",
            "ProjectAegis",
            "Assets",
            "Scripts",
            "Runtime",
            "MapCanvasTransientEffectsRenderer.cs");

        Assert.That(File.Exists(coursePath), Is.True, coursePath);
        var course = File.ReadAllText(coursePath);
        var overlay = File.ReadAllText(overlayPath);
        var transient = File.ReadAllText(transientPath);

        Assert.That(course, Does.Contain("map-overlay-course-layer"));
        Assert.That(course, Does.Contain("LayoutEdgePixels"));
        Assert.That(course, Does.Not.Contain("Bridge.Tick("));
        Assert.That(course, Does.Not.Contain("CatalogWriteGate"));
        Assert.That(course, Does.Not.Contain("CombatVfx"));
        Assert.That(course, Does.Not.Contain("Random.Shared"));

        Assert.That(overlay, Does.Not.Contain("MapCanvasCourseOverlayRenderer"));
        Assert.That(overlay, Does.Not.Contain("map-overlay-course-layer"));
        Assert.That(transient, Does.Not.Contain("MapCanvasCourseOverlayRenderer"));
    }

    [Test]
    public void Host_wires_courses_after_tick_not_inside_bridge()
    {
        var root = FindRepoRoot();
        Assert.That(root, Is.Not.Null);

        var host = File.ReadAllText(Path.Combine(
            root!,
            "unity",
            "ProjectAegis",
            "Assets",
            "Scripts",
            "Runtime",
            "DelegationBridgeHost.cs"));
        var map = File.ReadAllText(Path.Combine(
            root!,
            "unity",
            "ProjectAegis",
            "Assets",
            "Scripts",
            "Runtime",
            "MapPlaceholderPanelHost.cs"));
        var sim = File.ReadAllText(Path.Combine(
            root!,
            "unity",
            "ProjectAegis",
            "Assets",
            "Scripts",
            "Runtime",
            "SimplePlayModeSimHost.cs"));

        Assert.That(host, Does.Contain("LastMapCourses = MapPictureBridge.BuildCourses"));
        Assert.That(host, Does.Not.Contain("DelegationBridge.Tick"));
        Assert.That(map, Does.Contain("MapCanvasCourseOverlayRenderer"));
        Assert.That(map, Does.Contain("LastMapCourses"));
        Assert.That(map, Does.Contain("MapCanvasTransientEffectsRenderer"));
        Assert.That(sim, Does.Contain("PlayModeKinematicMover"));
        Assert.That(sim, Does.Contain("TryGetKinematicPose"));
        Assert.That(sim, Does.Not.Contain("CatalogWriteGate"));
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
