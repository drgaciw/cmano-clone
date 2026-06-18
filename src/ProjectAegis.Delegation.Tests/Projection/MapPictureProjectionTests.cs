using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

public sealed class MapPictureProjectionTests
{
    [Test]
    public void Place_is_stable_for_same_key_and_seed()
    {
        var a = MapPictureProjection.Place("u1", 42);
        var b = MapPictureProjection.Place("u1", 42);
        Assert.That(b, Is.EqualTo(a));
    }

    [Test]
    public void Project_includes_friendly_and_hostile_symbols()
    {
        var symbols = MapPictureProjection.Project(
            [new OobTreeEntry("u1", true)],
            [
                new ContactPictureEntry("c1", "hostile-1", "u1", "Detected", 1, 1.0),
            ],
            42);
        Assert.That(symbols.Any(s => s.Affiliation == "Friendly"), Is.True);
        Assert.That(symbols.Any(s => s.Affiliation == "Hostile"), Is.True);
    }

    [Test]
    public void Project_uses_app6_distinct_glyphs_and_sidc_fields()
    {
        var symbols = MapPictureProjection.Project(
            [new OobTreeEntry("u1", true)],
            [
                new ContactPictureEntry("c1", "hostile-1", "u1", "Detected", 1, 1.0),
            ],
            42);

        var friendly = symbols.Single(s => s.Affiliation == "Friendly");
        var hostile = symbols.Single(s => s.Affiliation == "Hostile");

        Assert.That(friendly.ShapeGlyph, Is.EqualTo(App6Sidc.FriendlySurfaceUnitGlyph));
        Assert.That(hostile.ShapeGlyph, Is.EqualTo(App6Sidc.HostileContactGlyph));
        Assert.That(friendly.ShapeGlyph, Is.Not.EqualTo(hostile.ShapeGlyph));
        Assert.That(App6Sidc.IsValidSidc(friendly.App6Sidc), Is.True);
        Assert.That(App6Sidc.IsValidSidc(hostile.App6Sidc), Is.True);
        Assert.That(friendly.App6UssFrameId, Is.EqualTo(App6Sidc.FriendlySurfaceUnitFrame));
        Assert.That(hostile.App6UssFrameId, Is.EqualTo(App6Sidc.HostileContactFrame));
        Assert.That(friendly.App6UssFrameId, Is.Not.EqualTo(hostile.App6UssFrameId));
    }

    [Test]
    public void Project_preserves_deterministic_ordering()
    {
        var symbols = MapPictureProjection.Project(
            [new OobTreeEntry("u2", true), new OobTreeEntry("u1", true)],
            [
                new ContactPictureEntry("c2", "hostile-2", "u1", "Detected", 1, 1.0),
                new ContactPictureEntry("c1", "hostile-1", "u1", "Detected", 1, 1.0),
            ],
            7);

        var ids = symbols.Select(s => s.SymbolId).ToArray();
        Assert.That(ids, Is.EqualTo(new[] { "u1", "u2", "c1", "c2" }));
    }
}