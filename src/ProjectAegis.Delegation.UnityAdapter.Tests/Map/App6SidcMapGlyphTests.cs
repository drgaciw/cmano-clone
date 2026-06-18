using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Map;

public sealed class App6SidcMapGlyphTests
{
    [Test]
    public void ResolveDisplay_with_loaded_atlas_uses_distinct_uss_frames_for_friendly_and_hostile()
    {
        var friendly = App6GlyphAtlas.ResolveDisplay("Friendly", atlas: App6AtlasCatalog.Default);
        var hostile = App6GlyphAtlas.ResolveDisplay("Hostile", atlas: App6AtlasCatalog.Default);

        Assert.That(friendly.UsesAtlasFrame, Is.True);
        Assert.That(hostile.UsesAtlasFrame, Is.True);
        Assert.That(friendly.AtlasFrameClass, Is.EqualTo(App6Sidc.FriendlySurfaceUnitFrame));
        Assert.That(hostile.AtlasFrameClass, Is.EqualTo(App6Sidc.HostileContactFrame));
        Assert.That(friendly.AtlasFrameClass, Is.Not.EqualTo(hostile.AtlasFrameClass));
        Assert.That(friendly.Glyph, Is.Empty);
        Assert.That(hostile.Glyph, Is.Empty);
    }

    [Test]
    public void ResolveDisplay_when_atlas_unavailable_degrades_to_unicode_fallback()
    {
        var friendly = App6GlyphAtlas.ResolveDisplay("Friendly", atlas: App6AtlasCatalog.Unavailable);
        var hostile = App6GlyphAtlas.ResolveDisplay("Hostile", atlas: App6AtlasCatalog.Unavailable);

        Assert.That(friendly.UsesAtlasFrame, Is.False);
        Assert.That(hostile.UsesAtlasFrame, Is.False);
        Assert.That(friendly.Glyph, Is.EqualTo(App6Sidc.FriendlySurfaceUnitGlyph));
        Assert.That(hostile.Glyph, Is.EqualTo(App6Sidc.HostileContactGlyph));
        Assert.That(friendly.AtlasFrameClass, Is.Null);
        Assert.That(hostile.AtlasFrameClass, Is.Null);
    }

    [Test]
    public void ResolveDisplay_when_frame_missing_degrades_to_unicode_fallback()
    {
        var partialAtlas = new App6AtlasCatalog([App6Sidc.FriendlySurfaceUnitFrame], isLoaded: true);

        var friendly = App6GlyphAtlas.ResolveDisplay("Friendly", atlas: partialAtlas);
        var hostile = App6GlyphAtlas.ResolveDisplay("Hostile", atlas: partialAtlas);

        Assert.That(friendly.UsesAtlasFrame, Is.True);
        Assert.That(hostile.UsesAtlasFrame, Is.False);
        Assert.That(hostile.Glyph, Is.EqualTo(App6Sidc.HostileContactGlyph));
    }

    [Test]
    public void ResolveDisplayFromSidc_invalid_sidc_uses_fallback_glyph()
    {
        var display = App6GlyphAtlas.ResolveDisplayFromSidc("SHORT", atlas: App6AtlasCatalog.Default);

        Assert.That(display.UsesAtlasFrame, Is.True);
        Assert.That(display.AtlasFrameClass, Is.EqualTo(App6Sidc.FallbackFrame));
        Assert.That(display.Glyph, Is.Empty);
    }

    [Test]
    public void MapPanelBinder_with_default_atlas_emits_frame_classes_on_projected_symbols()
    {
        var symbols = MapPictureProjection.Project(
            [new OobTreeEntry("u1", true)],
            [new ContactPictureEntry("c1", "hostile-1", "u1", "Detected", 1, 1.0)],
            layoutSeed: 42);

        var state = MapPanelBinder.Bind(symbols, "baltic-patrol-app6-atlas", atlas: App6AtlasCatalog.Default);

        var friendly = state.Symbols.Single(s => s.SymbolId == "u1");
        var hostile = state.Symbols.Single(s => s.SymbolId == "c1");

        Assert.That(friendly.UsesAtlasFrame, Is.True);
        Assert.That(hostile.UsesAtlasFrame, Is.True);
        Assert.That(friendly.AtlasFrameClass, Is.EqualTo(App6Sidc.FriendlySurfaceUnitFrame));
        Assert.That(hostile.AtlasFrameClass, Is.EqualTo(App6Sidc.HostileContactFrame));
        Assert.That(friendly.AtlasFrameClass, Is.Not.EqualTo(hostile.AtlasFrameClass));
    }

    [Test]
    public void ResolveDisplay_neutral_and_suspect_use_distinct_atlas_frames_when_loaded()
    {
        var neutral = App6GlyphAtlas.ResolveDisplay("Neutral", atlas: App6AtlasCatalog.Default);
        var suspect = App6GlyphAtlas.ResolveDisplay("Suspect", atlas: App6AtlasCatalog.Default);

        Assert.That(neutral.UsesAtlasFrame, Is.True);
        Assert.That(suspect.UsesAtlasFrame, Is.True);
        Assert.That(neutral.AtlasFrameClass, Is.EqualTo(App6Sidc.NeutralUnitFrame));
        Assert.That(suspect.AtlasFrameClass, Is.EqualTo(App6Sidc.SuspectContactFrame));
        Assert.That(neutral.AtlasFrameClass, Is.Not.EqualTo(suspect.AtlasFrameClass));
    }

    [Test]
    public void MapPanelBinder_with_unavailable_atlas_emits_unicode_glyphs()
    {
        var symbols = MapPictureProjection.Project(
            [new OobTreeEntry("u1", true)],
            [new ContactPictureEntry("c1", "hostile-1", "u1", "Detected", 1, 1.0)],
            layoutSeed: 42);

        var state = MapPanelBinder.Bind(symbols, "baltic-patrol-app6-fallback", atlas: App6AtlasCatalog.Unavailable);

        var friendly = state.Symbols.Single(s => s.SymbolId == "u1");
        var hostile = state.Symbols.Single(s => s.SymbolId == "c1");

        Assert.That(friendly.UsesAtlasFrame, Is.False);
        Assert.That(hostile.UsesAtlasFrame, Is.False);
        Assert.That(friendly.Glyph, Is.EqualTo(App6Sidc.FriendlySurfaceUnitGlyph));
        Assert.That(hostile.Glyph, Is.EqualTo(App6Sidc.HostileContactGlyph));
    }
}