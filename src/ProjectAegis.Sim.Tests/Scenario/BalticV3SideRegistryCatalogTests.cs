using ProjectAegis.Sim.Scenario;
using ProjectAegis.Sim.Sensors;
using NUnit.Framework;

namespace ProjectAegis.Sim.Tests.Scenario;

/// <summary>
/// Catalog gauntlet ORBAT: blue/red platformIds must resolve side correctly so
/// ResolveEngageVictim and HostileContactFilter do not produce red-on-red.
/// </summary>
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public sealed class BalticV3SideRegistryCatalogTests : IDisposable
{
    public BalticV3SideRegistryCatalogTests()
    {
        BalticV3SideRegistry.ClearScenarioSides();
    }

    public void Dispose()
    {
        BalticV3SideRegistry.ClearScenarioSides();
    }

    [Test]
    public void Catalog_blue_and_red_platform_ids_resolve_side_after_register()
    {
        BalticV3SideRegistry.RegisterScenarioSide("k-31-visby-2009", "blue");
        BalticV3SideRegistry.RegisterScenarioSide("em-sovremenny-i-pr-956-sarych", "red");

        Assert.That(BalticV3SideRegistry.IsBlueForceUnit("k-31-visby-2009"), Is.True);
        Assert.That(BalticV3SideRegistry.IsRedForceUnit("k-31-visby-2009"), Is.False);
        Assert.That(BalticV3SideRegistry.IsRedForceUnit("em-sovremenny-i-pr-956-sarych"), Is.True);
        Assert.That(BalticV3SideRegistry.IsBlueForceUnit("em-sovremenny-i-pr-956-sarych"), Is.False);
        Assert.That(BalticV3SideRegistry.GetDefaultBlueUnitId(), Is.EqualTo("k-31-visby-2009"));
    }

    [Test]
    public void HostileContactFilter_catalog_red_engageable_catalog_blue_not()
    {
        BalticV3SideRegistry.RegisterScenarioSide("k-31-visby-2009", "blue");
        BalticV3SideRegistry.RegisterScenarioSide("em-sovremenny-i-pr-956-sarych", "red");

        Assert.That(HostileContactFilter.IsEngageableHostileTarget("em-sovremenny-i-pr-956-sarych"), Is.True);
        Assert.That(HostileContactFilter.IsEngageableHostileTarget("hostile-1"), Is.True);
        Assert.That(HostileContactFilter.IsEngageableHostileTarget("k-31-visby-2009"), Is.False);
        Assert.That(HostileContactFilter.IsEngageableHostileTarget("u1"), Is.False);
    }

    [Test]
    public void Legacy_synthetic_sides_still_work_without_scenario_register()
    {
        Assert.That(BalticV3SideRegistry.IsBlueForceUnit("u1"), Is.True);
        Assert.That(BalticV3SideRegistry.IsRedForceUnit("hostile-1"), Is.True);
        Assert.That(HostileContactFilter.IsEngageableHostileTarget("hostile-far"), Is.True);
    }
}
