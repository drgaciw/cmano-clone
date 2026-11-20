using ProjectAegis.Sim.Policy;
using ProjectAegis.Data.Scenario.Policy;
using ProjectAegis.Sim.Scenario;
using NUnit.Framework;

namespace ProjectAegis.Sim.Tests.Scenario;

public sealed class ScenarioPolicyInheritanceTests
{
    [Test]
    public void Mission_roe_overrides_side_default_for_assigned_units()
    {
        var profile = new ScenarioPolicyProfile(
            EffectivePolicy.DefaultFree,
            opposingDefault: new EffectivePolicy(RoeLevel.WeaponsTight),
            missionRoe: new EffectivePolicy(RoeLevel.WeaponsTight),
            missionUnitIds: ["u1"]);

        var resolved = profile.ResolveUnitPolicy("u1", isFriendly: true);
        Assert.That(resolved.Effective.Roe, Is.EqualTo(RoeLevel.WeaponsTight));
        Assert.That(resolved.HasInheritedDoctrineFromMission, Is.True);

        var other = profile.ResolveUnitPolicy("u2", isFriendly: true);
        Assert.That(other.Effective.Roe, Is.EqualTo(RoeLevel.WeaponsFree));
        Assert.That(other.HasInheritedDoctrineFromMission, Is.False);
    }

    [Test]
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
        Assert.That(resolved.Effective.Roe, Is.EqualTo(RoeLevel.HoldFire));
        Assert.That(resolved.HasInheritedDoctrineFromMission, Is.False);
    }

    [Test]
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
        Assert.That(profile.FriendlyDefault.MaxSalvo, Is.EqualTo(3));

        var missionUnit = profile.ResolveUnitPolicy("f1", isFriendly: true);
        Assert.That(missionUnit.Effective.Roe, Is.EqualTo(RoeLevel.WeaponsTight));
        Assert.That(missionUnit.Effective.MaxSalvo, Is.EqualTo(2));
        Assert.That(missionUnit.HasInheritedDoctrineFromMission, Is.True);
    }
}