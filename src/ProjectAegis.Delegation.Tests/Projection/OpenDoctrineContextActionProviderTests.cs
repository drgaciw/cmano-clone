using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

/// <summary>Req 20 P0 T3: open_doctrine context action registration seam.</summary>
[TestFixture]
public sealed class OpenDoctrineContextActionProviderTests
{
    [Test]
    public void ActionId_is_open_doctrine()
    {
        var provider = new OpenDoctrineContextActionProvider();
        Assert.That(provider.ActionId, Is.EqualTo("open_doctrine"));
        Assert.That(provider.Label, Does.Contain("Doctrine"));
    }

    [Test]
    public void IsEligible_when_unit_selected_and_not_replay()
    {
        var provider = new OpenDoctrineContextActionProvider();
        var ctx = new ContextActionEvaluationContext(["u1"], isMapSurface: true, isReplay: false);
        Assert.That(provider.IsEligible(ctx), Is.True);
    }

    [Test]
    public void IsEligible_false_when_replay_or_no_selection()
    {
        var provider = new OpenDoctrineContextActionProvider();
        Assert.That(
            provider.IsEligible(new ContextActionEvaluationContext(["u1"], isMapSurface: true, isReplay: true)),
            Is.False);
        Assert.That(
            provider.IsEligible(new ContextActionEvaluationContext([], isMapSurface: false, isReplay: false)),
            Is.False);
    }

    [Test]
    public void CreateIntent_targets_selection_with_OpenDoctrinePanel_kind()
    {
        var provider = new OpenDoctrineContextActionProvider();
        var ctx = new ContextActionEvaluationContext(
            ["u1", "u2"], isMapSurface: true, isReplay: false, primaryUnitId: "u1");
        var intent = provider.CreateIntent(ctx);
        Assert.That(intent, Is.Not.Null);
        Assert.That(intent!.ActionId, Is.EqualTo("open_doctrine"));
        Assert.That(intent.IntentKind, Is.EqualTo(OpenDoctrineContextActionProvider.IntentKindValue));
        Assert.That(intent.TargetUnitIds.ToArray(), Is.EqualTo(new[] { "u1", "u2" }));
        Assert.That(intent.Detail, Is.EqualTo("u1"));
    }

    [Test]
    public void Registry_Eligible_includes_open_doctrine()
    {
        var registry = new ContextActionRegistry();
        registry.Register(new OpenDoctrineContextActionProvider());
        var eligible = registry.Eligible(
            new ContextActionEvaluationContext(["u1"], isMapSurface: false, isReplay: false));
        Assert.That(eligible.Select(p => p.ActionId).ToArray(), Is.EqualTo(new[] { "open_doctrine" }));
    }
}
