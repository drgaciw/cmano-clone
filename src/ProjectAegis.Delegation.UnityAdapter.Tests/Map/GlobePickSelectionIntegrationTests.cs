using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Presentation;
using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Map;

/// <summary>
/// TR-c2-004 + TR-c2-005: globe pick → <see cref="SelectionSet"/> /
/// <see cref="C2PresentationController"/> integration (presentation-only, ADR-010).
/// </summary>
[TestFixture]
public sealed class GlobePickSelectionIntegrationTests
{
    private static readonly GeographicBounds Baltic = TheaterQuickJump.BalticBounds;

    private static MapSymbolEntry[] DemoSymbols() =>
    [
        new MapSymbolEntry("u1", "Friendly", "■", "u1", 0.2f, 0.3f, false),
        new MapSymbolEntry("u2", "Friendly", "■", "u2", 0.8f, 0.7f, false),
        new MapSymbolEntry("c1", "Hostile", "◆", "c1", 0.21f, 0.31f, false),
        new MapSymbolEntry("u-dead", "Friendly", "■", "u-dead", 0.2f, 0.3f, true),
    ];

    [Test]
    public void ResolveNearestFriendly_picks_live_friendly_near_lat_lon()
    {
        var geo = GlobeCoordinateMapper.ToGeographic(0.2f, 0.3f, Baltic)!.Value;
        var id = GlobePickResolver.ResolveNearestFriendly(
            geo.Latitude,
            geo.Longitude,
            Baltic,
            DemoSymbols());

        Assert.That(id, Is.EqualTo("u1"));
    }

    [Test]
    public void ResolveNearestFriendly_ignores_hostile_and_destroyed_even_when_closer()
    {
        // Pick exactly on u1/u-dead/c1 cluster — only live friendly u1 may win.
        var geo = GlobeCoordinateMapper.ToGeographic(0.2f, 0.3f, Baltic)!.Value;
        var id = GlobePickResolver.ResolveNearestFriendly(geo.Latitude, geo.Longitude, Baltic, DemoSymbols());
        Assert.That(id, Is.EqualTo("u1"));
        Assert.That(id, Is.Not.EqualTo("c1"));
        Assert.That(id, Is.Not.EqualTo("u-dead"));
    }

    [Test]
    public void ResolveNearestFriendly_returns_null_outside_hit_radius()
    {
        var geo = GlobeCoordinateMapper.ToGeographic(0.5f, 0.5f, Baltic)!.Value;
        var id = GlobePickResolver.ResolveNearestFriendly(
            geo.Latitude,
            geo.Longitude,
            Baltic,
            DemoSymbols(),
            hitRadiusNdc: 0.01f);

        Assert.That(id, Is.Null);
    }

    [Test]
    public void ApplyPickReplace_selects_unit_on_shared_SelectionSet()
    {
        var controller = new C2PresentationController();
        controller.SelectFriendlyUnit("stale");
        var geo = GlobeCoordinateMapper.ToGeographic(0.8f, 0.7f, Baltic)!.Value;

        var hit = GlobePickResolver.ApplyPickReplace(
            controller,
            geo.Latitude,
            geo.Longitude,
            Baltic,
            DemoSymbols());

        Assert.That(hit, Is.True);
        Assert.That(controller.SelectedUnitId, Is.EqualTo("u2"));
        Assert.That(controller.Selection.OrderedTargetIds, Is.EqualTo(new[] { "u2" }));
        Assert.That(controller.SelectedContactId, Is.Null);
    }

    [Test]
    public void ApplyPickReplace_miss_leaves_selection_unchanged()
    {
        var controller = new C2PresentationController();
        controller.SelectFriendlyUnit("u1");
        var geo = GlobeCoordinateMapper.ToGeographic(0.5f, 0.5f, Baltic)!.Value;

        var hit = GlobePickResolver.ApplyPickReplace(
            controller,
            geo.Latitude,
            geo.Longitude,
            Baltic,
            DemoSymbols(),
            hitRadiusNdc: 0.005f);

        Assert.That(hit, Is.False);
        Assert.That(controller.SelectedUnitId, Is.EqualTo("u1"));
    }

    [Test]
    public void ApplyPickToggle_adds_then_removes_without_disturbing_rest()
    {
        var controller = new C2PresentationController();
        controller.SelectFriendlyUnits(new[] { "u1" });
        var geoU2 = GlobeCoordinateMapper.ToGeographic(0.8f, 0.7f, Baltic)!.Value;

        Assert.That(
            GlobePickResolver.ApplyPickToggle(controller, geoU2.Latitude, geoU2.Longitude, Baltic, DemoSymbols()),
            Is.True);
        Assert.That(controller.Selection.OrderedTargetIds, Is.EqualTo(new[] { "u1", "u2" }));

        Assert.That(
            GlobePickResolver.ApplyPickToggle(controller, geoU2.Latitude, geoU2.Longitude, Baltic, DemoSymbols()),
            Is.True);
        Assert.That(controller.Selection.OrderedTargetIds, Is.EqualTo(new[] { "u1" }));
    }

    [Test]
    public void HeadlessGlobeMapSurface_pick_uses_active_theater_bounds()
    {
        var surface = new HeadlessGlobeMapSurface(useGlobeMap: false, symbols: DemoSymbols());
        Assert.That(surface.UseGlobeMap, Is.False, "CI default must stay false");
        Assert.That(surface.ActiveTheaterId, Is.EqualTo(TheaterQuickJump.BalticId));

        var geo = GlobeCoordinateMapper.ToGeographic(0.2f, 0.3f, surface.ActiveTheaterBounds)!.Value;
        var controller = new C2PresentationController();
        var hit = GlobePickResolver.ApplyPickReplace(
            controller,
            geo.Latitude,
            geo.Longitude,
            surface.ActiveTheaterBounds,
            surface.CurrentSymbols);

        Assert.That(hit, Is.True);
        Assert.That(controller.Selection.PrimaryUnitId, Is.EqualTo("u1"));
    }
}
