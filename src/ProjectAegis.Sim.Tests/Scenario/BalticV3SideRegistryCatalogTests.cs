using ProjectAegis.Sim.Scenario;
using ProjectAegis.Sim.Sensors;
using Xunit;

namespace ProjectAegis.Sim.Tests.Scenario;

/// <summary>
/// Catalog gauntlet ORBAT: blue/red platformIds must resolve side correctly so
/// ResolveEngageVictim and HostileContactFilter do not produce red-on-red.
/// </summary>
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

    [Fact]
    public void Catalog_blue_and_red_platform_ids_resolve_side_after_register()
    {
        BalticV3SideRegistry.RegisterScenarioSide("k-31-visby-2009", "blue");
        BalticV3SideRegistry.RegisterScenarioSide("em-sovremenny-i-pr-956-sarych", "red");

        Assert.True(BalticV3SideRegistry.IsBlueForceUnit("k-31-visby-2009"));
        Assert.False(BalticV3SideRegistry.IsRedForceUnit("k-31-visby-2009"));
        Assert.True(BalticV3SideRegistry.IsRedForceUnit("em-sovremenny-i-pr-956-sarych"));
        Assert.False(BalticV3SideRegistry.IsBlueForceUnit("em-sovremenny-i-pr-956-sarych"));
        Assert.Equal("k-31-visby-2009", BalticV3SideRegistry.GetDefaultBlueUnitId());
    }

    [Fact]
    public void HostileContactFilter_catalog_red_engageable_catalog_blue_not()
    {
        BalticV3SideRegistry.RegisterScenarioSide("k-31-visby-2009", "blue");
        BalticV3SideRegistry.RegisterScenarioSide("em-sovremenny-i-pr-956-sarych", "red");

        Assert.True(HostileContactFilter.IsEngageableHostileTarget("em-sovremenny-i-pr-956-sarych"));
        Assert.True(HostileContactFilter.IsEngageableHostileTarget("hostile-1"));
        Assert.False(HostileContactFilter.IsEngageableHostileTarget("k-31-visby-2009"));
        Assert.False(HostileContactFilter.IsEngageableHostileTarget("u1"));
    }

    [Fact]
    public void Legacy_synthetic_sides_still_work_without_scenario_register()
    {
        Assert.True(BalticV3SideRegistry.IsBlueForceUnit("u1"));
        Assert.True(BalticV3SideRegistry.IsRedForceUnit("hostile-1"));
        Assert.True(HostileContactFilter.IsEngageableHostileTarget("hostile-far"));
    }
}
