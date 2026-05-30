using ProjectAegis.Sim.Policy;
using ProjectAegis.Sim.Scenario;
using Xunit;

namespace ProjectAegis.Sim.Tests.Scenario;

public sealed class ScenarioPolicyCatalogTests
{
    [Fact]
    public void Baltic_patrol_opp_hold_fire_resolves_opposing_roe()
    {
        var profile = ScenarioPolicyCatalog.TryGet("baltic-patrol-opp-hold-fire");
        Assert.NotNull(profile);
        Assert.Equal(RoeLevel.WeaponsFree, profile!.ResolveForUnit("f1", isFriendly: true).Roe);
        Assert.Equal(RoeLevel.HoldFire, profile.ResolveForUnit("o1", isFriendly: false).Roe);
    }
}
