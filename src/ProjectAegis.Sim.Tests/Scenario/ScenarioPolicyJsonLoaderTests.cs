using ProjectAegis.Sim.Policy;
using ProjectAegis.Data.Scenario.Policy;
using ProjectAegis.Sim.Scenario;
using NUnit.Framework;

namespace ProjectAegis.Sim.Tests.Scenario;

public sealed class ScenarioPolicyJsonLoaderTests
{
    [Test]
    public void Loads_restricted_engagement_with_unit_override()
    {
        var repoRoot = FindRepoRoot();
        Assert.That(repoRoot, Is.Not.Null);
        var path = Path.Combine(repoRoot!, "data", "scenarios", "restricted-engagement.policy.json");
        var profile = ScenarioPolicyJsonLoader.LoadFromFile(path);
        Assert.That(profile.Id, Is.EqualTo("restricted-engagement"));
        Assert.That(profile.ResolveForUnit("o1", isFriendly: false).Roe, Is.EqualTo(RoeLevel.WeaponsTight));
        Assert.That(profile.ResolveForUnit("f1", isFriendly: true).Roe, Is.EqualTo(RoeLevel.WeaponsFree));
        Assert.That(profile.PlayerInfoModel, Is.EqualTo(PlayerInfoModel.FullTransparency));
        Assert.That(profile.PersonalityEditPolicy, Is.EqualTo(PersonalityEditPolicy.Anytime));
    }

    [Test]
    public void ToProfile_applies_loop_policy_defaults_when_omitted()
    {
        var profile = ScenarioPolicyJsonLoader.ToProfile(new ScenarioPolicyJsonDto
        {
            Id = "test",
            FriendlyRoe = "WeaponsFree",
            OpposingRoe = "WeaponsFree",
        });

        Assert.That(profile.PlayerInfoModel, Is.EqualTo(PlayerInfoModel.FullTransparency));
        Assert.That(profile.PersonalityEditPolicy, Is.EqualTo(PersonalityEditPolicy.Anytime));
    }

    [Test]
    public void Loads_speculative_black_project_policy()
    {
        var repoRoot = FindRepoRoot();
        Assert.That(repoRoot, Is.Not.Null);
        var path = Path.Combine(repoRoot!, "data", "scenarios", "baltic-patrol-black-project.policy.json");
        var profile = ScenarioPolicyJsonLoader.LoadFromFile(path);
        Assert.That(profile.Speculative.BlackProjectMode, Is.True);
        Assert.That(profile.EngageDefaults!.WeaponTechnologyLevel, Is.EqualTo(5));
        Assert.That(profile.EngageDefaults.WeaponRequiresBlackProject, Is.True);
    }

    [Test]
    public void Loads_baltic_patrol_engage_defaults_from_json()
    {
        var repoRoot = FindRepoRoot();
        Assert.That(repoRoot, Is.Not.Null);
        var path = Path.Combine(repoRoot!, "data", "scenarios", "baltic-patrol.policy.json");
        var profile = ScenarioPolicyJsonLoader.LoadFromFile(path);
        Assert.That(profile.EngageDefaults, Is.Not.Null);
        Assert.That(profile.EngageDefaults!.RangeMeters, Is.EqualTo(45_000));
        Assert.That(profile.EngageDefaults.DefaultMagazineRounds, Is.EqualTo(4));
        var ctx = profile.ResolveEngageContext();
        Assert.That(ctx.RangeMeters, Is.EqualTo(45_000));
        Assert.That(ctx.Envelope.Contains(45_000), Is.True);
        AssertEx.Single(profile.DetectionTrials);
        Assert.That(profile.DetectionTrials[0].TargetId, Is.EqualTo("hostile-1"));
        Assert.That(profile.DetectionTrials[0].SensorId, Is.EqualTo("radar-1"));
        Assert.That(profile.DetectionTrials[0].BasePd, Is.EqualTo(1.0));
    }

    [Test]
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

        Assert.That(profile.PlayerInfoModel, Is.EqualTo(PlayerInfoModel.DelegationFog));
        Assert.That(profile.PersonalityEditPolicy, Is.EqualTo(PersonalityEditPolicy.PlanningOnly));
    }

    [Test]
    public void ToProfile_defaults_allowDualSideControl_to_false()
    {
        var profile = ScenarioPolicyJsonLoader.ToProfile(new ScenarioPolicyJsonDto
        {
            Id = "test",
            FriendlyRoe = "WeaponsFree",
            OpposingRoe = "WeaponsFree",
        });

        Assert.That(profile.AllowDualSideControl, Is.False);
    }

    [Test]
    public void ToProfile_parses_allowDualSideControl()
    {
        var profile = ScenarioPolicyJsonLoader.ToProfile(new ScenarioPolicyJsonDto
        {
            Id = "sandbox",
            FriendlyRoe = "WeaponsFree",
            OpposingRoe = "WeaponsFree",
            AllowDualSideControl = true,
        });

        Assert.That(profile.AllowDualSideControl, Is.True);
    }

    [Test]
    public void Loads_mission_roe_and_wra_cap_fixtures()
    {
        var repoRoot = FindRepoRoot();
        Assert.That(repoRoot, Is.Not.Null);
        var missionPath = Path.Combine(repoRoot!, "data", "scenarios", "baltic-patrol-mission-roe.policy.json");
        var missionProfile = ScenarioPolicyJsonLoader.LoadFromFile(missionPath);
        var missionUnit = missionProfile.ResolveUnitPolicy("u1", isFriendly: true);
        Assert.That(missionUnit.Effective.Roe, Is.EqualTo(RoeLevel.WeaponsTight));
        Assert.That(missionUnit.HasInheritedDoctrineFromMission, Is.True);

        var wraPath = Path.Combine(repoRoot!, "data", "scenarios", "baltic-patrol-wra-cap.policy.json");
        var wraProfile = ScenarioPolicyJsonLoader.LoadFromFile(wraPath);
        Assert.That(wraProfile.FriendlyDefault.MaxSalvo, Is.EqualTo(1));
        Assert.That(wraProfile.EngageDefaults!.SalvoSize, Is.EqualTo(2));
    }

    [Test]
    public void Loads_combat_domains_smoke_with_flag_enabled()
    {
        var repoRoot = FindRepoRoot();
        Assert.That(repoRoot, Is.Not.Null);
        var path = Path.Combine(repoRoot!, "data", "scenarios", "combat-domains-smoke.policy.json");
        var profile = ScenarioPolicyJsonLoader.LoadFromFile(path);
        Assert.That(profile.Id, Is.EqualTo("combat-domains-smoke"));
        Assert.That(profile.EngageDefaults, Is.Not.Null);
        Assert.That(profile.EngageDefaults!.CombatDomainsEnabled, Is.True);
    }

    [Test]
    public void Loads_baltic_patrol_combat_domains_with_flag_enabled()
    {
        var repoRoot = FindRepoRoot();
        Assert.That(repoRoot, Is.Not.Null);
        var path = Path.Combine(repoRoot!, "data", "scenarios", "baltic-patrol-combat-domains.policy.json");
        var profile = ScenarioPolicyJsonLoader.LoadFromFile(path);
        Assert.That(profile.Id, Is.EqualTo("baltic-patrol-combat-domains"));
        Assert.That(profile.EngageDefaults, Is.Not.Null);
        Assert.That(profile.EngageDefaults!.CombatDomainsEnabled, Is.True);
        Assert.That(profile.DetectionTrials[0].EnvMask, Is.EqualTo(1.0));
    }

    [Test]
    public void Loads_production_baltic_patrol_with_combat_domains_enabled()
    {
        var repoRoot = FindRepoRoot();
        Assert.That(repoRoot, Is.Not.Null);
        var path = Path.Combine(repoRoot!, "data", "scenarios", "baltic-patrol.policy.json");
        var profile = ScenarioPolicyJsonLoader.LoadFromFile(path);
        Assert.That(profile.Id, Is.EqualTo("baltic-patrol"));
        Assert.That(profile.EngageDefaults, Is.Not.Null);
        Assert.That(profile.EngageDefaults!.CombatDomainsEnabled, Is.True);
    }

    [Test]
    public void Loads_baltic_patrol_datalink_doctrine_with_side_sharing_enabled()
    {
        var repoRoot = FindRepoRoot();
        Assert.That(repoRoot, Is.Not.Null);
        var path = Path.Combine(repoRoot!, "data", "scenarios", "baltic-patrol-datalink.policy.json");
        var profile = ScenarioPolicyJsonLoader.LoadFromFile(path);
        Assert.That(profile.Id, Is.EqualTo("baltic-patrol-datalink"));
        Assert.That(profile.DatalinkDoctrine.OrganicOnly, Is.False);
        Assert.That(profile.DatalinkDoctrine.IsSharingEnabled, Is.True);
        Assert.That(profile.DatalinkDoctrine.ShareLagTicks, Is.EqualTo(0));
        Assert.That(profile.DatalinkDoctrine.ShareLagTicksSpecified, Is.False);
        Assert.That(profile.DatalinkDoctrine.ResolveSide("u1"), Is.EqualTo("blue"));
        Assert.That(profile.DatalinkDoctrine.ResolveSide("u2"), Is.EqualTo("blue"));
    }

    [Test]
    public void Loads_baltic_patrol_datalink_lag_fixture_with_shareLagTicks()
    {
        var repoRoot = FindRepoRoot();
        Assert.That(repoRoot, Is.Not.Null);
        var path = Path.Combine(repoRoot!, "data", "scenarios", "baltic-patrol-datalink-lag.policy.json");
        var profile = ScenarioPolicyJsonLoader.LoadFromFile(path);
        Assert.That(profile.Id, Is.EqualTo("baltic-patrol-datalink-lag"));
        Assert.That(profile.DatalinkDoctrine.ShareLagTicks, Is.EqualTo(2));
        Assert.That(profile.DatalinkDoctrine.ShareLagTicksSpecified, Is.True);
        Assert.That(profile.DatalinkDoctrine.IsSharingEnabled, Is.True);
    }

    [Test]
    public void Loads_baltic_patrol_datalink_comms_fixture_with_sharing_and_comms_transitions()
    {
        var repoRoot = FindRepoRoot();
        Assert.That(repoRoot, Is.Not.Null);
        var path = Path.Combine(repoRoot!, "data", "scenarios", "baltic-patrol-datalink-comms.policy.json");
        var profile = ScenarioPolicyJsonLoader.LoadFromFile(path);
        Assert.That(profile.Id, Is.EqualTo("baltic-patrol-datalink-comms"));
        Assert.That(profile.DatalinkDoctrine.OrganicOnly, Is.False);
        Assert.That(profile.DatalinkDoctrine.IsSharingEnabled, Is.True);
        Assert.That(profile.CommsTransitions.Count, Is.EqualTo(2));
        Assert.That(profile.CommsTransitions[0].AtTick, Is.EqualTo(2UL));
        Assert.That(profile.CommsTransitions[0].NewState, Is.EqualTo("Degraded"));
        Assert.That(profile.CommsTransitions[1].AtTick, Is.EqualTo(4UL));
        Assert.That(profile.CommsTransitions[1].NewState, Is.EqualTo("Denied"));
    }

    [Test]
    public void Loads_baltic_patrol_mine_transit_hazard_fixture()
    {
        var repoRoot = FindRepoRoot();
        Assert.That(repoRoot, Is.Not.Null);
        var path = Path.Combine(repoRoot!, "data", "scenarios", "baltic-patrol-mine-transit-hazard.policy.json");
        var profile = ScenarioPolicyJsonLoader.LoadFromFile(path);
        Assert.That(profile.Id, Is.EqualTo("baltic-patrol-mine-transit-hazard"));
        Assert.That(profile.MineHazard, Is.Not.Null);
        Assert.That(profile.MineHazard!.Mines.Count, Is.EqualTo(2));
        AssertEx.Single(profile.MineHazard.Transit);
        Assert.That(profile.EngageDefaults!.CombatDomainsEnabled, Is.True);
    }

    [Test]
    public void Loads_baltic_patrol_bda_lifecycle_fixture()
    {
        var repoRoot = FindRepoRoot();
        Assert.That(repoRoot, Is.Not.Null);
        var path = Path.Combine(repoRoot!, "data", "scenarios", "baltic-patrol-bda-lifecycle.policy.json");
        var profile = ScenarioPolicyJsonLoader.LoadFromFile(path);
        Assert.That(profile.Id, Is.EqualTo("baltic-patrol-bda-lifecycle"));
        Assert.That(profile.EngageDefaults!.CombatDomainsEnabled, Is.True);
        Assert.That(profile.EngageDefaults.PkKill, Is.EqualTo(0.0));
        AssertEx.Single(profile.DetectionTrials);
        Assert.That(profile.DetectionTrials[0].TargetId, Is.EqualTo("hostile-1"));
        AssertEx.Single(profile.CatalogWithdrawTargets);
        Assert.That(profile.CatalogWithdrawTargets[0].PlatformId, Is.EqualTo("hostile-1"));
        Assert.That(
            Math.Round(profile.CatalogWithdrawTargets[0].CurrentHpPct, 6, MidpointRounding.AwayFromZero),
            Is.EqualTo(Math.Round(30.0, 6, MidpointRounding.AwayFromZero)));
    }

    [Test]
    public void Loads_baltic_patrol_comms_logistics_and_display_settings()
    {
        var repoRoot = FindRepoRoot();
        Assert.That(repoRoot, Is.Not.Null);
        var path = Path.Combine(repoRoot!, "data", "scenarios", "baltic-patrol-comms.policy.json");
        var profile = ScenarioPolicyJsonLoader.LoadFromFile(path);
        Assert.That(profile.Logistics.JokerSimSeconds, Is.EqualTo(90));
        Assert.That(profile.Logistics.BingoSimSeconds, Is.EqualTo(180));
        Assert.That(profile.Logistics.UsesFuelBurnModel, Is.True);
        Assert.That(profile.Logistics.FuelCapacityKg, Is.EqualTo(10_000));
        Assert.That(profile.Logistics.BurnRateKgPerSecond, Is.EqualTo(80));
        Assert.That(profile.CommsDisplay.DegradedLagTicks, Is.EqualTo(2));
        Assert.That(
            Math.Round(profile.CommsDisplay.GhostOffsetX, 3, MidpointRounding.AwayFromZero),
            Is.EqualTo(Math.Round(0.06f, 3, MidpointRounding.AwayFromZero)));
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
