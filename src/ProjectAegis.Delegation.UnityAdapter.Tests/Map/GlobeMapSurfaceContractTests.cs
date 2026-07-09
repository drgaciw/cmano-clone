using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Presentation;
using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Map;

/// <summary>
/// ADR-018 thin adapter contracts for Cesium hosts + CI <c>useGlobeMap=false</c> invariant.
/// </summary>
[TestFixture]
public sealed class GlobeMapSurfaceContractTests
{
    [Test]
    public void Headless_default_keeps_UseGlobeMap_false_for_ci()
    {
        var surface = new HeadlessGlobeMapSurface();
        Assert.That(surface.UseGlobeMap, Is.False);
        Assert.That(surface.ActiveTheaterId, Is.EqualTo(TheaterQuickJump.BalticId));
        Assert.That(surface.CurrentSymbols, Is.Empty);
    }

    [Test]
    public void TryJumpToTheater_unknown_is_no_op()
    {
        var surface = new HeadlessGlobeMapSurface();
        Assert.That(surface.TryJumpToTheater("atlantis"), Is.False);
        Assert.That(surface.JumpCount, Is.EqualTo(0));
        Assert.That(surface.ActiveTheaterId, Is.EqualTo(TheaterQuickJump.BalticId));
    }

    [Test]
    public void TryJumpToTheater_baltic_scenario_alias()
    {
        var surface = new HeadlessGlobeMapSurface(initialTheater: TheaterQuickJump.Giuk);
        Assert.That(surface.TryJumpToTheater("baltic-patrol"), Is.True);
        Assert.That(surface.LastJumpedTheater.Id, Is.EqualTo(TheaterQuickJump.BalticId));
        Assert.That(surface.ActiveTheaterBounds, Is.EqualTo(TheaterQuickJump.BalticBounds));
    }

    [Test]
    public void Cesium_host_sources_remain_presentation_only_and_optional()
    {
        var repoRoot = FindRepoRoot();
        Assert.That(repoRoot, Is.Not.Null);

        var bridgePath = Path.Combine(
            repoRoot!,
            "unity",
            "ProjectAegis",
            "Assets",
            "Scripts",
            "Runtime",
            "Cesium",
            "CesiumGlobeBridge.cs");
        var hostPath = Path.Combine(
            repoRoot!,
            "unity",
            "ProjectAegis",
            "Assets",
            "Scripts",
            "Runtime",
            "Cesium",
            "CesiumGlobeHost.cs");

        Assert.That(File.Exists(bridgePath), Is.True);
        Assert.That(File.Exists(hostPath), Is.True);

        var bridge = File.ReadAllText(bridgePath);
        var host = File.ReadAllText(hostPath);

        // Presentation-only: no DelegationBridge / order sink / sim mutation APIs.
        Assert.That(bridge, Does.Not.Contain("DelegationBridge"));
        Assert.That(bridge, Does.Not.Contain("IOrderSink"));
        Assert.That(host, Does.Not.Contain("DelegationBridge"));

        // Production path still optional behind package define + ion token.
        Assert.That(bridge, Does.Contain("CESIUM_FOR_UNITY"));
        Assert.That(host, Does.Contain("CESIUM_FOR_UNITY"));
        Assert.That(bridge, Does.Contain("CesiumBillboardProjection"));
    }

    [Test]
    public void DelegationBridgeHost_source_keeps_useGlobeMap_default_false()
    {
        var repoRoot = FindRepoRoot();
        Assert.That(repoRoot, Is.Not.Null);

        var path = Path.Combine(
            repoRoot!,
            "unity",
            "ProjectAegis",
            "Assets",
            "Scripts",
            "Runtime",
            "DelegationBridgeHost.cs");
        var source = File.ReadAllText(path);

        Assert.That(source, Does.Contain("useGlobeMap"));
        Assert.That(source, Does.Contain("public bool UseGlobeMap => useGlobeMap;"));
        // Field declaration must stay default(false): `[SerializeField] private bool useGlobeMap;`
        // (Inspector may set true in CesiumSpike only — never in source default.)
        Assert.That(
            source,
            Does.Match(@"\[SerializeField\]\s+private\s+bool\s+useGlobeMap\s*;"),
            "useGlobeMap must default false for CI (no `= true` initializer)");
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
