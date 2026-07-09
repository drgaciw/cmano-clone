using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

/// <summary>
/// Req 20 P0 T6: assign_mission IContextActionProvider — selected units → logged intent;
/// registry registration only (no T2 menu shell edits).
/// </summary>
[TestFixture]
public sealed class AssignMissionContextActionProviderTests
{
    [Test]
    public void IsEligible_true_when_units_selected_and_not_replay()
    {
        var provider = new AssignMissionContextActionProvider();
        var ctx = new ContextActionEvaluationContext(["u1", "u2"], isMapSurface: false, isReplay: false);

        Assert.That(provider.IsEligible(ctx), Is.True);
    }

    [Test]
    public void IsEligible_false_when_no_selection()
    {
        var provider = new AssignMissionContextActionProvider();
        var ctx = new ContextActionEvaluationContext([], isMapSurface: false, isReplay: false);

        Assert.That(provider.IsEligible(ctx), Is.False);
    }

    [Test]
    public void IsEligible_false_when_replay()
    {
        var provider = new AssignMissionContextActionProvider();
        var ctx = new ContextActionEvaluationContext(["u1"], isMapSurface: true, isReplay: true);

        Assert.That(provider.IsEligible(ctx), Is.False);
    }

    [Test]
    public void CreateIntent_targets_selected_units()
    {
        var provider = new AssignMissionContextActionProvider();
        var ctx = new ContextActionEvaluationContext(["u1", "u2"], isMapSurface: false, isReplay: false, primaryUnitId: "u1");

        var intent = provider.CreateIntent(ctx);

        Assert.That(intent, Is.Not.Null);
        Assert.That(intent!.ActionId, Is.EqualTo(AssignMissionContextActionProvider.ActionIdValue));
        Assert.That(intent.IntentKind, Is.EqualTo(AssignMissionContextActionProvider.IntentKindValue));
        Assert.That(intent.TargetUnitIds.ToArray(), Is.EqualTo(new[] { "u1", "u2" }));
    }

    [Test]
    public void CreateIntent_null_when_ineligible()
    {
        var provider = new AssignMissionContextActionProvider();
        var ctx = new ContextActionEvaluationContext([], isMapSurface: false, isReplay: false);

        Assert.That(provider.CreateIntent(ctx), Is.Null);
    }

    [Test]
    public void RegisterDefaults_registers_assign_mission_once()
    {
        var registry = new ContextActionRegistry();
        MissionRuntimeContextActions.RegisterDefaults(registry);
        MissionRuntimeContextActions.RegisterDefaults(registry); // idempotent

        Assert.That(registry.Providers.Count, Is.EqualTo(1));
        Assert.That(registry.Providers[0].ActionId, Is.EqualTo("assign_mission"));

        var eligible = registry.Eligible(new ContextActionEvaluationContext(["u1"], isMapSurface: false, isReplay: false));
        Assert.That(eligible, Has.Count.EqualTo(1));

        var blocked = registry.Eligible(new ContextActionEvaluationContext(["u1"], isMapSurface: false, isReplay: true));
        Assert.That(blocked, Is.Empty);
    }
}
