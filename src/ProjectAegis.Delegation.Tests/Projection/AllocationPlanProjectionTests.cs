using System.Globalization;
using NUnit.Framework;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Sim.Engage;
using ProjectAegis.Sim.Glossary;
using ProjectAegis.Sim.Policy;

namespace ProjectAegis.Delegation.Tests.Projection;

public sealed class AllocationPlanProjectionTests
{
    [Test]
    public void Two_shooters_three_targets_expose_rows_totals_and_ledger_remainders()
    {
        var plan = Matrix();
        Assert.That(plan.IsValid, Is.True);
        Assert.That(plan.Rows.Count, Is.EqualTo(6));
        Assert.That(plan.Rows.All(r => r.CanFire), Is.True);
        Assert.That(plan.Shooters.Select(s => (s.ShooterUnitId, s.RequestedRounds)),
            Is.EqualTo(new[] { (1UL, 4L), (2UL, 4L) }));
        Assert.That(plan.Targets.Select(t => (t.TargetId, t.RequestedRounds)),
            Is.EqualTo(new[] { ("a", 3L), ("b", 3L), ("c", 2L) }));
        Assert.That(plan.Weapons.Select(w => (w.AvailableRounds, w.RemainingAfterPlan)),
            Is.EqualTo(new[] { (10, 6L), (8, 4L) }));
        Assert.That(plan.Rows.Select(r => (r.ShooterUnitId, r.TargetId)),
            Is.EqualTo(new[] { (1UL, "a"), (1UL, "b"), (1UL, "c"), (2UL, "a"), (2UL, "b"), (2UL, "c") }));
    }

    [Test]
    public void Mixed_policy_and_preview_denials_preserve_domain_reasons()
    {
        var facts = Facts();
        facts[0] = facts[0] with { PolicyAbortReason = FireAbortReason.OffGrid };
        facts[1] = facts[1] with { Preview = new EngagePreview("blocked", false, AbortReasonCatalog.Engage.DLZ_OUT) };
        var plan = Matrix(facts);
        Assert.That(plan.IsValid, Is.False);
        Assert.That(plan.Rows[0].CanFire, Is.False);
        Assert.That(plan.Rows[0].PolicyAbortReason, Is.EqualTo(FireAbortReason.OffGrid));
        Assert.That(plan.Rows[1].AbortCode, Is.EqualTo(AbortReasonCatalog.Engage.DLZ_OUT));
        Assert.That(plan.Rows.Skip(2).All(r => r.CanFire), Is.True);
        Assert.That(plan.Shooters[0].RequestedRounds, Is.EqualTo(4));
    }

    [Test]
    public void Collective_overallocation_rejects_all_affected_targets_without_partial_acceptance()
    {
        var plan = Single(
            [new(1, 10, "w", "a", 2), new(1, 10, "w", "b", 2)], available: 3);
        Assert.That(plan.IsValid, Is.False);
        Assert.That(plan.Rows.All(r => !r.CanFire && r.AbortCode == AbortReasonCatalog.Engage.NO_AMMO), Is.True);
        Assert.That(plan.Weapons[0].AvailableRounds, Is.EqualTo(3));
        Assert.That(plan.Weapons[0].MagazineRequestedRounds, Is.EqualTo(4));
        Assert.That(plan.Weapons[0].RemainingAfterPlan, Is.Zero);
        Assert.That(plan.Weapons[0].Shortfall, Is.EqualTo(1));
    }

    [Test]
    public void Duplicate_intents_are_combined_before_shared_magazine_validation()
    {
        var plan = Single([new(1, 10, "w", "a", 2), new(1, 10, "w", "a", 2)], available: 3);
        Assert.That(plan.Rows[0].RequestedRounds, Is.EqualTo(4));
        Assert.That(plan.Rows.Count, Is.EqualTo(2));
        Assert.That(plan.IsValid, Is.False);
        Assert.That(plan.Targets[0].RequestedRounds, Is.EqualTo(4));
    }

    [Test]
    public void Different_weapons_on_same_mount_share_one_budget()
    {
        var plan = AllocationPlanProjection.Build([1UL], ["a", "b"],
            [new(1, 10, "w", "weapon"), new(1, 10, "x", "other")],
            [Fact(1, 10, "w", "a"), Fact(1, 10, "x", "b")],
            [new(1, 10, 5, 8)],
            [new(1, 10, "w", "a", 3), new(1, 10, "x", "b", 3)]);
        Assert.That(plan.IsValid, Is.False);
        Assert.That(plan.Weapons.Select(w => w.RequestedRounds), Is.EqualTo(new[] { 3L, 3L }));
        Assert.That(plan.Weapons.All(w => w.MagazineRequestedRounds == 6 && w.Shortfall == 1), Is.True);
    }

    [Test]
    public void Same_mount_number_on_different_shooters_and_different_mounts_keep_separate_budgets()
    {
        var plan = AllocationPlanProjection.Build([1UL, 2UL], ["a"],
            [new(1, 10, "w", "weapon"), new(1, 11, "w", "weapon"), new(2, 10, "w", "weapon")],
            [Fact(1, 10, "w", "a"), Fact(1, 11, "w", "a"), Fact(2, 10, "w", "a")],
            [new(1, 10, 2, 2), new(1, 11, 2, 2), new(2, 10, 2, 2)],
            [new(1, 10, "w", "a", 2), new(1, 11, "w", "a", 2), new(2, 10, "w", "a", 2)]);
        Assert.That(plan.IsValid, Is.True);
        Assert.That(plan.Weapons.All(w => w.RemainingAfterPlan == 0), Is.True);
    }

    [Test]
    public void Blocked_intents_still_count_in_hypothetical_remainder()
    {
        var facts = new[] { Fact(1, 10, "w", "a") with { PolicyAbortReason = FireAbortReason.RoeHoldFire }, Fact(1, 10, "w", "b") };
        var plan = AllocationPlanProjection.Build([1UL], ["a", "b"], [new(1, 10, "w", "weapon")], facts,
            [new(1, 10, 3, 5)], [new(1, 10, "w", "a", 2), new(1, 10, "w", "b", 2)]);
        Assert.That(plan.Rows[0].PolicyAbortReason, Is.EqualTo(FireAbortReason.RoeHoldFire));
        Assert.That(plan.Rows[1].AbortCode, Is.EqualTo(AbortReasonCatalog.Engage.NO_AMMO));
        Assert.That(plan.Weapons[0].AvailableRounds, Is.EqualTo(3));
        Assert.That(plan.Weapons[0].RemainingAfterPlan, Is.Zero);
    }

    [Test]
    public void Reordered_inputs_and_cultures_produce_identical_snapshots()
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            var first = Matrix();
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
            var second = AllocationPlanProjection.Build(new[] { 1UL, 2UL }.Reverse(), new[] { "a", "b", "c" }.Reverse(),
                Weapons().Reverse(), Facts().Reverse(), Magazines().Reverse(), Requests().Reverse());
            Assert.That(second.Rows, Is.EqualTo(first.Rows));
            Assert.That(second.Weapons, Is.EqualTo(first.Weapons));
            Assert.That(second.Shooters, Is.EqualTo(first.Shooters));
            Assert.That(second.Targets, Is.EqualTo(first.Targets));
        }
        finally { CultureInfo.CurrentCulture = original; }
    }

    [Test]
    public void Target_order_is_ordinal_and_case_sensitive()
    {
        var targets = new[] { "ı", "i", "I" };
        var plan = AllocationPlanProjection.Build([1UL], targets, [new(1, 10, "w", "weapon")],
            targets.Select(t => Fact(1, 10, "w", t)), [new(1, 10, 3, 3)], []);
        Assert.That(plan.Targets.Select(t => t.TargetId), Is.EqualTo(new[] { "I", "i", "ı" }));
        Assert.That(plan.Rows.Select(r => r.TargetId), Is.EqualTo(new[] { "I", "i", "ı" }));
    }

    [Test]
    public void Ledger_and_source_collections_remain_unchanged_and_result_is_read_only()
    {
        var ledger = new MagazineLedger();
        ledger.SetRounds(1, 10, 5);
        var facts = new List<AllocationEngagementFact> { Fact(1, 10, "w", "a") };
        var weapons = new List<AllocationWeaponFact> { new(1, 10, "w", "weapon") };
        var requests = new List<AllocationRequest> { new(1, 10, "w", "a", 2) };
        var before = ledger.Snapshot().ToArray();
        var plan = AllocationPlanProjection.Build([1UL], ["a"], weapons, facts, ledger.Snapshot(), requests);
        Assert.That(ledger.Snapshot(), Is.EqualTo(before));
        Assert.That(facts.Count, Is.EqualTo(1));
        Assert.That(requests[0].Rounds, Is.EqualTo(2));
        facts.Clear();
        weapons.Clear();
        requests.Clear();
        ledger.TryConsume(1, 10);
        Assert.That(plan.Rows.Count, Is.EqualTo(1));
        Assert.That(plan.Weapons[0].AvailableRounds, Is.EqualTo(5));
        Assert.That(plan.Weapons[0].RemainingAfterPlan, Is.EqualTo(3));
        Assert.Throws<NotSupportedException>(() => ((IList<AllocationPlanRow>)plan.Rows).Clear());
        Assert.Throws<NotSupportedException>(() => ((IList<AllocationWeaponSummary>)plan.Weapons).Clear());
        Assert.Throws<NotSupportedException>(() => ((IList<AllocationShooterTotal>)plan.Shooters).Clear());
        Assert.Throws<NotSupportedException>(() => ((IList<AllocationTargetTotal>)plan.Targets).Clear());
    }

    [Test]
    public void Large_duplicate_quantities_do_not_wrap_into_legal_budget()
    {
        var plan = Single([new(1, 10, "w", "a", int.MaxValue), new(1, 10, "w", "a", 1)], int.MaxValue);
        Assert.That(plan.Rows[0].RequestedRounds, Is.EqualTo((long)int.MaxValue + 1));
        Assert.That(plan.IsValid, Is.False);
        Assert.That(plan.Weapons[0].Shortfall, Is.EqualTo(1));
    }

    [Test]
    public void Empty_plan_is_valid_and_zero_requests_do_not_consume_stock()
    {
        var plan = Single([new(1, 10, "w", "a", 0)], available: 3);
        Assert.That(plan.IsValid, Is.True);
        Assert.That(plan.Weapons[0].RemainingAfterPlan, Is.EqualTo(3));
        var empty = AllocationPlanProjection.Build([], [], [], [], [], []);
        Assert.That(empty.IsValid, Is.True);
        Assert.That(empty.Rows, Is.Empty);
    }

    [Test]
    public void Tracked_empty_magazine_has_existing_no_ammo_code()
    {
        var plan = Single([new(1, 10, "w", "a", 1)], available: 0);
        Assert.That(plan.Rows[0].AbortCode, Is.EqualTo(AbortReasonCatalog.Engage.NO_AMMO));
        Assert.That(plan.IsValid, Is.False);
    }

    [Test]
    public void Missing_engagement_or_magazine_facts_are_rejected_without_guessing()
    {
        Assert.Throws<ArgumentException>(() => AllocationPlanProjection.Build([1UL], ["a"], Weapons().Take(1), [],
            Magazines(), [new(1, 10, "w", "a", 1)]));
        Assert.Throws<ArgumentException>(() => AllocationPlanProjection.Build([1UL], ["a"], Weapons().Take(1),
            [Fact(1, 10, "w", "a")], [], []));
    }

    [TestCase(-1, 2)]
    [TestCase(3, 2)]
    public void Invalid_ledger_counts_are_rejected(int remaining, int capacity)
    {
        Assert.Throws<ArgumentException>(() => AllocationPlanProjection.Build([1UL], ["a"], Weapons().Take(1),
            [Fact(1, 10, "w", "a")], [new(1, 10, remaining, capacity)], []));
    }

    [Test]
    public void Duplicate_authoritative_facts_are_rejected()
    {
        Assert.Throws<ArgumentException>(() => AllocationPlanProjection.Build([1UL, 1UL], [], [], [], [], []));
        Assert.Throws<ArgumentException>(() => AllocationPlanProjection.Build([], ["a", "a"], [], [], [], []));
        Assert.Throws<ArgumentException>(() => AllocationPlanProjection.Build([1UL], [], [Weapons()[0], Weapons()[0]], [], Magazines(), []));
        Assert.Throws<ArgumentException>(() => AllocationPlanProjection.Build([1UL], ["a"], [Weapons()[0]],
            [Fact(1, 10, "w", "a"), Fact(1, 10, "w", "a")], Magazines(), []));
        Assert.Throws<ArgumentException>(() => AllocationPlanProjection.Build([1UL], [], [Weapons()[0]], [],
            [Magazines()[0], Magazines()[0]], []));
    }

    [Test]
    public void Unknown_references_negative_requests_and_blank_ids_are_rejected()
    {
        Assert.Throws<ArgumentException>(() => Single([new(1, 10, "w", "a", -1)], 3));
        Assert.Throws<ArgumentException>(() => Single([new(1, 10, "unknown", "a", 1)], 3));
        Assert.Throws<ArgumentException>(() => Single([new(1, 10, "w", "unknown", 1)], 3));
        Assert.Throws<ArgumentException>(() => Single([new(2, 10, "w", "a", 1)], 3));
        Assert.Throws<ArgumentException>(() => AllocationPlanProjection.Build([], [" "], [], [], [], []));
        Assert.Throws<ArgumentException>(() => AllocationPlanProjection.Build([1UL], [], [new(1, 10, "", "weapon")], [], Magazines(), []));
        Assert.Throws<ArgumentException>(() => AllocationPlanProjection.Build([1UL], ["a"], [Weapons()[0]], [Fact(2, 10, "w", "a")], Magazines(), []));
    }

    [Test]
    public void Contradictory_or_missing_abort_facts_are_rejected_and_unknown_source_code_is_preserved()
    {
        foreach (var bad in new[] { new EngagePreview("x", false, null), new EngagePreview("x", true, "abort") })
        {
            var invalidPreviewFacts = Facts();
            invalidPreviewFacts[0] = invalidPreviewFacts[0] with { Preview = bad };
            var error = Assert.Throws<ArgumentException>(() => Matrix(invalidPreviewFacts));
            Assert.That(error!.ParamName, Is.EqualTo("engagements"));
        }
        var invalidPolicyFacts = Facts();
        invalidPolicyFacts[0] = invalidPolicyFacts[0] with { PolicyAbortReason = (FireAbortReason)999 };
        var policyError = Assert.Throws<ArgumentException>(() => Matrix(invalidPolicyFacts));
        Assert.That(policyError!.ParamName, Is.EqualTo("engagements"));
        var facts = Facts();
        facts[0] = facts[0] with { Preview = new EngagePreview("x", false, "FUTURE_DOMAIN_ABORT") };
        Assert.That(Matrix(facts).Rows[0].AbortCode, Is.EqualTo("FUTURE_DOMAIN_ABORT"));
    }

    [Test]
    public void Null_collections_and_null_facts_are_rejected()
    {
        Assert.Throws<ArgumentNullException>(() => AllocationPlanProjection.Build(null!, [], [], [], [], []));
        Assert.Throws<ArgumentNullException>(() => AllocationPlanProjection.Build([], null!, [], [], [], []));
        Assert.Throws<ArgumentNullException>(() => AllocationPlanProjection.Build([], [], null!, [], [], []));
        Assert.Throws<ArgumentNullException>(() => AllocationPlanProjection.Build([], [], [], null!, [], []));
        Assert.Throws<ArgumentNullException>(() => AllocationPlanProjection.Build([], [], [], [], null!, []));
        Assert.Throws<ArgumentNullException>(() => AllocationPlanProjection.Build([], [], [], [], [], null!));
        Assert.Throws<ArgumentException>(() => AllocationPlanProjection.Build([1UL], [], [null!], [], [], []));
        Assert.Throws<ArgumentException>(() => Matrix([null!]));
        Assert.Throws<ArgumentException>(() => Single([null!], 3));
    }

    private static AllocationPlanSnapshot Matrix(AllocationEngagementFact[]? facts = null) =>
        AllocationPlanProjection.Build([2UL, 1UL], ["c", "b", "a"], Weapons(), facts ?? Facts(), Magazines(), Requests());

    private static AllocationPlanSnapshot Single(AllocationRequest[] requests, int available) =>
        AllocationPlanProjection.Build([1UL], ["a", "b"], [new(1, 10, "w", "weapon")],
            [Fact(1, 10, "w", "a"), Fact(1, 10, "w", "b")], [new(1, 10, available, available)], requests);

    private static AllocationWeaponFact[] Weapons() => [new(1, 10, "w", "missile"), new(2, 20, "w", "missile")];
    private static MagazineLedgerEntry[] Magazines() => [new(1, 10, 10, 10), new(2, 20, 8, 8)];
    private static AllocationEngagementFact[] Facts() =>
        (from s in new[] { 1UL, 2UL } from t in new[] { "a", "b", "c" } select Fact(s, s * 10, "w", t)).ToArray();
    private static AllocationEngagementFact Fact(ulong shooter, ulong mount, string weapon, string target) =>
        new(shooter, mount, weapon, target, new EngagePreview("in range", true, null), FireAbortReason.None);
    private static AllocationRequest[] Requests() =>
        [new(1, 10, "w", "a", 1), new(1, 10, "w", "b", 2), new(1, 10, "w", "c", 1),
         new(2, 20, "w", "a", 2), new(2, 20, "w", "b", 1), new(2, 20, "w", "c", 1)];
}
