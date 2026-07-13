using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Baltic;
using ProjectAegis.Delegation.UnityAdapter.Presentation;
using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

/// <summary>
/// Track T3 (req 20 §Alerting and Interruption, TR-c2-007/008, AC-9) end-to-end integration over a real
/// Baltic harness run: severity projection → toast queue → auto-pause command → click-to-focus.
/// </summary>
/// <remarks>
/// <b>AC-9 scope note:</b> "Critical alert triggers auto-pause" is proven up to the COMMAND boundary —
/// <see cref="AutoPausePolicy.Evaluate"/> returns the correct <see cref="AutoPauseCommand"/> for a
/// Critical alert sourced from a real harness run. Dispatching that command onto an actual sim
/// pause-reason stack is NOT exercised here because no such stack exists anywhere in this baseline (see
/// <see cref="AutoPauseCommand"/> remarks) — this is the reported blocker, not a gap in test coverage.
/// </remarks>
[TestFixture]
public sealed class AlertingIntegrationTests
{
    [Test]
    public void Critical_alert_from_harness_produces_auto_pause_command_but_does_not_touch_the_bridge()
    {
        var result = BalticReplayHarness.Run(42, "baltic-patrol", ticks: 4);
        var alerts = AlertProjection.Project(result.Messages);
        var criticalAlerts = new List<AlertItem>();
        foreach (var a in alerts)
        {
            if (a.Severity == AlertSeverity.Critical)
            {
                criticalAlerts.Add(a);
            }
        }

        Assert.That(criticalAlerts, Is.Not.Empty, "harness fixture is expected to contain a KILL_CONFIRMED line");

        var command = AutoPausePolicy.Evaluate(criticalAlerts, isReplay: false);

        Assert.That(command, Is.Not.Null);
        Assert.That(command!.Reason, Is.EqualTo(AutoPausePolicy.CriticalAlertReason));
        Assert.That(command.TriggerSequenceId, Is.EqualTo(criticalAlerts[0].SequenceId));
    }

    [Test]
    public void Replay_mode_suppresses_auto_pause_for_the_same_harness_alerts()
    {
        var result = BalticReplayHarness.Run(42, "baltic-patrol", ticks: 4);
        var alerts = AlertProjection.ProjectSeverity(result.Messages, AlertSeverity.Critical);

        Assert.That(AutoPausePolicy.Evaluate(alerts, isReplay: true), Is.Null);
    }

    [Test]
    public void Replay_mode_suppresses_toasts_for_the_same_harness_alerts()
    {
        var result = BalticReplayHarness.Run(42, "baltic-patrol", ticks: 4);
        var alerts = AlertProjection.Project(result.Messages);

        var replayStack = new ToastStackModel(isReplaySuppressed: true);
        foreach (var alert in alerts)
        {
            if (alert.Severity != AlertSeverity.Routine)
            {
                replayStack.Add(new ToastEntry(alert.SequenceId, alert.Severity, alert.Category, alert.Text, alert.UnitId));
            }
        }

        Assert.That(replayStack.TotalCount, Is.EqualTo(0));
        Assert.That(replayStack.VisibleToasts, Is.Empty);
    }

    [Test]
    public void Toast_click_resolves_focus_target_and_drives_presentation_selection()
    {
        var result = BalticReplayHarness.Run(42, "baltic-patrol", ticks: 4);
        var alerts = AlertProjection.ProjectSeverity(result.Messages, AlertSeverity.Critical);
        Assert.That(alerts, Is.Not.Empty);

        var killAlert = alerts[0];
        Assert.That(killAlert.UnitId, Is.Not.Null.And.Not.Empty, "harness KILL_CONFIRMED line carries a shooter unit id");

        var stack = new ToastStackModel();
        stack.Add(new ToastEntry(killAlert.SequenceId, killAlert.Severity, killAlert.Category, killAlert.Text, killAlert.UnitId));

        var focusTarget = stack.ResolveFocusTarget(killAlert.SequenceId);
        Assert.That(focusTarget, Is.EqualTo(killAlert.UnitId));

        // Toast click → presentation-only focus (ADR-010: no sim mutation). C2PresentationController
        // already exists as the presentation selection surface; toast click reuses it exactly like any
        // other selection source (OOB tree, map symbol, etc).
        var controller = new C2PresentationController();
        controller.SelectFriendlyUnit(focusTarget!);

        Assert.That(controller.SelectedUnitId, Is.EqualTo(killAlert.UnitId));
    }

    [Test]
    public void Toast_click_falls_back_to_sequence_id_when_entry_has_no_unit_id()
    {
        var stack = new ToastStackModel();
        stack.Add(new ToastEntry(123, AlertSeverity.Notable, "CONTACT", "New contact", FocusUnitId: null));

        Assert.That(stack.ResolveFocusTarget(123), Is.EqualTo("123"));
    }
}
