using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

/// <summary>
/// Req 20 P0 T2: context-menu shell enumeration + core action intent creation.
/// Eligibility: empty selection → map-only; unit selection → unit actions; replay → none.
/// </summary>
[TestFixture]
public sealed class ContextMenuShellTests
{
    private static ContextMenuShell CreateShell() =>
        CoreContextActionRegistration.CreateShellWithCoreActions();

    private static ContextActionEvaluationContext UnitCtx(
        string[] units,
        bool isMapSurface = true,
        bool isReplay = false) =>
        new(units, isMapSurface, isReplay, primaryUnitId: units.Length > 0 ? units[0] : null);

    private static ContextActionEvaluationContext MapEmptyCtx(
        bool isMapSurface = true,
        bool isReplay = false) =>
        new(Array.Empty<string>(), isMapSurface, isReplay);

    [Test]
    public void Core_registration_exposes_six_providers_in_order()
    {
        var registry = CoreContextActionRegistration.CreateRegistryWithCoreActions();
        Assert.That(
            registry.Providers.Select(p => p.ActionId).ToArray(),
            Is.EqualTo(CoreContextActionRegistration.CoreActionIds));
    }

    [Test]
    public void Unit_selection_enumerates_unit_actions_only()
    {
        var shell = CreateShell();
        var rows = shell.Enumerate(UnitCtx(["u1", "u2"]));

        Assert.That(rows.Select(r => r.ActionId).ToArray(), Is.EqualTo(new[]
        {
            ContextActionIds.AttackOptions,
            ContextActionIds.PlotCourse,
            ContextActionIds.Formation,
            ContextActionIds.AssignMission,
        }));
        Assert.That(rows.All(r => !string.IsNullOrWhiteSpace(r.Label)), Is.True);
    }

    [Test]
    public void Empty_map_selection_enumerates_map_only_actions()
    {
        var shell = CreateShell();
        var rows = shell.Enumerate(MapEmptyCtx());

        Assert.That(rows.Select(r => r.ActionId).ToArray(), Is.EqualTo(new[]
        {
            ContextActionIds.MeasureDistance,
            ContextActionIds.AddReferencePoint,
        }));
    }

    [Test]
    public void Empty_selection_off_map_enumerates_nothing()
    {
        var shell = CreateShell();
        var rows = shell.Enumerate(MapEmptyCtx(isMapSurface: false));

        Assert.That(rows, Is.Empty);
    }

    [Test]
    public void Replay_enumerates_nothing_for_unit_or_map()
    {
        var shell = CreateShell();

        Assert.That(shell.Enumerate(UnitCtx(["u1"], isReplay: true)), Is.Empty);
        Assert.That(shell.Enumerate(MapEmptyCtx(isReplay: true)), Is.Empty);
    }

    [Test]
    public void Invoke_attack_options_creates_intent_for_selection()
    {
        var shell = CreateShell();
        var ctx = UnitCtx(["u1", "u2"]);

        var intent = shell.Invoke(ContextActionIds.AttackOptions, ctx);

        Assert.That(intent, Is.Not.Null);
        Assert.That(intent!.ActionId, Is.EqualTo(ContextActionIds.AttackOptions));
        Assert.That(intent.IntentKind, Is.EqualTo(ContextActionIntentKinds.AttackOptions));
        Assert.That(intent.TargetUnitIds.ToArray(), Is.EqualTo(new[] { "u1", "u2" }));
        Assert.That(intent.Detail, Is.EqualTo("u1"));
    }

    [Test]
    public void Invoke_plot_course_and_formation_create_unit_intents()
    {
        var shell = CreateShell();
        var ctx = UnitCtx(["lead"]);

        var plot = shell.Invoke(ContextActionIds.PlotCourse, ctx);
        var form = shell.Invoke(ContextActionIds.Formation, ctx);

        Assert.That(plot, Is.Not.Null);
        Assert.That(plot!.IntentKind, Is.EqualTo(ContextActionIntentKinds.PlotCourse));
        Assert.That(plot.TargetUnitIds.ToArray(), Is.EqualTo(new[] { "lead" }));

        Assert.That(form, Is.Not.Null);
        Assert.That(form!.IntentKind, Is.EqualTo(ContextActionIntentKinds.Formation));
        Assert.That(form.TargetUnitIds.ToArray(), Is.EqualTo(new[] { "lead" }));
    }

    [Test]
    public void Invoke_assign_mission_emits_T6_stub_intent_kind()
    {
        var shell = CreateShell();
        var intent = shell.Invoke(ContextActionIds.AssignMission, UnitCtx(["u1"]));

        Assert.That(intent, Is.Not.Null);
        Assert.That(intent!.ActionId, Is.EqualTo(ContextActionIds.AssignMission));
        Assert.That(intent.IntentKind, Is.EqualTo(ContextActionIntentKinds.AssignMissionStub));
        Assert.That(intent.Detail, Is.EqualTo("stub"));
        Assert.That(intent.TargetUnitIds.ToArray(), Is.EqualTo(new[] { "u1" }));
    }

    [Test]
    public void Invoke_map_actions_create_intents_with_empty_targets()
    {
        var shell = CreateShell();
        var ctx = MapEmptyCtx();

        var measure = shell.Invoke(ContextActionIds.MeasureDistance, ctx);
        var refPoint = shell.Invoke(ContextActionIds.AddReferencePoint, ctx);

        Assert.That(measure, Is.Not.Null);
        Assert.That(measure!.IntentKind, Is.EqualTo(ContextActionIntentKinds.MeasureDistance));
        Assert.That(measure.TargetUnitIds, Is.Empty);

        Assert.That(refPoint, Is.Not.Null);
        Assert.That(refPoint!.IntentKind, Is.EqualTo(ContextActionIntentKinds.AddReferencePoint));
        Assert.That(refPoint.TargetUnitIds, Is.Empty);
    }

    [Test]
    public void Invoke_map_action_while_unit_selected_returns_null()
    {
        var shell = CreateShell();
        var ctx = UnitCtx(["u1"]);

        Assert.That(shell.Invoke(ContextActionIds.MeasureDistance, ctx), Is.Null);
        Assert.That(shell.Invoke(ContextActionIds.AddReferencePoint, ctx), Is.Null);
    }

    [Test]
    public void Invoke_unit_action_with_empty_selection_returns_null()
    {
        var shell = CreateShell();
        var ctx = MapEmptyCtx();

        Assert.That(shell.Invoke(ContextActionIds.AttackOptions, ctx), Is.Null);
        Assert.That(shell.Invoke(ContextActionIds.PlotCourse, ctx), Is.Null);
        Assert.That(shell.Invoke(ContextActionIds.Formation, ctx), Is.Null);
        Assert.That(shell.Invoke(ContextActionIds.AssignMission, ctx), Is.Null);
    }

    [Test]
    public void Invoke_unknown_or_blank_action_returns_null()
    {
        var shell = CreateShell();
        var ctx = UnitCtx(["u1"]);

        Assert.That(shell.Invoke("doctrine_stub_not_t2", ctx), Is.Null);
        Assert.That(shell.Invoke("", ctx), Is.Null);
        Assert.That(shell.Invoke("   ", ctx), Is.Null);
    }

    [Test]
    public void Invoke_during_replay_returns_null()
    {
        var shell = CreateShell();
        Assert.That(
            shell.Invoke(ContextActionIds.AttackOptions, UnitCtx(["u1"], isReplay: true)),
            Is.Null);
        Assert.That(
            shell.Invoke(ContextActionIds.MeasureDistance, MapEmptyCtx(isReplay: true)),
            Is.Null);
    }

    [Test]
    public void Shell_rejects_null_registry_and_null_context()
    {
        Assert.Throws<ArgumentNullException>((Action)(() => _ = new ContextMenuShell(null!)));
        var shell = CreateShell();
        Assert.Throws<ArgumentNullException>((Action)(() => shell.Enumerate(null!)));
        Assert.Throws<ArgumentNullException>((Action)(() => shell.Invoke(ContextActionIds.PlotCourse, null!)));
    }
}
