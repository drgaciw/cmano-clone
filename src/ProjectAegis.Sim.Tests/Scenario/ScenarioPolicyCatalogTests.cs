using ProjectAegis.Sim.Policy;
using ProjectAegis.Sim.Scenario;
using NUnit.Framework;

namespace ProjectAegis.Sim.Tests.Scenario;

public sealed class ScenarioPolicyCatalogTests
{
    [Test]
    public void Baltic_patrol_opp_hold_fire_resolves_opposing_roe()
    {
        var profile = ScenarioPolicyCatalog.TryGet("baltic-patrol-opp-hold-fire");
        Assert.That(profile, Is.Not.Null);
        Assert.That(profile!.ResolveForUnit("f1", isFriendly: true).Roe, Is.EqualTo(RoeLevel.WeaponsFree));
        Assert.That(profile.ResolveForUnit("o1", isFriendly: false).Roe, Is.EqualTo(RoeLevel.HoldFire));
    }
}
