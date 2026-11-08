namespace ProjectAegis.Delegation.UnityAdapter.Tests.CommandReview;

using Core;
using MissionIntent;
using ProjectAegis.Delegation.UnityAdapter.CommandReview;
using NUnit.Framework;

[TestFixture]
public sealed class CoordinationScenarioTests
{
    [Test]
    public void Run_exercises_review_recommendation_human_retask_log_and_unit_effects()
    {
        var result = CoordinationScenario.Run(CoordinationScenario.CreateDefaultInput());

        Assert.That(result.Review.Groups.Single().Intent.AdvisoryRetask, Is.EqualTo(MissionIntentRetaskAdvice.Withdraw));
        Assert.That(result.Decision.Accepted, Is.True);
        Assert.That(result.Decision.Decision, Is.EqualTo(CoordinationDecision.Withdraw));
        Assert.That(result.Log.PlayerOrders, Has.Count.EqualTo(1));
        Assert.That(result.Log.PlayerOrders.Single().Kind, Is.EqualTo(OrderKind.ReturnToBase));
        Assert.That(result.AppliedOrders, Has.Count.EqualTo(2));
        Assert.That(result.AppliedOrders.All(o => o.Kind == OrderKind.ReturnToBase), Is.True);
        Assert.That(result.AppliedOrders.Select(o => o.UnitId), Is.EqualTo(new[] { "slice-c-c2", "slice-c-shooter" }));
        Assert.That(result.AfterTick.Groups.Single().Effects, Has.Count.EqualTo(2));
    }

    [Test]
    public void Run_same_input_has_deterministic_log_and_effect_order()
    {
        var input = CoordinationScenario.CreateDefaultInput();
        var first = CoordinationScenario.Run(input);
        var second = CoordinationScenario.Run(input);

        Assert.That(first.Log.ComputeFingerprint(), Is.EqualTo(second.Log.ComputeFingerprint()));
        Assert.That(first.AppliedOrders, Is.EqualTo(second.AppliedOrders));
    }
}
