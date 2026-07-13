using System.Reflection;
using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Map;

/// <summary>
/// Grayscale-safe / shape-primary contract for map symbology (req 20 rev 2 §Map and Symbology —
/// "shape remains the primary cue per design/accessibility-requirements.md §5"; a11y §5
/// "Never: Encode affiliation, comms health, or staging change type by color alone").
/// Asserts <see cref="MapSymbolEntry"/> / <see cref="App6Sidc"/> carry a distinct shape/frame id per
/// affiliation, and that the contract carries no color field an integrator could substitute for shape.
/// </summary>
public sealed class App6GrayscaleAffiliationShapeTests
{
    private static readonly string[] KnownAffiliations = ["Friendly", "Hostile", "Neutral", "Suspect", "Pending"];

    [Test]
    public void MapSymbolEntry_record_carries_no_color_property()
    {
        // Affiliation must be readable from shape/frame identity alone -- if a "Color" property existed
        // on the map symbol contract, a consumer could (incorrectly) treat it as the affiliation cue.
        var properties = typeof(MapSymbolEntry).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        Assert.That(
            properties,
            Has.None.Matches<PropertyInfo>(p => p.Name.Contains("Color", StringComparison.OrdinalIgnoreCase)),
            "MapSymbolEntry must not carry a color field — affiliation is shape/frame-encoded only (a11y §5).");
    }

    [Test]
    public void MapSymbolDisplayRow_record_carries_no_color_property()
    {
        var properties = typeof(MapSymbolDisplayRow).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        Assert.That(
            properties,
            Has.None.Matches<PropertyInfo>(p => p.Name.Contains("Color", StringComparison.OrdinalIgnoreCase)),
            "MapSymbolDisplayRow must not carry a color field — affiliation is shape/frame-encoded only (a11y §5).");
    }

    [Test]
    public void Every_known_affiliation_resolves_a_distinct_uss_frame_id()
    {
        var frameIds = KnownAffiliations
            .Select(a => App6Sidc.ResolveMapGlyph(a).UssFrameId)
            .ToArray();

        Assert.That(frameIds, Is.Unique, "each affiliation must own a distinct frame (shape) id");
        Assert.That(frameIds, Has.All.Not.Empty);
    }

    [Test]
    public void Every_known_affiliation_resolves_a_distinct_unicode_shape_glyph()
    {
        var glyphs = KnownAffiliations
            .Select(a => App6Sidc.ResolveMapGlyph(a).UnicodeGlyph)
            .ToArray();

        Assert.That(glyphs, Is.Unique, "each affiliation must own a distinct fallback shape glyph");
    }

    [Test]
    public void Friendly_is_square_frame_and_hostile_is_diamond_frame_per_a11y_ac()
    {
        // a11y §5 explicit AC pair: friendly "square frame / ■", hostile "diamond / ◆".
        var friendly = App6Sidc.ResolveMapGlyph("Friendly");
        var hostile = App6Sidc.ResolveMapGlyph("Hostile");

        Assert.That(friendly.UssFrameId, Is.EqualTo(App6Sidc.FriendlySurfaceUnitFrame));
        Assert.That(hostile.UssFrameId, Is.EqualTo(App6Sidc.HostileContactFrame));
        Assert.That(friendly.UssFrameId, Is.Not.EqualTo(hostile.UssFrameId));

        // The frame identifiers themselves name the shape family (square vs diamond) so a downstream
        // consumer never needs to fall back to a color to distinguish them.
        Assert.That(App6Sidc.FriendlySurfaceUnitFrame, Does.Contain("friendly"));
        Assert.That(App6Sidc.HostileContactFrame, Does.Contain("hostile"));
    }

    [Test]
    public void Destroyed_friendly_still_resolves_a_distinct_frame_from_live_friendly_and_hostile()
    {
        var liveFriendly = App6Sidc.ResolveMapGlyph("Friendly", isDestroyed: false);
        var destroyedFriendly = App6Sidc.ResolveMapGlyph("Friendly", isDestroyed: true);
        var hostile = App6Sidc.ResolveMapGlyph("Hostile");

        Assert.That(destroyedFriendly.UssFrameId, Is.Not.EqualTo(liveFriendly.UssFrameId));
        Assert.That(destroyedFriendly.UssFrameId, Is.Not.EqualTo(hostile.UssFrameId));
    }

    [Test]
    public void ResolveMapGlyph_display_via_atlas_still_exposes_a_frame_id_independent_of_color()
    {
        // Even when the atlas degrades to unicode fallback (no texture, no color), the frame id used
        // to select the glyph is still affiliation-distinct -- shape survives when color/texture doesn't.
        var friendlyUnavailable = App6GlyphAtlas.ResolveDisplay("Friendly", atlas: App6AtlasCatalog.Unavailable);
        var hostileUnavailable = App6GlyphAtlas.ResolveDisplay("Hostile", atlas: App6AtlasCatalog.Unavailable);

        Assert.That(friendlyUnavailable.Glyph, Is.Not.EqualTo(hostileUnavailable.Glyph));
        Assert.That(friendlyUnavailable.UsesAtlasFrame, Is.False);
        Assert.That(hostileUnavailable.UsesAtlasFrame, Is.False);
    }

    [Test]
    public void Domain_modifier_classes_do_not_overlap_with_affiliation_frame_classes()
    {
        // Domain (Air/Surface/.../Facility) is a secondary cue layered on the frame; it must never
        // collide with or substitute for the primary affiliation shape classes.
        var affiliationFrames = App6Sidc.KnownUssFrameIds.ToHashSet(StringComparer.Ordinal);
        var domainClasses = App6DomainModifier.KnownDomainModifierClasses;

        foreach (var domainClass in domainClasses)
        {
            Assert.That(affiliationFrames, Does.Not.Contain(domainClass));
        }
    }
}
