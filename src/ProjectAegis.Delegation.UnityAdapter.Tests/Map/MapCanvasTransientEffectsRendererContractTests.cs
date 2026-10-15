using NUnit.Framework;
using ProjectAegis.Delegation.CombatEvents;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Bridge;

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

        var buildAt = host.IndexOf("LastCombatFrame = CombatPresentationFrameBridge.Build", StringComparison.Ordinal);
        var projectAt = host.IndexOf("LastCombatVfx = CombatVfxProjection.Project", StringComparison.Ordinal);
        Assert.That(buildAt, Is.GreaterThanOrEqualTo(0));
        Assert.That(projectAt, Is.GreaterThan(buildAt));
        Assert.That(host, Does.Contain("LastCombatVfx = CombatVfxProjection.Project"));
        Assert.That(host, Does.Contain("LastCombatFrame.Events"));
        Assert.That(host, Does.Contain("LastMapSymbols"));
        Assert.That(map, Does.Contain("CombatMapView"));
        Assert.That(map, Does.Contain("DisplayCombatFrame"));
        Assert.That(map, Does.Contain("new MapCanvasTransientEffectsRenderer"));
        Assert.That(map, Does.Contain("CombatVfxFrame.Empty"));
        Assert.That(map, Does.Contain("_transientEffects?.Sync(vfx)"));
        Assert.That(host, Does.Contain("LastCombatFrame = CombatPresentationFrameBridge.Build"));
        Assert.That(projection, Does.Contain("CombatEventSnapshot"));
        Assert.That(projection, Does.Contain("TerminalOutcome"));
        Assert.That(projection, Does.Not.Contain("DecisionLog"));
        Assert.That(projection, Does.Not.Contain("PkDraw"));
        Assert.That(projection, Does.Not.Contain("Random"));
    }

    [Test]
    public void Project_resolves_contact_pose_when_contact_id_differs_from_target_id()
    {
        var contacts = new[]
        {
            new ContactPictureEntry("contact-7", "target-2", "shooter", "Identified", 2, 2),
        };
        var symbols = new[]
        {
            new MapSymbolEntry("shooter", "Friendly", "■", "shooter", 0.2f, 0.3f, IsDestroyed: false),
            new MapSymbolEntry("contact-7", "Hostile", "◆", "contact-7", 0.8f, 0.7f, IsDestroyed: false),
        };
        var events = new CombatEventSnapshot(new[]
        {
            new CombatEvent(
                CombatEventPhase.TerminalOutcome,
                "shooter",
                "target-2",
                "Gun",
                "Hit",
                7,
                2,
                2,
                "contact-alias"),
        });

        var unresolved = CombatVfxProjection.Project(events, symbols, 2);
        Assert.That(unresolved, Is.EqualTo(CombatVfxFrame.Empty));

        var positions = CombatVfxProjection.BuildPositionIndex(
            CombatMapSymbolBridge.Build(symbols, contacts));
        var frame = CombatVfxProjection.Project(events, positions, 2);

        Assert.That(frame.FireLines, Has.Count.EqualTo(1));
        Assert.That(frame.ImpactMarkers, Has.Count.EqualTo(1));
        Assert.That(frame.FireLines[0].TargetUnitId, Is.EqualTo("target-2"));
        Assert.That(frame.FireLines[0].ToX, Is.EqualTo(0.8f).Within(1e-6f));
        Assert.That(frame.FireLines[0].ToY, Is.EqualTo(0.7f).Within(1e-6f));
        Assert.That(frame.ImpactMarkers[0].X, Is.EqualTo(0.8f).Within(1e-6f));
        Assert.That(symbols.Any(symbol => symbol.SymbolId == "target-2"), Is.False);

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
        var compact = string.Concat(host.Where(ch => !char.IsWhiteSpace(ch)));
        var aliasAt = compact.IndexOf(
            "CombatMapSymbolBridge.Build(LastMapSymbols,LastCombatFrame.Contacts.Contacts)",
            StringComparison.Ordinal);
        var indexAt = compact.IndexOf("CombatVfxProjection.BuildPositionIndex", StringComparison.Ordinal);
        var projectAt = compact.IndexOf("LastCombatVfx=CombatVfxProjection.Project", StringComparison.Ordinal);
        Assert.That(aliasAt, Is.GreaterThanOrEqualTo(0));
        Assert.That(indexAt, Is.GreaterThan(aliasAt));
        Assert.That(projectAt, Is.GreaterThan(indexAt));
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
