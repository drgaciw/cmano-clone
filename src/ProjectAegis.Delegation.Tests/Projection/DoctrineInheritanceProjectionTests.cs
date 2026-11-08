using System.Globalization;
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
        Assert.That(entry.EffectiveRoeLabel, Is.EqualTo("ROE: WeaponsTight"));
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
        Assert.That(entry.EffectiveRoeLabel, Is.EqualTo("ROE: HoldFire"));
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
        Assert.That(entry.EffectiveRoeLabel, Is.EqualTo("ROE: —"));
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
        Assert.That(entry.EffectiveRoeLabel, Is.EqualTo("ROE: WeaponsFree"));
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
        Assert.That(entry.EffectiveEmconLabel, Is.EqualTo("EMCON: OFF"));
    }

    [Test]
    public void ProjectFieldProvenance_side_default_matches_resolver_without_parent()
    {
        var policy = new ScenarioPolicyProfile(
            new EffectivePolicy(RoeLevel.WeaponsFree, MaxSalvo: 8, AutoEngageAuthorized: true, ExpendAuthorized: false),
            opposingDefault: new EffectivePolicy(RoeLevel.HoldFire, MaxSalvo: 5, AutoEngageAuthorized: false, ExpendAuthorized: true),
            missionRoe: new EffectivePolicy(RoeLevel.WeaponsTight, MaxSalvo: 2),
            missionUnitIds: ["other"]);

        AssertResolvedFields(policy, "u1", isFriendly: true, DoctrineFieldSourceLevel.FriendlyDefault, null);
        AssertResolvedFields(policy, "u1", isFriendly: false, DoctrineFieldSourceLevel.OpposingDefault, null);
    }

    [Test]
    public void ProjectFieldProvenance_mission_override_matches_resolver_and_exposes_side_parent()
    {
        var friendly = new EffectivePolicy(RoeLevel.WeaponsFree, MaxSalvo: 8);
        var opposing = new EffectivePolicy(RoeLevel.HoldFire, MaxSalvo: 5, AutoEngageAuthorized: false, ExpendAuthorized: true);
        var mission = new EffectivePolicy(RoeLevel.WeaponsTight, MaxSalvo: 2, AutoEngageAuthorized: false, ExpendAuthorized: true);
        var policy = new ScenarioPolicyProfile(
            friendly,
            opposingDefault: opposing,
            missionRoe: mission,
            missionUnitIds: ["Alpha"]);

        AssertResolvedFields(policy, "alpha", isFriendly: true, DoctrineFieldSourceLevel.Mission, friendly);
        AssertResolvedFields(policy, "alpha", isFriendly: false, DoctrineFieldSourceLevel.Mission, opposing);
    }

    [Test]
    public void ProjectFieldProvenance_unit_override_matches_resolver_and_exposes_mission_parent()
    {
        var friendly = new EffectivePolicy(RoeLevel.WeaponsFree, MaxSalvo: 8, AutoEngageAuthorized: true, ExpendAuthorized: false);
        var mission = new EffectivePolicy(RoeLevel.WeaponsTight, MaxSalvo: 2, AutoEngageAuthorized: false, ExpendAuthorized: true);
        var unit = new EffectivePolicy(RoeLevel.HoldFire, MaxSalvo: 1, AutoEngageAuthorized: true, ExpendAuthorized: false);
        var policy = new ScenarioPolicyProfile(
            friendly,
            unitOverrides: new Dictionary<string, EffectivePolicy> { ["u1"] = unit },
            missionRoe: mission,
            missionUnitIds: ["u1"]);

        var model = AssertResolvedFields(policy, "u1", isFriendly: true, DoctrineFieldSourceLevel.UnitOverride, mission);
        var roe = model.Field(DoctrineInheritanceFields.Roe);
        Assert.That(roe.IsOverridden, Is.True);
        Assert.That(roe.EffectiveValue, Is.EqualTo(nameof(RoeLevel.HoldFire)));
        Assert.That(roe.OverriddenParentValue, Is.EqualTo(nameof(RoeLevel.WeaponsTight)));
        Assert.That(model.Field(DoctrineInheritanceFields.WraMaxSalvo).OverriddenParentValue, Is.EqualTo("2"));
        Assert.That(model.Field(DoctrineInheritanceFields.AutoEngageAuthorized).OverriddenParentValue, Is.EqualTo(false.ToString()));
        Assert.That(model.Field(DoctrineInheritanceFields.ExpendAuthorized).OverriddenParentValue, Is.EqualTo(true.ToString()));
    }

    [Test]
    public void ProjectFieldProvenance_unit_override_without_mission_exposes_side_parent()
    {
        var friendly = new EffectivePolicy(RoeLevel.WeaponsFree, MaxSalvo: 8);
        var opposing = new EffectivePolicy(RoeLevel.WeaponsTight, MaxSalvo: 4, ExpendAuthorized: true);
        var unit = new EffectivePolicy(RoeLevel.HoldFire, MaxSalvo: 1);
        var policy = new ScenarioPolicyProfile(
            friendly,
            opposingDefault: opposing,
            unitOverrides: new Dictionary<string, EffectivePolicy> { ["u1"] = unit },
            missionRoe: new EffectivePolicy(RoeLevel.WeaponsTight, MaxSalvo: 2),
            missionUnitIds: ["someone-else"]);

        AssertResolvedFields(policy, "u1", isFriendly: true, DoctrineFieldSourceLevel.UnitOverride, friendly);
        AssertResolvedFields(policy, "u1", isFriendly: false, DoctrineFieldSourceLevel.UnitOverride, opposing);
    }

    [Test]
    public void ProjectFieldProvenance_override_key_stays_case_sensitive_like_resolver()
    {
        var mission = new EffectivePolicy(RoeLevel.WeaponsTight, MaxSalvo: 2);
        var policy = new ScenarioPolicyProfile(
            EffectivePolicy.DefaultFree,
            unitOverrides: new Dictionary<string, EffectivePolicy>
            {
                ["U1"] = new(RoeLevel.HoldFire, MaxSalvo: 1),
            },
            missionRoe: mission,
            missionUnitIds: ["u1"]);

        AssertResolvedFields(policy, "u1", isFriendly: true, DoctrineFieldSourceLevel.Mission, policy.FriendlyDefault);
    }

    [Test]
    public void ProjectFieldProvenance_group_level_is_unsupported_and_unmodeled_fields_are_explicit()
    {
        var policy = new ScenarioPolicyProfile(
            EffectivePolicy.DefaultFree,
            unitRadarEmcon: new Dictionary<string, EmconState> { ["u1"] = EmconState.Passive });

        var model = DoctrineInheritanceProjection.ProjectFieldProvenance(new TargetId("u1"), policy, isFriendly: true);
        var entry = DoctrineInheritanceProjection.ProjectUnit(new TargetId("u1"), policy, isFriendly: true);

        Assert.That(entry.EffectiveEmconLabel, Is.EqualTo("EMCON: PASSIVE"));
        Assert.That(model.Fields.Any(field => field.FieldId == DoctrineInheritanceLevels.Group), Is.False);
        Assert.That(model.Fields.Any(field => field.SourceLevel == DoctrineFieldSourceLevel.Unsupported), Is.False);

        var group = model.Levels.Single(level => level.Level == DoctrineInheritanceLevels.Group);
        Assert.That(group.Availability, Is.EqualTo(DoctrineLevelAvailability.Unsupported));
        Assert.That(
            model.Levels.Where(level => level.Level != DoctrineInheritanceLevels.Group).Select(level => level.Availability),
            Is.All.EqualTo(DoctrineLevelAvailability.Modeled));
        Assert.That(
            model.Levels.Select(level => level.Level),
            Is.EqualTo(new[]
            {
                DoctrineInheritanceLevels.UnitOverride,
                DoctrineInheritanceLevels.Mission,
                DoctrineInheritanceLevels.FriendlyDefault,
                DoctrineInheritanceLevels.OpposingDefault,
                DoctrineInheritanceLevels.Group,
            }));

        foreach (var fieldId in DoctrineInheritanceFields.NotModeled)
        {
            var field = model.Field(fieldId);
            Assert.That(field.EffectiveValue, Is.EqualTo(nameof(DoctrineFieldSourceLevel.NotModeled)));
            Assert.That(field.SourceLevel, Is.EqualTo(DoctrineFieldSourceLevel.NotModeled));
            Assert.That(field.IsOverridden, Is.False);
            Assert.That(field.OverriddenParentValue, Is.Null);
        }
    }

    [Test]
    public void ProjectFieldProvenance_without_policy_does_not_invent_a_source()
    {
        var model = DoctrineInheritanceProjection.ProjectFieldProvenance(new TargetId("u1"), policy: null, isFriendly: true);

        Assert.That(model.UnitId, Is.EqualTo("u1"));
        foreach (var field in model.Fields)
        {
            Assert.That(field.EffectiveValue, Is.EqualTo("—"));
            Assert.That(field.SourceLevel, Is.EqualTo(DoctrineFieldSourceLevel.NotModeled));
            Assert.That(field.IsOverridden, Is.False);
            Assert.That(field.OverriddenParentValue, Is.Null);
        }

        Assert.That(
            model.Levels.Single(level => level.Level == DoctrineInheritanceLevels.Group).Availability,
            Is.EqualTo(DoctrineLevelAvailability.Unsupported));
    }

    [Test]
    public void ProjectFieldProvenance_selected_units_follow_resolver_in_ordinal_order()
    {
        var mission = new EffectivePolicy(RoeLevel.WeaponsTight, MaxSalvo: 2, ExpendAuthorized: true);
        var policy = new ScenarioPolicyProfile(
            EffectivePolicy.DefaultFree,
            unitOverrides: new Dictionary<string, EffectivePolicy>
            {
                ["u2"] = new(RoeLevel.HoldFire, MaxSalvo: 1),
            },
            missionRoe: mission,
            missionUnitIds: ["u1"]);

        var models = DoctrineInheritanceProjection.ProjectFieldProvenance(
            [new TargetId("u2"), new TargetId("u10"), new TargetId("u1")],
            policy,
            isFriendly: true);

        Assert.That(models.Select(model => model.UnitId), Is.EqualTo(new[] { "u1", "u10", "u2" }));
        AssertResolvedFields(policy, "u1", isFriendly: true, DoctrineFieldSourceLevel.Mission, policy.FriendlyDefault);
        AssertResolvedFields(policy, "u10", isFriendly: true, DoctrineFieldSourceLevel.FriendlyDefault, null);
        AssertResolvedFields(policy, "u2", isFriendly: true, DoctrineFieldSourceLevel.UnitOverride, policy.FriendlyDefault);
    }

    [Test]
    public void Source_fence_projection_has_no_unity_session_or_bridge_refs()
    {
        var root = RepoRoot();
        var paths = new[]
        {
            Path.Combine(root, "src", "ProjectAegis.Delegation", "Projection", "DoctrineInheritanceProjection.cs"),
            Path.Combine(root, "src", "ProjectAegis.Delegation", "Projection", "DoctrineInheritanceFieldProvenance.cs"),
        };

        var forbidden = new[]
        {
            "UnityEngine",
            "MonoBehaviour",
            "ApplyOrder",
            "DecisionLog",
            "SimulationSession",
            "DelegationBridge",
        };

        foreach (var path in paths)
        {
            Assert.That(File.Exists(path), Is.True, path);
            var source = File.ReadAllText(path);
            foreach (var token in forbidden)
            {
                Assert.That(source, Does.Not.Contain(token), $"{path} contains {token}");
            }
        }

        Assert.That(File.ReadAllText(paths[0]), Does.Contain("ResolveUnitPolicy"));
    }

    private static DoctrineInheritanceReadModel AssertResolvedFields(
        ScenarioPolicyProfile policy,
        string unitId,
        bool isFriendly,
        DoctrineFieldSourceLevel expectedSource,
        EffectivePolicy? expectedParent)
    {
        var resolved = policy.ResolveUnitPolicy(unitId, isFriendly);
        var model = DoctrineInheritanceProjection.ProjectFieldProvenance(new TargetId(unitId), policy, isFriendly);
        Assert.That(model.UnitId, Is.EqualTo(unitId));

        foreach (var fieldId in DoctrineInheritanceFields.Resolved)
        {
            var field = model.Field(fieldId);
            Assert.That(field.UnitId, Is.EqualTo(unitId));
            Assert.That(field.EffectiveValue, Is.EqualTo(FormatExpected(resolved.Effective, fieldId)));
            Assert.That(field.SourceLevel, Is.EqualTo(expectedSource));
            if (expectedParent == null)
            {
                Assert.That(field.IsOverridden, Is.False);
                Assert.That(field.OverriddenParentValue, Is.Null);
            }
            else
            {
                Assert.That(field.IsOverridden, Is.True);
                Assert.That(field.OverriddenParentValue, Is.EqualTo(FormatExpected(expectedParent.Value, fieldId)));
            }
        }

        return model;
    }

    private static string FormatExpected(EffectivePolicy policy, string fieldId)
    {
        if (fieldId == DoctrineInheritanceFields.Roe)
        {
            return policy.Roe.ToString();
        }

        if (fieldId == DoctrineInheritanceFields.WraMaxSalvo)
        {
            return policy.MaxSalvo.ToString(CultureInfo.InvariantCulture);
        }

        if (fieldId == DoctrineInheritanceFields.AutoEngageAuthorized)
        {
            return policy.AutoEngageAuthorized.ToString();
        }

        if (fieldId == DoctrineInheritanceFields.ExpendAuthorized)
        {
            return policy.ExpendAuthorized.ToString();
        }

        throw new ArgumentOutOfRangeException(nameof(fieldId), fieldId, "Not a resolver field.");
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "ProjectAegis.sln")))
        {
            dir = dir.Parent;
        }

        Assert.That(dir, Is.Not.Null, "repo root not found");
        return dir!.FullName;
    }
}