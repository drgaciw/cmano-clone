using ProjectAegis.Sim.Policy;
using ProjectAegis.Sim.Scenario;
using Xunit;

namespace ProjectAegis.Sim.Tests.Scenario;

public sealed class ScenarioPolicyJsonLoaderTests
{
    [Fact]
    public void Loads_restricted_engagement_with_unit_override()
    {
        var repoRoot = FindRepoRoot();
        Assert.NotNull(repoRoot);
        var path = Path.Combine(repoRoot!, "data", "scenarios", "restricted-engagement.policy.json");
        var profile = ScenarioPolicyJsonLoader.LoadFromFile(path);
        Assert.Equal("restricted-engagement", profile.Id);
        Assert.Equal(RoeLevel.WeaponsTight, profile.ResolveForUnit("o1", isFriendly: false).Roe);
        Assert.Equal(RoeLevel.WeaponsFree, profile.ResolveForUnit("f1", isFriendly: true).Roe);
        Assert.Equal(PlayerInfoModel.FullTransparency, profile.PlayerInfoModel);
        Assert.Equal(PersonalityEditPolicy.Anytime, profile.PersonalityEditPolicy);
    }

    [Fact]
    public void ToProfile_applies_loop_policy_defaults_when_omitted()
    {
        var profile = ScenarioPolicyJsonLoader.ToProfile(new ScenarioPolicyJsonDto
        {
            Id = "test",
            FriendlyRoe = "WeaponsFree",
            OpposingRoe = "WeaponsFree",
        });

        Assert.Equal(PlayerInfoModel.FullTransparency, profile.PlayerInfoModel);
        Assert.Equal(PersonalityEditPolicy.Anytime, profile.PersonalityEditPolicy);
    }

    [Fact]
    public void ToProfile_parses_loop_policy_overrides()
    {
        var profile = ScenarioPolicyJsonLoader.ToProfile(new ScenarioPolicyJsonDto
        {
            Id = "training-fog",
            FriendlyRoe = "WeaponsFree",
            OpposingRoe = "WeaponsFree",
            PlayerInfoModel = "delegationFog",
            PersonalityEditPolicy = "planningOnly",
        });

        Assert.Equal(PlayerInfoModel.DelegationFog, profile.PlayerInfoModel);
        Assert.Equal(PersonalityEditPolicy.PlanningOnly, profile.PersonalityEditPolicy);
    }

    [Fact]
    public void ToProfile_defaults_allowDualSideControl_to_false()
    {
        var profile = ScenarioPolicyJsonLoader.ToProfile(new ScenarioPolicyJsonDto
        {
            Id = "test",
            FriendlyRoe = "WeaponsFree",
            OpposingRoe = "WeaponsFree",
        });

        Assert.False(profile.AllowDualSideControl);
    }

    [Fact]
    public void ToProfile_parses_allowDualSideControl()
    {
        var profile = ScenarioPolicyJsonLoader.ToProfile(new ScenarioPolicyJsonDto
        {
            Id = "sandbox",
            FriendlyRoe = "WeaponsFree",
            OpposingRoe = "WeaponsFree",
            AllowDualSideControl = true,
        });

        Assert.True(profile.AllowDualSideControl);
    }

    private static string? FindRepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 8; i++)
        {
            if (File.Exists(Path.Combine(dir, "ProjectAegis.sln")))
            {
                return dir;
            }

            dir = Directory.GetParent(dir)?.FullName ?? dir;
        }

        return null;
    }
}
