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
    public void Resolve_neutral_affiliation_uses_neutral_registry_entry()
    {
        var neutral = App6Sidc.Resolve("Neutral");

        Assert.That(neutral.Glyph, Is.EqualTo(App6Sidc.NeutralUnitGlyph));
        Assert.That(neutral.Sidc, Is.EqualTo(App6Sidc.NeutralUnitSidc));
        Assert.That(neutral.Glyph, Is.Not.EqualTo(App6Sidc.FallbackGlyph));
    }

    [Test]
    public void Resolve_unknown_affiliation_falls_back()
    {
        var unknown = App6Sidc.Resolve("Unrecognized");

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

    [Test]
    public void ResolveMapGlyph_friendly_and_hostile_have_distinct_uss_frame_ids()
    {
        var friendly = App6Sidc.ResolveMapGlyph("Friendly");
        var hostile = App6Sidc.ResolveMapGlyph("Hostile");

        Assert.That(friendly.UssFrameId, Is.EqualTo(App6Sidc.FriendlySurfaceUnitFrame));
        Assert.That(hostile.UssFrameId, Is.EqualTo(App6Sidc.HostileContactFrame));
        Assert.That(friendly.UssFrameId, Is.Not.EqualTo(hostile.UssFrameId));
        Assert.That(App6Sidc.KnownUssFrameIds, Does.Contain(friendly.UssFrameId));
        Assert.That(App6Sidc.KnownUssFrameIds, Does.Contain(hostile.UssFrameId));
    }

    [Test]
    public void ResolveMapGlyph_destroyed_friendly_uses_destroyed_frame()
    {
        var destroyed = App6Sidc.ResolveMapGlyph("Friendly", isDestroyed: true);

        Assert.That(destroyed.UssFrameId, Is.EqualTo(App6Sidc.FriendlyDestroyedFrame));
        Assert.That(destroyed.UnicodeGlyph, Is.EqualTo(App6Sidc.FriendlyDestroyedGlyph));
    }

    [Test]
    public void ResolveMapGlyphFromSidc_unknown_identity_uses_fallback_frame()
    {
        var fallback = App6Sidc.ResolveMapGlyphFromSidc("SUZPU----------");

        Assert.That(fallback.UssFrameId, Is.EqualTo(App6Sidc.FallbackFrame));
        Assert.That(fallback.UnicodeGlyph, Is.EqualTo(App6Sidc.FallbackGlyph));
    }

    [Test]
    public void ResolveMapGlyph_suspect_and_pending_have_distinct_frames()
    {
        var suspect = App6Sidc.ResolveMapGlyph("Suspect");
        var pending = App6Sidc.ResolveMapGlyph("Pending");

        Assert.That(suspect.UssFrameId, Is.EqualTo(App6Sidc.SuspectContactFrame));
        Assert.That(pending.UssFrameId, Is.EqualTo(App6Sidc.PendingUnitFrame));
        Assert.That(suspect.UnicodeGlyph, Is.EqualTo(App6Sidc.SuspectContactGlyph));
        Assert.That(pending.UnicodeGlyph, Is.EqualTo(App6Sidc.PendingUnitGlyph));
        Assert.That(App6Sidc.KnownUssFrameIds, Has.Count.EqualTo(7));
    }

    [Test]
    public void ResolveMapGlyphFromSidc_suspect_identity_maps_to_suspect_not_hostile()
    {
        var resolution = App6Sidc.ResolveMapGlyphFromSidc(App6Sidc.SuspectContactSidc);

        Assert.That(resolution.UssFrameId, Is.EqualTo(App6Sidc.SuspectContactFrame));
        Assert.That(resolution.UssFrameId, Is.Not.EqualTo(App6Sidc.HostileContactFrame));
    }
}