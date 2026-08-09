using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

public sealed class MapScaleAndCycleTests
{
    [Test]
    public void Scale_and_altitude_labels()
    {
        var s = MapScaleProjection.Project(cameraAltitudeMeters: 867_550, metersPerScreenUnit: 1852.0);
        Assert.That(s.ScaleNauticalMiles, Is.EqualTo(1.0).Within(0.001));
        Assert.That(s.ScaleBarLabel, Does.Contain("1.00 NM"));
        Assert.That(s.CameraAltitudeLabel, Does.Contain("867550"));
    }

    [Test]
    public void Measure_range_bearing_north()
    {
        var m = MapMeasureProjection.Measure(0, 0, 0, 1, metersPerUnit: 1852.0);
        Assert.That(m.RangeNauticalMiles, Is.EqualTo(1.0).Within(0.001));
        Assert.That(m.BearingDegrees, Is.EqualTo(0.0).Within(0.1));
        Assert.That(m.Label, Does.Contain("RNG"));
    }

    [Test]
    public void Unit_cycle_wraps()
    {
        var ids = new[] { "a", "b", "c" };
        Assert.That(UnitCycleProjection.Next(ids, "a"), Is.EqualTo("b"));
        Assert.That(UnitCycleProjection.Next(ids, "c"), Is.EqualTo("a"));
        Assert.That(UnitCycleProjection.Previous(ids, "a"), Is.EqualTo("c"));
        Assert.That(UnitCycleProjection.Next(ids, null), Is.EqualTo("a"));
    }
}
