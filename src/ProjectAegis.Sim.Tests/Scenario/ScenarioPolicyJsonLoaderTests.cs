using ProjectAegis.Sim.Policy;
using ProjectAegis.Data.Scenario.Policy;
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
    public void Loads_speculative_black_project_policy()
    {
        var repoRoot = FindRepoRoot();
        Assert.NotNull(repoRoot);
        var path = Path.Combine(repoRoot!, "data", "scenarios", "baltic-patrol-black-project.policy.json");
        var profile = ScenarioPolicyJsonLoader.LoadFromFile(path);
        Assert.True(profile.Speculative.BlackProjectMode);
        Assert.Equal(5, profile.EngageDefaults!.WeaponTechnologyLevel);
        Assert.True(profile.EngageDefaults.WeaponRequiresBlackProject);
    }

    [Fact]
    public void Loads_baltic_patrol_engage_defaults_from_json()
    {
        var repoRoot = FindRepoRoot();
        Assert.NotNull(repoRoot);
        var path = Path.Combine(repoRoot!, "data", "scenarios", "baltic-patrol.policy.json");
        var profile = ScenarioPolicyJsonLoader.LoadFromFile(path);
        Assert.NotNull(profile.EngageDefaults);
        Assert.Equal(45_000, profile.EngageDefaults!.RangeMeters);
        Assert.Equal(4, profile.EngageDefaults.DefaultMagazineRounds);
        var ctx = profile.ResolveEngageContext();
        Assert.Equal(45_000, ctx.RangeMeters);
        Assert.True(ctx.Envelope.Contains(45_000));
        Assert.Single(profile.DetectionTrials);
        Assert.Equal("hostile-1", profile.DetectionTrials[0].TargetId);
        Assert.Equal("radar-1", profile.DetectionTrials[0].SensorId);
        Assert.Equal(1.0, profile.DetectionTrials[0].BasePd);
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

    [Fact]
    public void Loads_mission_roe_and_wra_cap_fixtures()
    {
        var repoRoot = FindRepoRoot();
        Assert.NotNull(repoRoot);
        var missionPath = Path.Combine(repoRoot!, "data", "scenarios", "baltic-patrol-mission-roe.policy.json");
        var missionProfile = ScenarioPolicyJsonLoader.LoadFromFile(missionPath);
        var missionUnit = missionProfile.ResolveUnitPolicy("u1", isFriendly: true);
        Assert.Equal(RoeLevel.WeaponsTight, missionUnit.Effective.Roe);
        Assert.True(missionUnit.HasInheritedDoctrineFromMission);

        var wraPath = Path.Combine(repoRoot!, "data", "scenarios", "baltic-patrol-wra-cap.policy.json");
        var wraProfile = ScenarioPolicyJsonLoader.LoadFromFile(wraPath);
        Assert.Equal(1, wraProfile.FriendlyDefault.MaxSalvo);
        Assert.Equal(2, wraProfile.EngageDefaults!.SalvoSize);
    }

    [Fact]
    public void Loads_combat_domains_smoke_with_flag_enabled()
    {
        var repoRoot = FindRepoRoot();
        Assert.NotNull(repoRoot);
        var path = Path.Combine(repoRoot!, "data", "scenarios", "combat-domains-smoke.policy.json");
        var profile = ScenarioPolicyJsonLoader.LoadFromFile(path);
        Assert.Equal("combat-domains-smoke", profile.Id);
        Assert.NotNull(profile.EngageDefaults);
        Assert.True(profile.EngageDefaults!.CombatDomainsEnabled);
    }

    [Fact]
    public void Loads_baltic_patrol_combat_domains_with_flag_enabled()
    {
        var repoRoot = FindRepoRoot();
        Assert.NotNull(repoRoot);
        var path = Path.Combine(repoRoot!, "data", "scenarios", "baltic-patrol-combat-domains.policy.json");
        var profile = ScenarioPolicyJsonLoader.LoadFromFile(path);
        Assert.Equal("baltic-patrol-combat-domains", profile.Id);
        Assert.NotNull(profile.EngageDefaults);
        Assert.True(profile.EngageDefaults!.CombatDomainsEnabled);
        Assert.Equal(1.0, profile.DetectionTrials[0].EnvMask);
    }

    [Fact]
    public void Loads_production_baltic_patrol_with_combat_domains_disabled()
    {
        var repoRoot = FindRepoRoot();
        Assert.NotNull(repoRoot);
        var path = Path.Combine(repoRoot!, "data", "scenarios", "baltic-patrol.policy.json");
        var profile = ScenarioPolicyJsonLoader.LoadFromFile(path);
        Assert.Equal("baltic-patrol", profile.Id);
        Assert.NotNull(profile.EngageDefaults);
        Assert.False(profile.EngageDefaults!.CombatDomainsEnabled);
    }

    [Fact]
    public void Loads_baltic_patrol_comms_logistics_and_display_settings()
    {
        var repoRoot = FindRepoRoot();
        Assert.NotNull(repoRoot);
        var path = Path.Combine(repoRoot!, "data", "scenarios", "baltic-patrol-comms.policy.json");
        var profile = ScenarioPolicyJsonLoader.LoadFromFile(path);
        Assert.Equal(90, profile.Logistics.JokerSimSeconds);
        Assert.Equal(180, profile.Logistics.BingoSimSeconds);
        Assert.True(profile.Logistics.UsesFuelBurnModel);
        Assert.Equal(10_000, profile.Logistics.FuelCapacityKg);
        Assert.Equal(80, profile.Logistics.BurnRateKgPerSecond);
        Assert.Equal(2, profile.CommsDisplay.DegradedLagTicks);
        Assert.Equal(0.06f, profile.CommsDisplay.GhostOffsetX, 3);
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
