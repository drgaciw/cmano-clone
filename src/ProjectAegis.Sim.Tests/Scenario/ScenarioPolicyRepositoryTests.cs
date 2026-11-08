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
        Assert.Equal(RoeLevel.WeaponsFree, profile.ResolveForUnit("f1", isFriendly: true).Roe);
    }

    [Fact]
    public void EnsureLoadedFrom_unity_assets_path_binds_classify_side_roe()
    {
        var assets = FindUnityProjectAssets();
        Assert.True(ScenarioPolicyPlayModeLoad.EnsureLoadedFrom(assets));
        var profile = ScenarioPolicyRepository.TryGet("baltic-patrol-classify");
        Assert.NotNull(profile);
        Assert.Equal(RoeLevel.WeaponsFree, profile.FriendlyDefault.Roe);
        Assert.Equal(RoeLevel.WeaponsTight, profile.OpposingDefault.Roe);
    }

    private static string FindUnityProjectAssets()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        for (var i = 0; i < 12 && dir != null; i++)
        {
            var candidate = Path.Combine(dir.FullName, "unity", "ProjectAegis", "Assets");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("unity/ProjectAegis/Assets was not found above the test base directory.");
    }

    [Fact]
    public void Built_in_still_available_when_no_json_file()
    {
        ScenarioPolicyRepository.EnsureDefaultJsonLoaded();
        var profile = ScenarioPolicyRepository.TryGet("baltic-patrol");
        Assert.NotNull(profile);
        Assert.Equal(RoeLevel.WeaponsFree, profile.ResolveForUnit("any", isFriendly: true).Roe);
    }
}
