using ProjectAegis.Sim.Policy;
using ProjectAegis.Sim.Scenario;
using Xunit;

namespace ProjectAegis.Sim.Tests.Scenario;

public sealed class ScenarioPolicyRepositoryTests
{
    [Fact]
    public void Json_restricted_engagement_overrides_built_in_f1_roe()
    {
        ScenarioPolicyRepository.EnsureDefaultJsonLoaded();
        var profile = ScenarioPolicyRepository.TryGet("restricted-engagement");
        Assert.NotNull(profile);
        Assert.Equal(RoeLevel.WeaponsFree, profile!.ResolveForUnit("f1", isFriendly: true).Roe);
    }

    [Fact]
    public void Built_in_still_available_when_no_json_file()
    {
        ScenarioPolicyRepository.EnsureDefaultJsonLoaded();
        var profile = ScenarioPolicyRepository.TryGet("baltic-patrol");
        Assert.NotNull(profile);
        Assert.Equal(RoeLevel.WeaponsFree, profile!.ResolveForUnit("any", isFriendly: true).Roe);
    }
}
