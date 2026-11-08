using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

[TestFixture]
public sealed class PlatformCatalogListProjectionTests
{
    [Test]
    public void FormatRow_includes_damage_workbook_columns()
    {
        var row = new CatalogPlatformBrowseRow(
            "u1",
            LatDeg: 57,
            LonDeg: 20,
            CombatRadiusNm: 400,
            MaxHp: 100,
            Resilience: 1,
            WithdrawThresholdPct: 25,
            CriticalFlags: 0,
            MaxSpeedKnots: 32,
            MountCount: 1,
            SensorCount: 2);

        var line = PlatformCatalogListProjection.FormatRow(row);

        Assert.That(line, Does.Contain("u1"));
        Assert.That(line, Does.Contain("hp=100"));
        Assert.That(line, Does.Contain("res=1"));
        Assert.That(line, Does.Contain("withdraw=25"));
        Assert.That(line, Does.Contain("flags=0"));
        Assert.That(line, Does.Contain("speed=32"));
    }

    [Test]
    public void FormatRow_missing_damage_fields_use_placeholders()
    {
        var row = new CatalogPlatformBrowseRow(
            "solo-ship",
            LatDeg: 1,
            LonDeg: 2,
            CombatRadiusNm: 10,
            MaxHp: null,
            Resilience: null,
            WithdrawThresholdPct: null,
            CriticalFlags: null,
            MaxSpeedKnots: null);

        var line = PlatformCatalogListProjection.FormatRow(row);

        Assert.That(line, Does.Contain("hp=—"));
        Assert.That(line, Does.Contain("res=—"));
        Assert.That(line, Does.Contain("withdraw=—"));
        Assert.That(line, Does.Contain("flags=—"));
    }
}