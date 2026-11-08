using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

public sealed class MapSymbolPresentationLerpTests
{
    [Test]
    public void Lerp_midpoint_slides_authoritative_pose()
    {
        var from = new MapSymbolEntry("u1", "Friendly", "■", "u1", 0.2f, 0.2f, false, HasAuthoritativePose: true);
        var to = new MapSymbolEntry("u1", "Friendly", "■", "u1", 0.4f, 0.6f, false, HasAuthoritativePose: true);

        var mid = MapSymbolPresentationLerp.Lerp([from], [to], 0.5f);

        Assert.That(mid[0].NormalizedX, Is.EqualTo(0.3f).Within(1e-5f));
        Assert.That(mid[0].NormalizedY, Is.EqualTo(0.4f).Within(1e-5f));
    }

    [Test]
    public void Lerp_does_not_slide_hash_or_destroyed()
    {
        var from = new MapSymbolEntry("u1", "Friendly", "■", "u1", 0.2f, 0.2f, false);
        var to = new MapSymbolEntry("u1", "Friendly", "■", "u1", 0.8f, 0.8f, false);
        var deadFrom = new MapSymbolEntry("u2", "Friendly", "■", "u2", 0.1f, 0.1f, true, HasAuthoritativePose: true);
        var deadTo = new MapSymbolEntry("u2", "Friendly", "■", "u2", 0.9f, 0.9f, true, HasAuthoritativePose: true);

        var hash = MapSymbolPresentationLerp.Lerp([from], [to], 0.5f);
        var dead = MapSymbolPresentationLerp.Lerp([deadFrom], [deadTo], 0.5f);

        Assert.That(hash[0].NormalizedX, Is.EqualTo(0.8f).Within(1e-6f));
        Assert.That(dead[0].NormalizedX, Is.EqualTo(0.9f).Within(1e-6f));
    }
}
