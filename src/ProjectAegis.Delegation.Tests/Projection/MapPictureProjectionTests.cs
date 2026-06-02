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
}