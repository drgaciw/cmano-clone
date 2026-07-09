using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

/// <summary>Req 20 P0: <c>delegate_agent</c> context action registration + intent factory.</summary>
[TestFixture]
public sealed class DelegateAgentContextActionTests
{
    [Test]
    public void Register_exposes_delegate_agent_when_units_selected()
    {
        var registry = new ContextActionRegistry();
        DelegateAgentContextAction.Register(registry);

        var ctx = new ContextActionEvaluationContext(["u1"], isMapSurface: false, isReplay: false);
        var eligible = registry.Eligible(ctx);

        Assert.That(eligible, Has.Count.EqualTo(1));
        Assert.That(eligible[0].ActionId, Is.EqualTo(DelegateAgentContextAction.ActionIdValue));

        var intent = eligible[0].CreateIntent(ctx);
        Assert.That(intent, Is.Not.Null);
        Assert.That(intent!.IntentKind, Is.EqualTo(DelegateAgentContextAction.IntentKindValue));
        Assert.That(intent.TargetUnitIds.ToArray(), Is.EqualTo(new[] { "u1" }));
        Assert.That(intent.Detail, Is.EqualTo("FullAutonomous"));
    }

    [Test]
    public void IsEligible_false_when_no_selection_or_replay()
    {
        var action = new DelegateAgentContextAction();
        Assert.That(
            action.IsEligible(new ContextActionEvaluationContext([], isMapSurface: true, isReplay: false)),
            Is.False);
        Assert.That(
            action.IsEligible(new ContextActionEvaluationContext(["u1"], isMapSurface: true, isReplay: true)),
            Is.False);
    }
}
