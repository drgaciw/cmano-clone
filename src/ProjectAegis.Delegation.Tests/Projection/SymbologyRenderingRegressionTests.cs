using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

/// <summary>PR #694 regressions through map, globe and LOD display consumers.</summary>
[TestFixture]
public sealed class SymbologyRenderingRegressionTests
{
    [TestCase(false)]
    [TestCase(true)]
    public void Map_binder_distinguishes_combatant_from_unknown_surface(bool atlasLoaded)
    {
        var atlas = atlasLoaded ? App6AtlasCatalog.Default : App6AtlasCatalog.Unavailable;
        var combatant = Style("Friendly", SymbolKeyRegistry.NavalSurfaceCombatant);
        var unknown = Style("Friendly", SymbolKeyRegistry.NavalSurfaceUnknown);

        var combatantRow = MapPanelBinder.Bind([combatant], "Baltic", atlas).Symbols.Single();
        var unknownRow = MapPanelBinder.Bind([unknown], "Baltic", atlas).Symbols.Single();

        Assert.That(combatantRow.Glyph, Does.Contain("CBT"));
        Assert.That(unknownRow.Glyph, Does.Contain("?"));
        Assert.That(combatantRow.Glyph, Is.Not.EqualTo(unknownRow.Glyph));
        Assert.That(combatantRow.Label, Is.EqualTo(unknownRow.Label));
    }

    [Test]
    public void Cesium_preserves_military_glyph_frame_and_sidc(
        [Values("Friendly", "Hostile", "Neutral", "Unknown")] string affiliation,
        [Values(SymbolKeyRegistry.NavalSurfaceCombatant, SymbolKeyRegistry.NavalSubsurfaceSubmarine,
            SymbolKeyRegistry.NavalSurfaceUnknown, SymbolKeyRegistry.GenericUnknown)] string key,
        [Values(false, true)] bool destroyed)
    {
        var symbol = Style(affiliation, key, destroyed);
        var marker = CesiumBillboardProjection.ProjectWithCamera(
            [symbol], new GlobeCameraState(60, 25, 1000, 0, 0)).Single();

        Assert.That(marker.UnicodeGlyph, Is.EqualTo(symbol.ShapeGlyph));
        Assert.That(marker.UssFrameId, Is.EqualTo(symbol.App6UssFrameId));
        Assert.That(marker.Sidc, Is.EqualTo(symbol.App6Sidc));
        Assert.That(marker.Latitude, Is.EqualTo(symbol.Latitude));
        Assert.That(marker.Longitude, Is.EqualTo(symbol.Longitude));
        Assert.That(marker.DistanceLabel, Is.Not.Empty);
    }

    [TestCase("Suspect")]
    [TestCase("Pending")]
    public void Cesium_preserves_unknown_military_fallback_for_frozen_affiliations(string affiliation)
    {
        var symbol = Style(affiliation, SymbolKeyRegistry.NavalSubsurfaceSubmarine);
        var marker = CesiumBillboardProjection.Project([symbol]).Single();

        Assert.That(marker.Sidc, Is.EqualTo("SUUPS----------"));
        Assert.That(marker.UssFrameId, Is.EqualTo("map-sym-mil--unknown-subsurface"));
        Assert.That(marker.UnicodeGlyph, Does.Contain("SUB"));
    }

    [Test]
    public void Close_lod_preserves_submarine_frame_and_type_icon()
    {
        var symbol = Style("Friendly", SymbolKeyRegistry.NavalSubsurfaceSubmarine);
        var row = MapSymbolLodClusterer.Cluster([symbol], MapLodBand.Close, gridDivisions: 4).Single();

        Assert.That(row.App6Glyph, Is.EqualTo("◡ SUB"));
    }

    [Test]
    public void Registry_cannot_be_mutated_through_public_collection_interfaces()
    {
        var entries = SymbolKeyRegistry.All;
        var before = SymbologyLegendProjection.Build(SymbologyProfile.MilitaryTactical).Rows.ToArray();

        Assert.That(entries, Is.Not.InstanceOf<SymbolKeyDefinition[]>());
        if (entries is IList<SymbolKeyDefinition> list)
        {
            Assert.That(list.IsReadOnly, Is.True);
            Assert.Throws<NotSupportedException>(() => list[0] = null!);
            Assert.Throws<NotSupportedException>(() => list.Clear());
        }

        Assert.That(SymbologyLegendProjection.Build(SymbologyProfile.MilitaryTactical).Rows, Is.EqualTo(before));
        foreach (var entry in entries)
        {
            Assert.That(SymbolKeyRegistry.ResolveObserved(entry.Key), Is.EqualTo(entry));
        }
    }

    [Test]
    public void Cesium_legacy_sidc_still_overrides_conflicting_glyph_and_frame()
    {
        var symbol = Style("Friendly", SymbolKeyRegistry.NavalSurfaceCombatant) with
        {
            App6Sidc = App6Sidc.HostileContactSidc,
            App6UssFrameId = App6Sidc.FriendlySurfaceUnitFrame,
        };

        Assert.That(CesiumBillboardProjection.ResolveGlyph(symbol),
            Is.EqualTo(App6Sidc.ResolveMapGlyph("Hostile")));
    }

    [Test]
    public void Cesium_invalid_sidc_still_falls_back_with_military_frame()
    {
        var symbol = Style("Friendly", SymbolKeyRegistry.NavalSubsurfaceSubmarine) with { App6Sidc = "SHORT" };

        Assert.That(CesiumBillboardProjection.ResolveGlyph(symbol),
            Is.EqualTo(App6Sidc.ResolveMapGlyph("Unknown")));
    }

    private static MapSymbolEntry Style(string affiliation, string key, bool destroyed = false)
    {
        var input = new MapSymbolEntry("track", affiliation, "", "Track", 0.5f, 0.5f,
            destroyed, Latitude: 60, Longitude: 25);
        return SymbologyProjection.Apply([input], SymbologyProfile.MilitaryTactical,
            new Dictionary<string, string>(StringComparer.Ordinal) { [input.SymbolId] = key }).Single();
    }
}
