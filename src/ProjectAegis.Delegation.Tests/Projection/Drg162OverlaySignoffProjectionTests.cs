using ProjectAegis.Data.Catalog;
using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

/// <summary>
/// Headless DRG-162 composition: selected-unit rings + smoke-ORBAT datalink edge + HUD counts.
/// </summary>
public sealed class Drg162OverlaySignoffProjectionTests
{
    [Test]
    public void Smoke_orbat_pair_with_catalog_projects_rings_edge_and_matching_hud_counts()
    {
        var catalog = InMemoryCatalogReader.BalticPatrolFixture();
        var selected = "u1";
        var (sensorNm, weaponNm) = CatalogEnvelopeRangeResolver.ResolveSelectedUnitRanges(
            catalog,
            selected,
            CatalogWeaponIds.MvpDefault);

        var rings = TacticalOverlayProjection.ProjectSelectedUnitEnvelopes(selected, sensorNm, weaponNm);
        var edges = DatalinkUnitPairFeed.ProjectEdges(
            ["u1", "hostile-1"],
            catalog.GetSortedLinks());

        var symbols = MapPictureProjection.Project(
            [new OobTreeEntry("u1", true), new OobTreeEntry("hostile-1", true)],
            Array.Empty<ContactPictureEntry>(),
            layoutSeed: 42);
        var state = MapPanelBinder.Bind(symbols, "baltic-patrol", selected, selectedContactId: null);
        var positions = MapCanvasOverlayGeometry.BuildUnitPositionIndex(state.Symbols);
        var ringShapes = MapCanvasOverlayGeometry.ProjectRings(rings, positions);
        var edgeShapes = MapCanvasOverlayGeometry.ProjectEdges(edges, positions);
        var hud = MapPanelApplyState.Apply(state, rings, edges);

        Assert.That(rings, Has.Count.EqualTo(2));
        Assert.That(rings[0].RingKind, Is.EqualTo(TacticalOverlayProjection.RingKindSensor));
        Assert.That(rings[1].RingKind, Is.EqualTo(TacticalOverlayProjection.RingKindWeapon));
        Assert.That(edges, Has.Count.EqualTo(1));
        Assert.That(ringShapes, Has.Count.EqualTo(2));
        Assert.That(edgeShapes, Has.Count.EqualTo(1));
        Assert.That(hud.EnvelopeRingCount, Is.EqualTo(ringShapes.Count));
        Assert.That(hud.DatalinkEdgeCount, Is.EqualTo(edgeShapes.Count));
        Assert.That(ringShapes[0].StyleClass, Is.EqualTo(MapCanvasOverlayGeometry.RingStyleSensor));
        Assert.That(ringShapes[1].StyleClass, Is.EqualTo(MapCanvasOverlayGeometry.RingStyleWeapon));
    }

    [Test]
    public void Null_catalog_still_projects_fallback_rings_but_no_datalink_edges()
    {
        var (sensorNm, weaponNm) = CatalogEnvelopeRangeResolver.ResolveSelectedUnitRanges(
            catalog: null,
            unitId: "u1",
            CatalogWeaponIds.MvpDefault);
        var rings = TacticalOverlayProjection.ProjectSelectedUnitEnvelopes("u1", sensorNm, weaponNm);

        Assert.That(rings, Has.Count.EqualTo(2));
        Assert.That(sensorNm, Is.EqualTo(CatalogEnvelopeRangeResolver.DefaultSensorRangeNm));
        Assert.That(weaponNm, Is.EqualTo(CatalogEnvelopeRangeResolver.DefaultWeaponRangeNm));
        Assert.That(
            DatalinkUnitPairFeed.ProjectEdges(["u1", "hostile-1"], Array.Empty<CatalogLinkEntry>()),
            Is.Empty);
    }
}
