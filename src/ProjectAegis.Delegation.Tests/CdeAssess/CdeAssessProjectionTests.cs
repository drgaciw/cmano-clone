using ProjectAegis.Data.Catalog;
using ProjectAegis.Delegation.CdeAssess;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Sim.Glossary;
using ProjectAegis.Sim.Policy;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.CdeAssess;

public sealed class CdeAssessProjectionTests
{
    [Test]
    public void Low_risk_feasible_emits_advisory_row_with_assumptions_geometry_and_policy_fields()
    {
        var input = new CdeAssessInput(
            ShooterId: "u1",
            TargetId: "hostile-1",
            WeaponFamilyId: CatalogWeaponIds.MvpDefault,
            SimTick: 1,
            SimTime: 1.0,
            CorrelationId: 42,
            Preview: new EngagePreview("DLZ: In", CanFire: true, AbortPreviewCode: null),
            RangeClassLabel: "medium");

        var snapshot = CdeAssessProjection.Project(input, log: null);

        Assert.That(snapshot.Rows, Has.Count.EqualTo(1));
        var row = snapshot.Rows[0];
        Assert.That(row.RiskKind, Is.EqualTo(CdeAssessRiskKind.Low));
        Assert.That(row.ShooterId, Is.EqualTo("u1"));
        Assert.That(row.TargetId, Is.EqualTo("hostile-1"));
        Assert.That(row.CorrelationId, Is.EqualTo(42UL));
        Assert.That(row.SimTick, Is.EqualTo(1UL));
        Assert.That(row.SimTime, Is.EqualTo(1.0));
        Assert.That(row.WithholdReason, Is.Null);
        Assert.That(row.Assumptions, Is.Not.Empty);
        Assert.That(row.Assumptions, Does.Contain(CdeAssessProjection.AssumptionPreviewInRange));
        Assert.That(row.Assumptions, Does.Contain(CdeAssessProjection.AssumptionNoCdeWithhold));
        Assert.That(row.Assumptions, Does.Contain(CdeAssessProjection.AssumptionNoPolicyDenial));
        Assert.That(row.GeometryRangeClass, Does.Contain("range:medium"));
        Assert.That(row.GeometryRangeClass, Does.Contain("DLZ: In"));
        Assert.That(row.PolicyConstraintText, Is.EqualTo(CdeAssessProjection.PolicyConstraintClear));
    }

    [Test]
    public void Low_risk_output_is_advisory_never_authorizes()
    {
        var input = new CdeAssessInput(
            ShooterId: "u1",
            TargetId: "hostile-1",
            WeaponFamilyId: CatalogWeaponIds.MvpDefault,
            SimTick: 1,
            SimTime: 1.0,
            CorrelationId: 7,
            Preview: new EngagePreview("DLZ: In", CanFire: true, AbortPreviewCode: null));

        var snapshot = CdeAssessProjection.Project(input, log: null);
        var row = snapshot.Rows[0];

        Assert.That(row.RiskKind, Is.EqualTo(CdeAssessRiskKind.Low));
        Assert.That(GetPropertyNames(typeof(CdeAssessRow)), Does.Not.Contain("Authorized"));
        Assert.That(GetPropertyNames(typeof(CdeAssessRow)), Does.Not.Contain("CanFire"));
        Assert.That(GetPropertyNames(typeof(CdeAssessSnapshot)), Does.Not.Contain("Authorized"));
    }

    [Test]
    public void Withheld_by_explicit_cde_collateral_emits_withheld_with_reason()
    {
        var input = new CdeAssessInput(
            ShooterId: "u1",
            TargetId: "hostile-1",
            WeaponFamilyId: CatalogWeaponIds.MvpDefault,
            SimTick: 2,
            SimTime: 2.0,
            CorrelationId: 11,
            Preview: new EngagePreview("DLZ: In", CanFire: true, AbortPreviewCode: null),
            CollateralWithheld: true,
            CollateralWithholdReason: "CDE_NEAR_CIVILIAN");

        var snapshot = CdeAssessProjection.Project(input, log: null);

        Assert.That(snapshot.Rows, Has.Count.EqualTo(1));
        var row = snapshot.Rows[0];
        Assert.That(row.RiskKind, Is.EqualTo(CdeAssessRiskKind.Withheld));
        Assert.That(row.WithholdReason, Is.EqualTo("CDE_NEAR_CIVILIAN"));
        Assert.That(row.PolicyConstraintText, Does.Contain("CDE_NEAR_CIVILIAN"));
        Assert.That(row.Assumptions, Does.Contain(CdeAssessProjection.AssumptionCdeWithhold));
        Assert.That(GetPropertyNames(typeof(CdeAssessRow)), Does.Not.Contain("Authorized"));
    }

    [Test]
    public void Withheld_by_policy_denial_emits_withheld_with_reason_for_shooter_scoped_log_row()
    {
        var input = new CdeAssessInput(
            ShooterId: "u1",
            TargetId: "hostile-1",
            WeaponFamilyId: CatalogWeaponIds.MvpDefault,
            SimTick: 1,
            SimTime: 1.0,
            CorrelationId: 9,
            Preview: new EngagePreview("DLZ: In", CanFire: true, AbortPreviewCode: null));

        var log = new DecisionLog();
        log.AppendPolicyDenial(new PolicyDenialRecord(
            1, 1.2, 2,
            new AgentId("a1"),
            new TargetId("u1"),
            0,
            FireAbortReason.RoeHoldFire,
            OrderKind.Engage));

        var snapshot = CdeAssessProjection.Project(input, log);

        Assert.That(snapshot.Rows, Has.Count.EqualTo(1));
        var row = snapshot.Rows[0];
        Assert.That(row.RiskKind, Is.EqualTo(CdeAssessRiskKind.Withheld));
        Assert.That(row.WithholdReason, Is.EqualTo(nameof(FireAbortReason.RoeHoldFire)));
        Assert.That(row.PolicyConstraintText, Does.Contain(nameof(FireAbortReason.RoeHoldFire)));
        Assert.That(row.Assumptions, Does.Contain(CdeAssessProjection.AssumptionPolicyDenial));
        Assert.That(GetPropertyNames(typeof(CdeAssessRow)), Does.Not.Contain("CanFire"));
    }

    [Test]
    public void Cde_withhold_takes_precedence_over_clear_preview_and_policy()
    {
        var input = new CdeAssessInput(
            ShooterId: "u1",
            TargetId: "hostile-1",
            WeaponFamilyId: CatalogWeaponIds.MvpDefault,
            SimTick: 5,
            SimTime: 5.0,
            CorrelationId: 20,
            Preview: new EngagePreview("DLZ: In", CanFire: true, AbortPreviewCode: null),
            CollateralWithheld: true,
            CollateralWithholdReason: "CDE_BUFFER_ZONE");

        var log = new DecisionLog();
        log.AppendPolicyDenial(new PolicyDenialRecord(
            1, 5.0, 5,
            new AgentId("a1"),
            new TargetId("u1"),
            0,
            FireAbortReason.WeaponsTight,
            OrderKind.Engage));

        var snapshot = CdeAssessProjection.Project(input, log);

        var row = snapshot.Rows[0];
        Assert.That(row.RiskKind, Is.EqualTo(CdeAssessRiskKind.Withheld));
        Assert.That(row.WithholdReason, Is.EqualTo("CDE_BUFFER_ZONE"));
    }

    [Test]
    public void Victim_scoped_policy_denial_does_not_withhold_cde_assess()
    {
        var input = new CdeAssessInput(
            ShooterId: "u1",
            TargetId: "hostile-2",
            WeaponFamilyId: CatalogWeaponIds.MvpDefault,
            SimTick: 10,
            SimTime: 10.0,
            CorrelationId: 20,
            Preview: new EngagePreview("DLZ: In", CanFire: true, AbortPreviewCode: null));

        var log = new DecisionLog();
        log.AppendPolicyDenial(new PolicyDenialRecord(
            1, 9.0, 9,
            new AgentId("a1"),
            new TargetId("hostile-1"),
            0,
            FireAbortReason.RoeHoldFire,
            OrderKind.Engage));

        var snapshot = CdeAssessProjection.Project(input, log);

        Assert.That(snapshot.Rows[0].RiskKind, Is.EqualTo(CdeAssessRiskKind.Low));
    }

    [Test]
    public void Stale_shooter_scoped_policy_denial_does_not_apply_to_later_attempt()
    {
        var log = new DecisionLog();
        log.AppendPolicyDenial(new PolicyDenialRecord(
            1, 1.0, 1,
            new AgentId("a1"),
            new TargetId("u1"),
            0,
            FireAbortReason.RoeHoldFire,
            OrderKind.Engage));

        var input = new CdeAssessInput(
            ShooterId: "u1",
            TargetId: "hostile-1",
            WeaponFamilyId: CatalogWeaponIds.MvpDefault,
            SimTick: 5,
            SimTime: 5.0,
            CorrelationId: 30,
            Preview: new EngagePreview("DLZ: In", CanFire: true, AbortPreviewCode: null));

        var snapshot = CdeAssessProjection.Project(input, log);

        Assert.That(snapshot.Rows[0].RiskKind, Is.EqualTo(CdeAssessRiskKind.Low));
    }

    [Test]
    public void Preview_blocked_without_policy_or_cde_emits_elevated_risk()
    {
        var input = new CdeAssessInput(
            ShooterId: "u1",
            TargetId: "hostile-1",
            WeaponFamilyId: CatalogWeaponIds.MvpDefault,
            SimTick: 1,
            SimTime: 1.0,
            CorrelationId: 7,
            Preview: new EngagePreview(
                "DLZ: Out",
                CanFire: false,
                AbortPreviewCode: AbortReasonCatalog.Engage.DLZ_OUT));

        var snapshot = CdeAssessProjection.Project(input, log: null);

        var row = snapshot.Rows[0];
        Assert.That(row.RiskKind, Is.EqualTo(CdeAssessRiskKind.Elevated));
        Assert.That(row.WithholdReason, Is.Null);
        Assert.That(row.GeometryRangeClass, Does.Contain(AbortReasonCatalog.Engage.DLZ_OUT));
        Assert.That(row.Assumptions, Does.Contain(CdeAssessProjection.AssumptionPreviewOutOfRange));
    }

    [Test]
    public void Snapshot_row_list_is_immutable_after_construction()
    {
        var rows = new List<CdeAssessRow>
        {
            new(
                "u1",
                "hostile-1",
                CdeAssessRiskKind.Low,
                new[] { CdeAssessProjection.AssumptionNoCdeWithhold },
                "DLZ: In",
                CdeAssessProjection.PolicyConstraintClear,
                1,
                1.0,
                1,
                null),
        };

        var snapshot = new CdeAssessSnapshot(rows);
        rows.Add(new CdeAssessRow(
            "u1",
            "hostile-2",
            CdeAssessRiskKind.Withheld,
            new[] { CdeAssessProjection.AssumptionCdeWithhold },
            "DLZ: Out",
            "CDE/collateral withhold: test",
            2,
            2.0,
            2,
            "CDE_TEST"));

        Assert.That(snapshot.Rows, Has.Count.EqualTo(1));
        Assert.That(snapshot.Rows[0].TargetId, Is.EqualTo("hostile-1"));
    }

    [Test]
    public void Fingerprint_is_identical_for_identical_inputs()
    {
        var input = new CdeAssessInput(
            "u1",
            "hostile-1",
            CatalogWeaponIds.MvpDefault,
            SimTick: 4,
            SimTime: 4.5,
            CorrelationId: 100,
            Preview: new EngagePreview("DLZ: In", true, null),
            RangeClassLabel: "short");

        var first = CdeAssessProjection.Project(input, log: null);
        var second = CdeAssessProjection.Project(input, log: null);

        Assert.That(
            CdeAssessFingerprint.Compute(first),
            Is.EqualTo(CdeAssessFingerprint.Compute(second)));
    }

    [Test]
    public void Dto_surface_omits_ui_derived_truth_fields()
    {
        var uiDerivedNames = new[]
        {
            "Selection",
            "Hover",
            "Camera",
            "Panel",
            "Visible",
            "Chrome",
            "IsSelected",
            "Authorized",
            "CanFire",
        };

        foreach (var type in new[]
                 {
                     typeof(CdeAssessRow),
                     typeof(CdeAssessInput),
                     typeof(CdeAssessSnapshot),
                 })
        {
            foreach (var prop in type.GetProperties())
            {
                foreach (var forbidden in uiDerivedNames)
                {
                    Assert.That(
                        prop.Name.Contains(forbidden, StringComparison.OrdinalIgnoreCase),
                        Is.False,
                        $"{type.Name}.{prop.Name} must not encode UI-derived truth or authorization");
                }
            }
        }
    }

    private static IEnumerable<string> GetPropertyNames(Type type) =>
        type.GetProperties().Select(p => p.Name);
}
