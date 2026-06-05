using ProjectAegis.Delegation.Projection;
using ProjectAegis.Sim.Scenario;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

public sealed class FuelStateProjectionTests
{
    [Test]
    public void FormatUnitFuelLine_uses_default_thresholds()
    {
        Assert.That(FuelStateProjection.FormatUnitFuelLine("u1", 100), Does.Contain("NOMINAL"));
        Assert.That(FuelStateProjection.FormatUnitFuelLine("u1", 400), Does.Contain("JOKER"));
        Assert.That(FuelStateProjection.FormatUnitFuelLine("u1", 700), Does.Contain("BINGO"));
    }

    [Test]
    public void FormatUnitFuelLine_uses_scenario_logistics_thresholds()
    {
        var logistics = new ScenarioLogisticsSettings(50, 100);
        Assert.That(FuelStateProjection.FormatUnitFuelLine("u1", 40, logistics), Does.Contain("NOMINAL"));
        Assert.That(FuelStateProjection.FormatUnitFuelLine("u1", 60, logistics), Does.Contain("JOKER"));
        Assert.That(FuelStateProjection.FormatUnitFuelLine("u1", 110, logistics), Does.Contain("BINGO"));
    }

    [Test]
    public void FormatUnitFuelLine_uses_burn_model_when_configured()
    {
        var logistics = new ScenarioLogisticsSettings(300, 600, fuelCapacityKg: 10_000, burnRateKgPerSecond: 80);
        Assert.That(FuelStateProjection.FormatUnitFuelLine("u1", 10, logistics), Does.Contain("NOMINAL").And.Contains("9200kg"));
        Assert.That(FuelStateProjection.FormatUnitFuelLine("u1", 100, logistics), Does.Contain("JOKER"));
        Assert.That(FuelStateProjection.FormatUnitFuelLine("u1", 120, logistics), Does.Contain("BINGO"));
    }
}