using ProjectAegis.Sim.Policy;
using ProjectAegis.Sim.Scenario;
using Xunit;

namespace ProjectAegis.Sim.Tests.Scenario;

public sealed class ScenarioPolicyInheritanceTests
{
    [Fact]
    public void Mission_roe_overrides_side_default_for_assigned_units()
    {
        var profile = new ScenarioPolicyProfile(
            EffectivePolicy.DefaultFree,
            opposingDefault: new EffectivePolicy(RoeLevel.WeaponsTight),
            missionRoe: new EffectivePolicy(RoeLevel.WeaponsTight),
            missionUnitIds: ["u1"]);

        var resolved = profile.ResolveUnitPolicy("u1", isFriendly: true);
        Assert.Equal(RoeLevel.WeaponsTight, resolved.Effective.Roe);
        Assert.True(resolved.HasInheritedDoctrineFromMission);

        var other = profile.ResolveUnitPolicy("u2", isFriendly: true);
        Assert.Equal(RoeLevel.WeaponsFree, other.Effective.Roe);
        Assert.False(other.HasInheritedDoctrineFromMission);
    }

    [Fact]
    public void Unit_override_wins_over_mission_roe()
    {
        var profile = new ScenarioPolicyProfile(
            EffectivePolicy.DefaultFree,
            unitOverrides: new Dictionary<string, EffectivePolicy>
            {
                ["u1"] = new(RoeLevel.HoldFire),
            },
            missionRoe: new EffectivePolicy(RoeLevel.WeaponsTight),
            missionUnitIds: ["u1"]);

        var resolved = profile.ResolveUnitPolicy("u1", isFriendly: true);
        Assert.Equal(RoeLevel.HoldFire, resolved.Effective.Roe);
        Assert.False(resolved.HasInheritedDoctrineFromMission);
    }

    [Fact]
    public void Json_loader_parses_mission_policy_and_max_salvo()
    {
        var dto = new ScenarioPolicyJsonDto
        {
            Id = "test-mission-wra",
            FriendlyRoe = "WeaponsFree",
            OpposingRoe = "WeaponsFree",
            Engage = new ScenarioEngageJsonDto { MaxSalvo = 3 },
            MissionPolicy = new ScenarioMissionPolicyJsonDto
            {
                Roe = "WeaponsTight",
                UnitIds = ["f1"],
                MaxSalvo = 2,
            },
        };

        var profile = ScenarioPolicyJsonLoader.ToProfile(dto);
        Assert.Equal(3, profile.FriendlyDefault.MaxSalvo);

        var missionUnit = profile.ResolveUnitPolicy("f1", isFriendly: true);
        Assert.Equal(RoeLevel.WeaponsTight, missionUnit.Effective.Roe);
        Assert.Equal(2, missionUnit.Effective.MaxSalvo);
        Assert.True(missionUnit.HasInheritedDoctrineFromMission);
    }
}