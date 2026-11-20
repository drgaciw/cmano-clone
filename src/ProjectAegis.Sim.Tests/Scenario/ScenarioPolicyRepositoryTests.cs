using ProjectAegis.Sim.Policy;
using ProjectAegis.Sim.Scenario;
using NUnit.Framework;

namespace ProjectAegis.Sim.Tests.Scenario;

public sealed class ScenarioPolicyRepositoryTests
{
    [Test]
    public void Json_restricted_engagement_overrides_built_in_f1_roe()
    {
        ScenarioPolicyRepository.EnsureDefaultJsonLoaded();
        var profile = ScenarioPolicyRepository.TryGet("restricted-engagement");
        Assert.That(profile, Is.Not.Null);
        Assert.That(profile!.ResolveForUnit("f1", isFriendly: true).Roe, Is.EqualTo(RoeLevel.WeaponsFree));
    }

    [Test]
    public void Built_in_still_available_when_no_json_file()
    {
        ScenarioPolicyRepository.EnsureDefaultJsonLoaded();
        var profile = ScenarioPolicyRepository.TryGet("baltic-patrol");
        Assert.That(profile, Is.Not.Null);
        Assert.That(profile!.ResolveForUnit("any", isFriendly: true).Roe, Is.EqualTo(RoeLevel.WeaponsFree));
    }
}
