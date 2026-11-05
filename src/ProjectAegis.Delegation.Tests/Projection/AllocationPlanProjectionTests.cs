using ProjectAegis.Delegation.EmploymentLedger;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.ThreatAssessment;
using ProjectAegis.Sim.Engage;
using ProjectAegis.Sim.Glossary;
using ProjectAegis.Sim.Policy;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

public sealed class AllocationPlanProjectionTests
{
    private const string Weapon = "asm";

    [Test]
    public void Two_by_three_plan_lists_can_fire_cells_and_totals()
    {
        var plan = AllocationPlanProjection.Project(Plan(
            shooters: new[] { "s1", "s2" },
            targets: new[] { "t1", "t2", "t3" },
            allocations: new[]
            {
                Line("s2", "t3", 1, 3),
                Line("s1", "t2", 1, 3),
                Line("s2", "t1", 1, 3),
                Line("s1", "t3", 1, 3),
                Line("s2", "t2", 1, 3),
                Line("s1", "t1", 1, 3),
            },
            magazines: new[]
            {
                new EmploymentLedgerMagazineFacts("s2", Weapon, 3, 1),
                new EmploymentLedgerMagazineFacts("s1", Weapon, 3, 1),
            }));

        Assert.That(plan.IsFireOrder, Is.False);
        Assert.That(plan.Cells, Has.Count.EqualTo(6));
        Assert.That(
            plan.Cells.Select(cell => $"{cell.ShooterId}/{cell.TargetId}:{cell.AcceptedQuantity}:{(cell.CanFire ? 1 : 0)}:{cell.MagazineRemaining}"),
            Is.EqualTo(new[]
            {
                "s1/t1:1:1:0",
                "s1/t2:1:1:0",
                "s1/t3:1:1:0",
                "s2/t1:1:1:0",
                "s2/t2:1:1:0",
                "s2/t3:1:1:0",
            }));
        Assert.That(plan.Cells.All(cell => cell.AbortReason is null && cell.ReasonCode is null), Is.True);
        Assert.That(plan.Cells[0].ReasonText, Is.EqualTo(ClearText(3)));
        Assert.That(
            plan.ShooterTotals.Select(row => $"{row.ShooterId}:{row.AcceptedQuantity}:{row.MagazineRemaining}"),
            Is.EqualTo(new[] { "s1:3:0", "s2:3:0" }));
        Assert.That(
            plan.TargetTotals.Select(row => $"{row.TargetId}:{row.AcceptedQuantity}"),
            Is.EqualTo(new[] { "t1:2", "t2:2", "t3:2" }));
    }

    [Test]
    public void Mixed_plan_keeps_can_fire_and_domain_abort_reasons()
    {
        var withheld = WeaponRecommendation.Empty with
        {
            ShooterUnitId = "s1",
            WeaponId = Weapon,
            TargetId = "t3",
            ContactId = "t3",
            Outcome = WeaponRecommendationOutcome.WithheldByPolicy,
            WithheldReasonCode = nameof(FireAbortReason.WeaponsTight),
        };
        var plan = AllocationPlanProjection.Project(Plan(
            shooters: new[] { "s1" },
            targets: new[] { "t1", "t2", "t3" },
            allocations: new[]
            {
                Line("s1", "t2", 1, 4, track: false),
                Line("s1", "t3", 1, 4, recommendation: withheld),
                Line("s1", "t1", 1, 4),
            },
            magazines: new[] { new EmploymentLedgerMagazineFacts("s1", Weapon, 4, 1) }));

        Assert.That(plan.IsFireOrder, Is.False);
        Assert.That(plan.Cells[0].TargetId, Is.EqualTo("t1"));
        Assert.That(plan.Cells[0].CanFire, Is.True);
        Assert.That(plan.Cells[0].AcceptedQuantity, Is.EqualTo(1));

        Assert.That(plan.Cells[1].TargetId, Is.EqualTo("t2"));
        Assert.That(plan.Cells[1].CanFire, Is.False);
        Assert.That(plan.Cells[1].AcceptedQuantity, Is.EqualTo(0));
        Assert.That(plan.Cells[1].AbortReason, Is.EqualTo(FireAbortReason.NoFireControlTrack));
        Assert.That(plan.Cells[1].ReasonCode, Is.EqualTo(AbortReasonCatalog.Engage.NO_FIRE_CONTROL_TRACK));
        Assert.That(
            plan.Cells[1].ReasonText,
            Is.EqualTo(EngageExplainProjection.ExplainCode(AbortReasonCatalog.Engage.NO_FIRE_CONTROL_TRACK)));

        Assert.That(plan.Cells[2].TargetId, Is.EqualTo("t3"));
        Assert.That(plan.Cells[2].CanFire, Is.False);
        Assert.That(plan.Cells[2].AbortReason, Is.EqualTo(FireAbortReason.WeaponsTight));
        Assert.That(plan.Cells[2].ReasonCode, Is.EqualTo(AbortReasonCatalog.Doctrine.ROE_WEAPONS_TIGHT));
        Assert.That(
            plan.Cells[2].ReasonText,
            Is.EqualTo(EngageExplainProjection.ExplainCode(AbortReasonCatalog.Doctrine.ROE_WEAPONS_TIGHT)));

        Assert.That(plan.Cells[0].MagazineRemaining, Is.EqualTo(3));
        Assert.That(plan.ShooterTotals[0].AcceptedQuantity, Is.EqualTo(1));
        Assert.That(plan.ShooterTotals[0].MagazineRemaining, Is.EqualTo(3));
        Assert.That(
            plan.TargetTotals.Select(row => $"{row.TargetId}:{row.AcceptedQuantity}"),
            Is.EqualTo(new[] { "t1:1", "t2:0", "t3:0" }));
    }

    [Test]
    public void Recommendation_fire_flag_does_not_authorize_or_block_a_clear_cell()
    {
        var recommendation = WeaponRecommendation.Empty with
        {
            ShooterUnitId = "s1",
            WeaponId = Weapon,
            TargetId = "t1",
            Outcome = WeaponRecommendationOutcome.Feasible,
            IsFireOrder = true,
            IsWeaponsReleaseAuthorization = true,
            IsAutomaticEngagement = true,
        };
        var plan = AllocationPlanProjection.Project(Plan(
            shooters: new[] { "s1" },
            targets: new[] { "t1" },
            allocations: new[] { Line("s1", "t1", 1, 2, recommendation: recommendation) },
            magazines: new[] { new EmploymentLedgerMagazineFacts("s1", Weapon, 2, 1) }));

        Assert.That(plan.IsFireOrder, Is.False);
        Assert.That(plan.Cells[0].CanFire, Is.True);
        Assert.That(plan.Cells[0].AcceptedQuantity, Is.EqualTo(1));
        Assert.That(plan.Cells[0].MagazineRemaining, Is.EqualTo(1));
    }

    [Test]
    public void Matching_recommendation_rejects_quantity_above_max_salvo()
    {
        var recommendation = WeaponRecommendation.Empty with
        {
            ShooterUnitId = "s1",
            WeaponId = Weapon,
            TargetId = "t1",
            ContactId = "t1",
            Outcome = WeaponRecommendationOutcome.Feasible,
            PolicyConstraints = new ThreatPolicyConstraints(
                RoeLevel.WeaponsFree,
                MaxSalvo: 2,
                AutoEngageAuthorized: true,
                ExpendAuthorized: true,
                PolicyAllowsFire: true,
                PolicyAbortCode: null),
        };
        var plan = AllocationPlanProjection.Project(Plan(
            shooters: new[] { "s1" },
            targets: new[] { "t1" },
            allocations: new[] { Line("s1", "t1", quantity: 4, rounds: 8, recommendation: recommendation) },
            magazines: new[] { new EmploymentLedgerMagazineFacts("s1", Weapon, 8, 1) }));

        Assert.That(plan.Cells, Has.Count.EqualTo(1));
        Assert.That(plan.Cells[0].CanFire, Is.False);
        Assert.That(plan.Cells[0].AcceptedQuantity, Is.EqualTo(0));
        Assert.That(plan.Cells[0].AbortReason, Is.EqualTo(FireAbortReason.WraSalvo));
        Assert.That(plan.Cells[0].ReasonCode, Is.EqualTo(AbortReasonCatalog.Doctrine.WRA_SALVO));
        Assert.That(plan.ShooterTotals[0].MagazineRemaining, Is.EqualTo(8));
    }


    [Test]
    public void Over_allocation_is_rejected_and_does_not_draw_the_magazine()
    {
        var plan = AllocationPlanProjection.Project(Plan(
            shooters: new[] { "s1" },
            targets: new[] { "t1" },
            allocations: new[] { Line("s1", "t1", 4, 2) },
            magazines: new[] { new EmploymentLedgerMagazineFacts("s1", Weapon, 2, 1) }));

        Assert.That(plan.IsFireOrder, Is.False);
        Assert.That(plan.Cells, Has.Count.EqualTo(1));
        Assert.That(plan.Cells[0].CanFire, Is.False);
        Assert.That(plan.Cells[0].AcceptedQuantity, Is.EqualTo(0));
        Assert.That(plan.Cells[0].AbortReason, Is.Null);
        Assert.That(plan.Cells[0].ReasonCode, Is.EqualTo(AbortReasonCatalog.Engage.NO_AMMO));
        Assert.That(
            plan.Cells[0].ReasonText,
            Is.EqualTo(EngageExplainProjection.ExplainCode(AbortReasonCatalog.Engage.NO_AMMO)));
        Assert.That(plan.Cells[0].MagazineRemaining, Is.EqualTo(2));
        Assert.That(plan.ShooterTotals[0].AcceptedQuantity, Is.EqualTo(0));
        Assert.That(plan.ShooterTotals[0].MagazineRemaining, Is.EqualTo(2));
    }

    [Test]
    public void Later_line_past_the_magazine_is_winchester_and_earlier_lines_stand()
    {
        var plan = AllocationPlanProjection.Project(Plan(
            shooters: new[] { "s1" },
            targets: new[] { "t1", "t2", "t3" },
            allocations: new[]
            {
                Line("s1", "t3", 1, 2),
                Line("s1", "t1", 1, 2),
                Line("s1", "t2", 1, 2),
            },
            magazines: new[] { new EmploymentLedgerMagazineFacts("s1", Weapon, 2, 1) }));

        Assert.That(
            plan.Cells.Select(cell => $"{cell.TargetId}:{cell.AcceptedQuantity}:{cell.ReasonCode ?? "ok"}"),
            Is.EqualTo(new[]
            {
                "t1:1:ok",
                "t2:1:ok",
                $"t3:0:{AbortReasonCatalog.Engage.WINCHESTER_ORDNANCE}",
            }));
        Assert.That(plan.Cells[2].ReasonText, Is.EqualTo(
            EngageExplainProjection.ExplainCode(AbortReasonCatalog.Engage.WINCHESTER_ORDNANCE)));
        Assert.That(plan.ShooterTotals[0].AcceptedQuantity, Is.EqualTo(2));
        Assert.That(plan.ShooterTotals[0].MagazineRemaining, Is.EqualTo(0));
        Assert.That(plan.IsFireOrder, Is.False);
    }

    [Test]
    public void Same_inputs_are_replay_deterministic()
    {
        var forward = Plan(
            shooters: new[] { "s1", "s2" },
            targets: new[] { "t1", "t2", "t3" },
            allocations: new[]
            {
                Line("s1", "t1", 1, 3),
                Line("s1", "t2", 1, 3),
                Line("s1", "t3", 1, 3),
                Line("s2", "t1", 1, 3),
                Line("s2", "t2", 1, 3),
                Line("s2", "t3", 1, 3),
            },
            magazines: new[]
            {
                new EmploymentLedgerMagazineFacts("s1", Weapon, 3, 1),
                new EmploymentLedgerMagazineFacts("s2", Weapon, 3, 1),
            });
        var reversed = forward with
        {
            Allocations = forward.Allocations!.Reverse().ToArray(),
            Magazines = forward.Magazines!.Reverse().ToArray(),
        };

        var first = AllocationPlanProjection.Project(forward);
        var second = AllocationPlanProjection.Project(forward);
        var shuffled = AllocationPlanProjection.Project(reversed);

        Assert.That(AllocationPlanProjection.Fingerprint(first), Is.EqualTo(AllocationPlanProjection.Fingerprint(second)));
        Assert.That(AllocationPlanProjection.Fingerprint(shuffled), Is.EqualTo(AllocationPlanProjection.Fingerprint(first)));
        Assert.That(first.IsFireOrder, Is.False);
        Assert.That(shuffled.IsFireOrder, Is.False);
    }

    [Test]
    public void Unknown_target_does_not_draw_rounds()
    {
        var plan = AllocationPlanProjection.Project(Plan(
            shooters: new[] { "s1" },
            targets: new[] { "t1" },
            allocations: new[] { Line("s1", "t-missing", 1, 3) },
            magazines: new[] { new EmploymentLedgerMagazineFacts("s1", Weapon, 3, 1) }));

        Assert.That(plan.Cells[0].CanFire, Is.False);
        Assert.That(plan.Cells[0].ReasonCode, Is.EqualTo("UNKNOWN_TARGET"));
        Assert.That(plan.Cells[0].MagazineRemaining, Is.EqualTo(3));
        Assert.That(plan.ShooterTotals[0].AcceptedQuantity, Is.EqualTo(0));
        Assert.That(plan.TargetTotals[0].AcceptedQuantity, Is.EqualTo(0));
        Assert.That(plan.IsFireOrder, Is.False);
    }

    [Test]
    public void Empty_request_is_not_a_fire_order()
    {
        var plan = AllocationPlanProjection.Project(new AllocationPlanRequest(null, null, null, null));

        Assert.That(plan.IsFireOrder, Is.False);
        Assert.That(plan.Cells, Is.Empty);
        Assert.That(
            AllocationPlanProjection.Fingerprint(plan),
            Is.EqualTo(AllocationPlanProjection.Fingerprint(AllocationPlan.Empty)));
        Assert.That(
            AllocationPlanProjection.Fingerprint(AllocationPlanProjection.Project(null)),
            Is.EqualTo("ap:empty"));
    }

    [Test]
    public void Source_fence_excludes_unity_orders_and_sim_session()
    {
        var source = File.ReadAllText(ProjectionPath());

        Assert.That(source, Does.Contain("EngageAttackOptions.Build"));
        Assert.That(source, Does.Contain("EngageAttackOrderResolver.TryResolve"));
        Assert.That(source, Does.Contain("WeaponRecommendation"));
        Assert.That(source, Does.Contain("EmploymentLedgerProjection.Project"));
        Assert.That(source, Does.Contain("EngageExplainProjection.ExplainCode"));
        Assert.That(source, Does.Contain("FireAbortReason"));
        Assert.That(source, Does.Not.Contain("UnityEngine"));
        Assert.That(source, Does.Not.Contain("MonoBehaviour"));
        Assert.That(source, Does.Not.Contain("ApplyOrder"));
        Assert.That(source, Does.Not.Contain("DecisionLog"));
        Assert.That(source, Does.Not.Contain("SimulationSession"));
        Assert.That(source, Does.Not.Contain("DelegationBridge"));
        Assert.That(source, Does.Not.Contain("TryIssuePlayerCommand"));
        Assert.That(source, Does.Not.Contain("TryEnqueueAttackOption"));
    }

    private static AllocationPlanRequest Plan(
        IReadOnlyList<string> shooters,
        IReadOnlyList<string> targets,
        IReadOnlyList<AllocationPlanAllocation> allocations,
        IReadOnlyList<EmploymentLedgerMagazineFacts> magazines) =>
        new(
            shooters.Select(id => new AllocationPlanShooter(id)).ToArray(),
            targets.Select(id => new AllocationPlanTarget(id)).ToArray(),
            allocations,
            magazines);

    private static AllocationPlanAllocation Line(
        string shooter,
        string target,
        int quantity,
        int rounds,
        bool track = true,
        WeaponRecommendation? recommendation = null)
    {
        var context = new EngageContext(
            50_000,
            new WeaponEnvelope(1_000, 100_000),
            RoundsRemaining: rounds,
            HasFireControlTrack: track,
            SalvoSize: 1);
        return new AllocationPlanAllocation(
            shooter,
            Weapon,
            target,
            quantity,
            context,
            EngagePreviewProjection.Project(context, DlzPersonality.Normal),
            recommendation);
    }

    private static string ClearText(int rounds)
    {
        var context = new EngageContext(
            50_000,
            new WeaponEnvelope(1_000, 100_000),
            RoundsRemaining: rounds,
            HasFireControlTrack: true,
            SalvoSize: 1);
        return EngageExplainProjection.Project(EngagePreviewProjection.Project(context, DlzPersonality.Normal)).ReasonPlain;
    }

    private static string ProjectionPath()
    {
        var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "ProjectAegis.sln")))
        {
            dir = dir.Parent;
        }

        Assert.That(dir, Is.Not.Null, "repo root not found");
        return Path.Combine(
            dir!.FullName,
            "src",
            "ProjectAegis.Delegation",
            "Projection",
            "AllocationPlanProjection.cs");
    }
}
