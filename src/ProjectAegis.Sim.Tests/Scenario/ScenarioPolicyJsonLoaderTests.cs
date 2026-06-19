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
    public void Loads_production_baltic_patrol_with_combat_domains_enabled()
    {
        var repoRoot = FindRepoRoot();
        Assert.NotNull(repoRoot);
        var path = Path.Combine(repoRoot!, "data", "scenarios", "baltic-patrol.policy.json");
        var profile = ScenarioPolicyJsonLoader.LoadFromFile(path);
        Assert.Equal("baltic-patrol", profile.Id);
        Assert.NotNull(profile.EngageDefaults);
        Assert.True(profile.EngageDefaults!.CombatDomainsEnabled);
    }

    [Fact]
    public void Loads_baltic_patrol_datalink_doctrine_with_side_sharing_enabled()
    {
        var repoRoot = FindRepoRoot();
        Assert.NotNull(repoRoot);
        var path = Path.Combine(repoRoot!, "data", "scenarios", "baltic-patrol-datalink.policy.json");
        var profile = ScenarioPolicyJsonLoader.LoadFromFile(path);
        Assert.Equal("baltic-patrol-datalink", profile.Id);
        Assert.False(profile.DatalinkDoctrine.OrganicOnly);
        Assert.True(profile.DatalinkDoctrine.IsSharingEnabled);
        Assert.Equal(0, profile.DatalinkDoctrine.ShareLagTicks);
        Assert.False(profile.DatalinkDoctrine.ShareLagTicksSpecified);
        Assert.Equal("blue", profile.DatalinkDoctrine.ResolveSide("u1"));
        Assert.Equal("blue", profile.DatalinkDoctrine.ResolveSide("u2"));
    }

    [Fact]
    public void Loads_baltic_patrol_datalink_lag_fixture_with_shareLagTicks()
    {
        var repoRoot = FindRepoRoot();
        Assert.NotNull(repoRoot);
        var path = Path.Combine(repoRoot!, "data", "scenarios", "baltic-patrol-datalink-lag.policy.json");
        var profile = ScenarioPolicyJsonLoader.LoadFromFile(path);
        Assert.Equal("baltic-patrol-datalink-lag", profile.Id);
        Assert.Equal(2, profile.DatalinkDoctrine.ShareLagTicks);
        Assert.True(profile.DatalinkDoctrine.ShareLagTicksSpecified);
        Assert.True(profile.DatalinkDoctrine.IsSharingEnabled);
    }

    [Fact]
    public void Loads_baltic_patrol_datalink_comms_fixture_with_sharing_and_comms_transitions()
    {
        var repoRoot = FindRepoRoot();
        Assert.NotNull(repoRoot);
        var path = Path.Combine(repoRoot!, "data", "scenarios", "baltic-patrol-datalink-comms.policy.json");
        var profile = ScenarioPolicyJsonLoader.LoadFromFile(path);
        Assert.Equal("baltic-patrol-datalink-comms", profile.Id);
        Assert.False(profile.DatalinkDoctrine.OrganicOnly);
        Assert.True(profile.DatalinkDoctrine.IsSharingEnabled);
        Assert.Equal(2, profile.CommsTransitions.Count);
        Assert.Equal(2UL, profile.CommsTransitions[0].AtTick);
        Assert.Equal("Degraded", profile.CommsTransitions[0].NewState);
        Assert.Equal(4UL, profile.CommsTransitions[1].AtTick);
        Assert.Equal("Denied", profile.CommsTransitions[1].NewState);
    }

    [Fact]
    public void Loads_baltic_patrol_mine_transit_hazard_fixture()
    {
        var repoRoot = FindRepoRoot();
        Assert.NotNull(repoRoot);
        var path = Path.Combine(repoRoot!, "data", "scenarios", "baltic-patrol-mine-transit-hazard.policy.json");
        var profile = ScenarioPolicyJsonLoader.LoadFromFile(path);
        Assert.Equal("baltic-patrol-mine-transit-hazard", profile.Id);
        Assert.NotNull(profile.MineHazard);
        Assert.Equal(2, profile.MineHazard!.Mines.Count);
        Assert.Single(profile.MineHazard.Transit);
        Assert.True(profile.EngageDefaults!.CombatDomainsEnabled);
    }

    [Fact]
    public void Loads_baltic_patrol_bda_lifecycle_fixture()
    {
        var repoRoot = FindRepoRoot();
        Assert.NotNull(repoRoot);
        var path = Path.Combine(repoRoot!, "data", "scenarios", "baltic-patrol-bda-lifecycle.policy.json");
        var profile = ScenarioPolicyJsonLoader.LoadFromFile(path);
        Assert.Equal("baltic-patrol-bda-lifecycle", profile.Id);
        Assert.True(profile.EngageDefaults!.CombatDomainsEnabled);
        Assert.Equal(0.0, profile.EngageDefaults.PkKill);
        Assert.Single(profile.DetectionTrials);
        Assert.Equal("hostile-1", profile.DetectionTrials[0].TargetId);
        Assert.Single(profile.CatalogWithdrawTargets);
        Assert.Equal("hostile-1", profile.CatalogWithdrawTargets[0].PlatformId);
        Assert.Equal(30.0, profile.CatalogWithdrawTargets[0].CurrentHpPct, precision: 6);
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
