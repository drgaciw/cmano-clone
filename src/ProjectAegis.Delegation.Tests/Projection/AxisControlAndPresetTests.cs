using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

public sealed class AxisControlAndPresetTests
{
    [Test]
    public void Default_axes_are_automatic()
    {
        var s = AxisControlProjection.ProjectDefault("u1");
        Assert.That(s.AltitudeAxis, Is.EqualTo(AxisMode.Automatic));
        Assert.That(AxisControlProjection.IsMixed(s), Is.False);
        Assert.That(AxisControlProjection.FormatBadge(s), Does.Contain("AUTO"));
    }

    [Test]
    public void Toggle_produces_mixed()
    {
        var s = AxisControlProjection.ProjectDefault("u1");
        s = AxisControlProjection.Toggle(s, AxisKind.Altitude);
        Assert.That(s.AltitudeAxis, Is.EqualTo(AxisMode.Manual));
        Assert.That(AxisControlProjection.IsMixed(s), Is.True);
        Assert.That(AxisControlProjection.FormatBadge(s), Does.Contain("MAN"));
    }

    [Test]
    public void Subsurface_altitude_presets_include_layer()
    {
        var panel = DomainPresetProjection.Project("ssn-1", PlatformDomain.Subsurface);
        Assert.That(panel.AltitudePresets.Any(p => p.Id == "under_layer"), Is.True);
        Assert.That(panel.ThrottlePresets.Any(p => p.Id == "creep"), Is.True);
    }

    [Test]
    public void Air_throttle_has_flank()
    {
        var panel = DomainPresetProjection.Project("f18", PlatformDomain.Air);
        Assert.That(panel.ThrottlePresets.Any(p => p.Id == "flank"), Is.True);
        Assert.That(panel.AltitudePresets.Any(p => p.Id == "high"), Is.True);
    }
}
