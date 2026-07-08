using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Map;

/// <summary>
/// Headless contract tests for S25-14 Cesium APP-6 billboard wiring (ADR-007 Phase B/C, ADR-010).
/// </summary>
public sealed class CesiumApp6BillboardContractTests
{
    [Test]
    public void Project_resolves_distinct_app6_frames_for_friendly_and_hostile_symbols()
    {
        var symbols = MapPictureProjection.Project(
            [new OobTreeEntry("u1", true)],
            [new ContactPictureEntry("c1", "hostile-1", "u1", "Detected", 1, 1.0)],
            layoutSeed: 7);

        var markers = CesiumBillboardProjection.Project(symbols, layoutSeed: 7);

        var friendly = markers.Single(m => m.SymbolId == "u1");
        var hostile = markers.Single(m => m.SymbolId == "c1");

        Assert.That(friendly.UssFrameId, Is.EqualTo(App6Sidc.FriendlySurfaceUnitFrame));
        Assert.That(hostile.UssFrameId, Is.EqualTo(App6Sidc.HostileContactFrame));
        Assert.That(friendly.UnicodeGlyph, Is.EqualTo(App6Sidc.FriendlySurfaceUnitGlyph));
        Assert.That(hostile.UnicodeGlyph, Is.EqualTo(App6Sidc.HostileContactGlyph));
        Assert.That(friendly.Sidc, Is.EqualTo(App6Sidc.FriendlySurfaceUnitSidc));
        Assert.That(hostile.Sidc, Is.EqualTo(App6Sidc.HostileContactSidc));
        Assert.That(friendly.UssFrameId, Is.Not.EqualTo(hostile.UssFrameId));
    }

    [Test]
    public void ResolveGlyph_valid_sidc_overrides_affiliation_label()
    {
        var symbol = new MapSymbolEntry(
            "x1",
            "Friendly",
            App6Sidc.FriendlySurfaceUnitGlyph,
            "x1",
            0.5f,
            0.5f,
            IsDestroyed: false,
            App6Sidc.HostileContactSidc,
            App6Sidc.FriendlySurfaceUnitFrame);

        var resolution = CesiumBillboardProjection.ResolveGlyph(symbol);

        Assert.That(resolution.UssFrameId, Is.EqualTo(App6Sidc.HostileContactFrame));
        Assert.That(resolution.UnicodeGlyph, Is.EqualTo(App6Sidc.HostileContactGlyph));
        Assert.That(resolution.Sidc, Is.EqualTo(App6Sidc.HostileContactSidc));
    }

    [Test]
    public void ResolveGlyph_missing_sidc_uses_affiliation_fallback()
    {
        var symbol = new MapSymbolEntry(
            "u1",
            "Friendly",
            App6Sidc.FriendlySurfaceUnitGlyph,
            "u1",
            0.2f,
            0.3f,
            IsDestroyed: false,
            App6Sidc: null,
            App6UssFrameId: null);

        var resolution = CesiumBillboardProjection.ResolveGlyph(symbol);

        Assert.That(resolution.UssFrameId, Is.EqualTo(App6Sidc.FriendlySurfaceUnitFrame));
        Assert.That(resolution.UnicodeGlyph, Is.EqualTo(App6Sidc.FriendlySurfaceUnitGlyph));
    }

    [Test]
    public void ResolveGlyph_unknown_affiliation_without_sidc_uses_fallback_glyph_and_frame()
    {
        var symbol = new MapSymbolEntry(
            "unk",
            "Unknown",
            App6Sidc.FallbackGlyph,
            "unk",
            0.1f,
            0.2f,
            IsDestroyed: false,
            App6Sidc: null,
            App6UssFrameId: null);

        var resolution = CesiumBillboardProjection.ResolveGlyph(symbol);

        Assert.That(resolution.UssFrameId, Is.EqualTo(App6Sidc.FallbackFrame));
        Assert.That(resolution.UnicodeGlyph, Is.EqualTo(App6Sidc.FallbackGlyph));
    }

    [Test]
    public void ResolveGlyph_invalid_sidc_uses_fallback_glyph_and_frame()
    {
        var symbol = new MapSymbolEntry(
            "bad",
            "Hostile",
            App6Sidc.HostileContactGlyph,
            "bad",
            0.4f,
            0.6f,
            IsDestroyed: false,
            "SHORT",
            App6Sidc.HostileContactFrame);

        var resolution = CesiumBillboardProjection.ResolveGlyph(symbol);

        Assert.That(resolution.UssFrameId, Is.EqualTo(App6Sidc.FallbackFrame));
        Assert.That(resolution.UnicodeGlyph, Is.EqualTo(App6Sidc.FallbackGlyph));
        Assert.That(resolution.Sidc, Is.EqualTo(App6Sidc.FallbackSidc));
    }

    [Test]
    public void ProjectDemoPair_matches_baltic_spike_coordinates_with_app6_frames()
    {
        var markers = CesiumBillboardProjection.ProjectDemoPair();

        Assert.That(markers, Has.Count.EqualTo(2));
        Assert.That(markers[0].Latitude, Is.EqualTo(60.17).Within(1e-6));
        Assert.That(markers[0].Longitude, Is.EqualTo(24.94).Within(1e-6));
        Assert.That(markers[1].Latitude, Is.EqualTo(59.95).Within(1e-6));
        Assert.That(markers[1].Longitude, Is.EqualTo(24.50).Within(1e-6));
        Assert.That(markers[0].UssFrameId, Is.EqualTo(App6Sidc.FriendlySurfaceUnitFrame));
        Assert.That(markers[1].UssFrameId, Is.EqualTo(App6Sidc.HostileContactFrame));
    }

    [Test]
    public void CesiumGlobeBridge_source_wires_App6Sidc_and_GetBillboardMarkers()
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
        Assert.That(File.Exists(bridgePath), Is.True);

        var source = File.ReadAllText(bridgePath);
        Assert.That(source, Does.Contain("GetBillboardMarkers"));
        Assert.That(source, Does.Contain("CesiumBillboardProjection"));
        Assert.That(source, Does.Contain("App6Sidc"));
        Assert.That(source, Does.Contain("UssFrameId"));
        Assert.That(source, Does.Contain("UnicodeGlyph"));
        Assert.That(source, Does.Contain("frame="));
        Assert.That(source, Does.Contain("glyph="));
        Assert.That(source, Does.Not.Contain("isHostile ? \"Hostile\" : \"Friendly\""));
    }

    [Test]
    public void MapPlaceholderPanelHost_exposes_CurrentMapSymbols_for_cesium_read_only_feed()
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
            "MapPlaceholderPanelHost.cs");
        var source = File.ReadAllText(hostPath);

        Assert.That(source, Does.Contain("CurrentMapSymbols"));
        Assert.That(source, Does.Contain("LastMapSymbols"));
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