using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Sim.Policy;
using ProjectAegis.Sim.Scenario;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

[TestFixture]
public sealed class DoctrineInheritanceProjectionTests
{
    [Test]
    public void ProjectUnit_mission_assigned_unit_inherits_mission_roe_and_source()
    {
        var policy = new ScenarioPolicyProfile(
            EffectivePolicy.DefaultFree,
            missionRoe: new EffectivePolicy(RoeLevel.WeaponsTight, MaxSalvo: 2),
            missionUnitIds: ["u1"]);
        var entry = DoctrineInheritanceProjection.ProjectUnit(new TargetId("u1"), policy, isFriendly: true);
        Assert.That(entry, Is.Not.Null);
        Assert.That(entry!.EffectiveRoeLabel, Is.EqualTo("ROE: WeaponsTight"));
        Assert.That(entry.EffectiveMaxSalvoLabel, Is.EqualTo("SALVO: 2"));
        Assert.That(entry.EffectiveEmconLabel, Is.EqualTo("EMCON: ACTIVE"));
        Assert.That(entry.InheritanceSource, Is.EqualTo("SOURCE: Mission"));
        Assert.That(entry.IsInheritedFromMission, Is.True);
        Assert.That(entry.HasLocalOverride, Is.False);
        Assert.That(entry.OverrideButtonLabel, Is.EqualTo("OVERRIDE: NONE"));
    }

    [Test]
    public void ProjectUnit_unit_override_wins_over_mission_roe()
    {
        var policy = new ScenarioPolicyProfile(
            EffectivePolicy.DefaultFree,
            unitOverrides: new Dictionary<string, EffectivePolicy> { ["u1"] = new(RoeLevel.HoldFire, MaxSalvo: 1) },
            missionRoe: new EffectivePolicy(RoeLevel.WeaponsTight),
            missionUnitIds: ["u1"]);
        var entry = DoctrineInheritanceProjection.ProjectUnit(new TargetId("u1"), policy, isFriendly: true);
        Assert.That(entry, Is.Not.Null);
        Assert.That(entry!.EffectiveRoeLabel, Is.EqualTo("ROE: HoldFire"));
        Assert.That(entry.EffectiveMaxSalvoLabel, Is.EqualTo("SALVO: 1"));
        Assert.That(entry.EffectiveEmconLabel, Is.EqualTo("EMCON: ACTIVE"));
        Assert.That(entry.InheritanceSource, Is.EqualTo("SOURCE: Unit Override"));
        Assert.That(entry.IsInheritedFromMission, Is.False);
        Assert.That(entry.HasLocalOverride, Is.True);
        Assert.That(entry.OverrideButtonLabel, Is.EqualTo("OVERRIDE: ACTIVE"));
    }

    [Test]
    public void ProjectUnit_without_policy_returns_unavailable_placeholder()
    {
        var entry = DoctrineInheritanceProjection.ProjectUnit(new TargetId("u1"), policy: null, isFriendly: true);
        Assert.That(entry, Is.Not.Null);
        Assert.That(entry!.EffectiveRoeLabel, Is.EqualTo("ROE: —"));
        Assert.That(entry.EffectiveEmconLabel, Is.EqualTo("EMCON: —"));
        Assert.That(entry.InheritanceSource, Is.EqualTo("SOURCE: —"));
        Assert.That(entry.OverrideButtonLabel, Is.EqualTo("OVERRIDE: UNAVAILABLE"));
    }

    [Test]
    public void ProjectUnit_scenario_default_for_non_mission_unit()
    {
        var policy = new ScenarioPolicyProfile(
            EffectivePolicy.DefaultFree,
            missionRoe: new EffectivePolicy(RoeLevel.WeaponsTight),
            missionUnitIds: ["u1"]);
        var entry = DoctrineInheritanceProjection.ProjectUnit(new TargetId("u2"), policy, isFriendly: true);
        Assert.That(entry, Is.Not.Null);
        Assert.That(entry!.EffectiveRoeLabel, Is.EqualTo("ROE: WeaponsFree"));
        Assert.That(entry.InheritanceSource, Is.EqualTo("SOURCE: Scenario Default"));
        Assert.That(entry.IsInheritedFromMission, Is.False);
    }

    [Test]
    public void ProjectUnit_scenario_emcon_off_maps_to_read_only_label()
    {
        var policy = new ScenarioPolicyProfile(
            EffectivePolicy.DefaultFree,
            unitRadarEmcon: new Dictionary<string, EmconState> { ["u1"] = EmconState.Off });
        var entry = DoctrineInheritanceProjection.ProjectUnit(new TargetId("u1"), policy, isFriendly: true);
        Assert.That(entry, Is.Not.Null);
        Assert.That(entry!.EffectiveEmconLabel, Is.EqualTo("EMCON: OFF"));
    }
}