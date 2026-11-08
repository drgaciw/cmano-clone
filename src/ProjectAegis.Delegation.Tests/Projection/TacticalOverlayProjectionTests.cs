using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

public sealed class TacticalOverlayProjectionTests
{
    [Test]
    public void ProjectSelectedUnitEnvelopes_null_or_blank_returns_empty()
    {
        Assert.That(TacticalOverlayProjection.ProjectSelectedUnitEnvelopes(null, 40, 20), Is.Empty);
        Assert.That(TacticalOverlayProjection.ProjectSelectedUnitEnvelopes("", 40, 20), Is.Empty);
        Assert.That(TacticalOverlayProjection.ProjectSelectedUnitEnvelopes("   ", 40, 20), Is.Empty);
    }

    [Test]
    public void ProjectSelectedUnitEnvelopes_returns_sensor_and_weapon_rings()
    {
        var rings = TacticalOverlayProjection.ProjectSelectedUnitEnvelopes("u1", 40.5, 18.25, "Surface");

        Assert.That(rings, Has.Count.EqualTo(2));
        Assert.That(rings[0].UnitId, Is.EqualTo("u1"));
        Assert.That(rings[0].RingKind, Is.EqualTo(TacticalOverlayProjection.RingKindSensor));
        Assert.That(rings[0].Domain, Is.EqualTo("Surface"));
        Assert.That(rings[0].RangeNm, Is.EqualTo(40.5));
        Assert.That(rings[0].IsSelectedUnit, Is.True);

        Assert.That(rings[1].RingKind, Is.EqualTo(TacticalOverlayProjection.RingKindWeapon));
        Assert.That(rings[1].RangeNm, Is.EqualTo(18.25));
        Assert.That(rings[1].IsSelectedUnit, Is.True);
        Assert.That(rings[1].UnitId, Is.EqualTo("u1"));
    }

    [Test]
    public void ProjectSelectedUnitEnvelopes_default_domain_is_unknown()
    {
        var rings = TacticalOverlayProjection.ProjectSelectedUnitEnvelopes("alpha", 10, 5);
        Assert.That(rings.All(r => r.Domain == TacticalOverlayProjection.DomainUnknown), Is.True);
    }

    [Test]
    public void ProjectSelectedUnitEnvelopes_blank_domain_falls_back_to_unknown()
    {
        var rings = TacticalOverlayProjection.ProjectSelectedUnitEnvelopes("alpha", 1, 1, "  ");
        Assert.That(rings[0].Domain, Is.EqualTo(TacticalOverlayProjection.DomainUnknown));
    }

    [Test]
    public void ProjectSelectedUnitEnvelopes_is_pure_and_stable()
    {
        var a = TacticalOverlayProjection.ProjectSelectedUnitEnvelopes("u1", 12, 8, "Air");
        var b = TacticalOverlayProjection.ProjectSelectedUnitEnvelopes("u1", 12, 8, "Air");
        Assert.That(b, Is.EqualTo(a));
    }
}
