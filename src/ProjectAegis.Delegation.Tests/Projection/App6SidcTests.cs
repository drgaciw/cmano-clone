using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

public sealed class App6SidcTests
{
    [Test]
    public void Resolve_friendly_and_hostile_produce_distinct_glyphs()
    {
        var friendly = App6Sidc.Resolve("Friendly");
        var hostile = App6Sidc.Resolve("Hostile");

        Assert.That(friendly.Glyph, Is.EqualTo(App6Sidc.FriendlySurfaceUnitGlyph));
        Assert.That(hostile.Glyph, Is.EqualTo(App6Sidc.HostileContactGlyph));
        Assert.That(friendly.Glyph, Is.Not.EqualTo(hostile.Glyph));
    }

    [Test]
    public void Resolve_includes_15_char_sidc_strings_for_atlas()
    {
        var friendly = App6Sidc.Resolve("Friendly");
        var hostile = App6Sidc.Resolve("Hostile");

        Assert.That(App6Sidc.IsValidSidc(friendly.Sidc), Is.True);
        Assert.That(App6Sidc.IsValidSidc(hostile.Sidc), Is.True);
        Assert.That(friendly.Sidc, Is.EqualTo(App6Sidc.FriendlySurfaceUnitSidc));
        Assert.That(hostile.Sidc, Is.EqualTo(App6Sidc.HostileContactSidc));
        Assert.That(friendly.Sidc[1], Is.EqualTo('F'));
        Assert.That(hostile.Sidc[1], Is.EqualTo('H'));
    }

    [Test]
    public void Resolve_destroyed_friendly_uses_hollow_glyph()
    {
        var destroyed = App6Sidc.Resolve("Friendly", isDestroyed: true);

        Assert.That(destroyed.Glyph, Is.EqualTo(App6Sidc.FriendlyDestroyedGlyph));
        Assert.That(destroyed.Glyph, Is.Not.EqualTo(App6Sidc.HostileContactGlyph));
    }

    [Test]
    public void Resolve_unknown_affiliation_falls_back()
    {
        var unknown = App6Sidc.Resolve("Neutral");

        Assert.That(unknown.Glyph, Is.EqualTo(App6Sidc.FallbackGlyph));
        Assert.That(unknown.Sidc, Is.EqualTo(App6Sidc.FallbackSidc));
    }

    [Test]
    public void ResolveGlyphFromSidc_invalid_or_missing_uses_fallback()
    {
        Assert.That(App6Sidc.ResolveGlyphFromSidc(null), Is.EqualTo(App6Sidc.FallbackGlyph));
        Assert.That(App6Sidc.ResolveGlyphFromSidc(""), Is.EqualTo(App6Sidc.FallbackGlyph));
        Assert.That(App6Sidc.ResolveGlyphFromSidc("SHORT"), Is.EqualTo(App6Sidc.FallbackGlyph));
        Assert.That(App6Sidc.ResolveGlyphFromSidc("XFGPU----------"), Is.EqualTo(App6Sidc.FallbackGlyph));
    }

    [Test]
    public void ResolveGlyphFromSidc_parses_standard_identity_prefix()
    {
        Assert.That(
            App6Sidc.ResolveGlyphFromSidc(App6Sidc.FriendlySurfaceUnitSidc),
            Is.EqualTo(App6Sidc.FriendlySurfaceUnitGlyph));
        Assert.That(
            App6Sidc.ResolveGlyphFromSidc(App6Sidc.HostileContactSidc),
            Is.EqualTo(App6Sidc.HostileContactGlyph));
    }
}