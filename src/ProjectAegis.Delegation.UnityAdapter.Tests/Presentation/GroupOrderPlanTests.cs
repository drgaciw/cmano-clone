using ProjectAegis.Delegation.UnityAdapter.Presentation;
using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

/// <summary>req 20 §Selection, TR-c2-005 (AC-7): group-order eligibility + fan-out planning. Pure —
/// no DelegationBridge dependency; the evaluator delegate stands in for live projections.</summary>
[TestFixture]
public sealed class GroupOrderPlanTests
{
    [Test]
    public void Build_routes_eligible_units_to_EligibleUnitIds_in_selection_order()
    {
        var plan = GroupOrderPlan.Build(
            new[] { "u1", "u2", "u3" },
            id => GroupOrderUnitVerdict.Eligible(id));

        Assert.That(plan.EligibleUnitIds, Is.EqualTo(new[] { "u1", "u2", "u3" }));
        Assert.That(plan.IneligibleUnits, Is.Empty);
        Assert.That(plan.HasAnyEligible, Is.True);
    }

    [Test]
    public void Build_drops_destroyed_units_and_names_them_in_IneligibleUnits()
    {
        var plan = GroupOrderPlan.Build(
            new[] { "u1", "u2", "u3" },
            id => id == "u2"
                ? GroupOrderUnitVerdict.Ineligible(id, GroupOrderIneligibleReason.Destroyed)
                : GroupOrderUnitVerdict.Eligible(id));

        Assert.That(plan.EligibleUnitIds, Is.EqualTo(new[] { "u1", "u3" }),
            "destroyed unit dropped; fan-out proceeds for survivors only (GDD edge case)");
        Assert.That(plan.IneligibleUnits, Has.Count.EqualTo(1));
        Assert.That(plan.IneligibleUnits[0].UnitId, Is.EqualTo("u2"));
        Assert.That(plan.IneligibleUnits[0].Reason, Is.EqualTo(GroupOrderIneligibleReason.Destroyed));
    }

    [Test]
    public void Build_reports_option_not_available_and_unknown_unit_reasons()
    {
        var plan = GroupOrderPlan.Build(
            new[] { "u1", "u2" },
            id => id == "u1"
                ? GroupOrderUnitVerdict.Ineligible(id, GroupOrderIneligibleReason.OptionNotAvailable)
                : GroupOrderUnitVerdict.Ineligible(id, GroupOrderIneligibleReason.UnknownUnit));

        Assert.That(plan.EligibleUnitIds, Is.Empty);
        Assert.That(plan.HasAnyEligible, Is.False);
        Assert.That(plan.IneligibleUnits.Select(v => v.Reason), Is.EqualTo(new[]
        {
            GroupOrderIneligibleReason.OptionNotAvailable,
            GroupOrderIneligibleReason.UnknownUnit,
        }));
    }

    [Test]
    public void Build_evaluates_each_id_exactly_once_and_deduplicates_input()
    {
        var evaluationCount = 0;
        var plan = GroupOrderPlan.Build(
            new[] { "u1", "u1", "u2" },
            id =>
            {
                evaluationCount++;
                return GroupOrderUnitVerdict.Eligible(id);
            });

        Assert.That(evaluationCount, Is.EqualTo(2), "duplicate selection ids are only evaluated once");
        Assert.That(plan.EligibleUnitIds, Is.EqualTo(new[] { "u1", "u2" }));
    }

    [Test]
    public void Build_AllVerdicts_preserves_full_selection_order_including_ineligible()
    {
        var plan = GroupOrderPlan.Build(
            new[] { "u1", "u2", "u3" },
            id => id == "u2"
                ? GroupOrderUnitVerdict.Ineligible(id, GroupOrderIneligibleReason.Destroyed)
                : GroupOrderUnitVerdict.Eligible(id));

        Assert.That(plan.AllVerdicts.Select(v => v.UnitId), Is.EqualTo(new[] { "u1", "u2", "u3" }));
    }

    [Test]
    public void Build_with_empty_selection_yields_empty_plan()
    {
        var plan = GroupOrderPlan.Build(Array.Empty<string>(), GroupOrderUnitVerdict.Eligible);

        Assert.That(plan.EligibleUnitIds, Is.Empty);
        Assert.That(plan.IneligibleUnits, Is.Empty);
        Assert.That(plan.HasAnyEligible, Is.False);
    }
}
