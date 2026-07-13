using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Presentation;
using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Map;

/// <summary>
/// TR-c2-004 drag-box on globe surface: lat/lon corners → NDC →
/// <see cref="SelectionBoxResolver"/> → <see cref="C2PresentationController.SelectFriendlyUnits"/>.
/// </summary>
[TestFixture]
public sealed class GlobeDragBoxSelectionTests
{
    private static readonly GeographicBounds Baltic = TheaterQuickJump.BalticBounds;

    private static MapSymbolEntry[] Symbols() =>
    [
        new MapSymbolEntry("u1", "Friendly", "■", "u1", 0.2f, 0.2f, false),
        new MapSymbolEntry("u2", "Friendly", "■", "u2", 0.5f, 0.5f, false),
        new MapSymbolEntry("u3", "Friendly", "■", "u3", 0.9f, 0.9f, false),
        new MapSymbolEntry("c1", "Hostile", "◆", "c1", 0.25f, 0.25f, false),
        new MapSymbolEntry("u-dead", "Friendly", "■", "u-dead", 0.3f, 0.3f, true),
    ];

    [Test]
    public void LatLon_drag_box_reuses_SelectionBoxResolver_for_friendly_ids_in_input_order()
    {
        // NDC rect roughly [0.1,0.1]–[0.6,0.6] in lat/lon via mapper inverse.
        var sw = GlobeCoordinateMapper.ToGeographic(0.1f, 0.1f, Baltic)!.Value;
        var ne = GlobeCoordinateMapper.ToGeographic(0.6f, 0.6f, Baltic)!.Value;

        var rect = GlobeCoordinateMapper.ToNormalizedRect(
            sw.Latitude, sw.Longitude, ne.Latitude, ne.Longitude, Baltic);
        Assert.That(rect, Is.Not.Null);
        Assert.That(rect!.Value.IsDrag, Is.True);

        var ids = SelectionBoxResolver.Resolve(rect.Value, Symbols());
        Assert.That(ids, Is.EqualTo(new[] { "u1", "u2" }),
            "hostile + destroyed excluded; u3 outside rect; input order preserved");
    }

    [Test]
    public void Globe_drag_box_apply_replaces_SelectionSet_on_controller()
    {
        var controller = new C2PresentationController();
        controller.SelectFriendlyUnit("stale");

        var sw = GlobeCoordinateMapper.ToGeographic(0.1f, 0.1f, Baltic)!.Value;
        var ne = GlobeCoordinateMapper.ToGeographic(0.6f, 0.6f, Baltic)!.Value;
        var rect = GlobeCoordinateMapper.ToNormalizedRect(
            sw.Latitude, sw.Longitude, ne.Latitude, ne.Longitude, Baltic)!.Value;

        var ids = SelectionBoxResolver.Resolve(rect, Symbols());
        controller.SelectFriendlyUnits(ids);

        Assert.That(controller.Selection.OrderedTargetIds, Is.EqualTo(new[] { "u1", "u2" }));
        Assert.That(controller.SelectedUnitId, Is.EqualTo("u1"));
    }

    [Test]
    public void Globe_shift_drag_box_AddFriendlyUnits_unions_into_existing_multi_select()
    {
        var controller = new C2PresentationController();
        controller.SelectFriendlyUnits(new[] { "u3" });

        var sw = GlobeCoordinateMapper.ToGeographic(0.1f, 0.1f, Baltic)!.Value;
        var ne = GlobeCoordinateMapper.ToGeographic(0.6f, 0.6f, Baltic)!.Value;
        var rect = GlobeCoordinateMapper.ToNormalizedRect(
            sw.Latitude, sw.Longitude, ne.Latitude, ne.Longitude, Baltic)!.Value;
        var ids = SelectionBoxResolver.Resolve(rect, Symbols());

        controller.AddFriendlyUnits(ids);

        Assert.That(controller.Selection.OrderedTargetIds, Is.EqualTo(new[] { "u3", "u1", "u2" }));
    }

    [Test]
    public void Sub_threshold_globe_rect_is_not_a_drag_click_disambiguation()
    {
        var center = GlobeCoordinateMapper.ToGeographic(0.5f, 0.5f, Baltic)!.Value;
        // Tiny geo delta maps to sub-threshold NDC movement.
        var rect = GlobeCoordinateMapper.ToNormalizedRect(
            center.Latitude,
            center.Longitude,
            center.Latitude + 0.0001,
            center.Longitude + 0.0001,
            Baltic);

        Assert.That(rect, Is.Not.Null);
        Assert.That(rect!.Value.IsDrag, Is.False,
            "hosts must treat sub-threshold globe gesture as click, not marquee");
    }

    [Test]
    public void Headless_surface_jump_updates_bounds_used_for_drag_mapping()
    {
        var surface = new HeadlessGlobeMapSurface(symbols: Symbols());
        Assert.That(surface.TryJumpToTheater("giuk"), Is.True);
        Assert.That(surface.ActiveTheaterId, Is.EqualTo("giuk"));
        Assert.That(surface.JumpCount, Is.EqualTo(1));
        Assert.That(surface.ActiveTheaterBounds, Is.EqualTo(TheaterQuickJump.Giuk.Bounds));

        // Symbols stay in NDC; mapping uses new bounds (still pure / deterministic).
        var sw = GlobeCoordinateMapper.ToGeographic(0.1f, 0.1f, surface.ActiveTheaterBounds)!.Value;
        var ne = GlobeCoordinateMapper.ToGeographic(0.6f, 0.6f, surface.ActiveTheaterBounds)!.Value;
        var rect = GlobeCoordinateMapper.ToNormalizedRect(
            sw.Latitude, sw.Longitude, ne.Latitude, ne.Longitude, surface.ActiveTheaterBounds)!.Value;

        Assert.That(SelectionBoxResolver.Resolve(rect, surface.CurrentSymbols), Is.EqualTo(new[] { "u1", "u2" }));
    }
}
