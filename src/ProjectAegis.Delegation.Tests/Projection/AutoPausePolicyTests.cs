using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

/// <summary>
/// Track T3 (req 20 §Alerting and Interruption, TR-c2-007, AC-9): a Critical-tier alert requests
/// auto-pause via a pure COMMAND value (ADR-010 — UI never mutates sim directly). See
/// <see cref="AutoPauseCommand"/> remarks for the pause-reason-stack reachability blocker: this policy
/// only proves the COMMAND is produced correctly, not that it is dispatched onto a stack (none exists
/// in this baseline).
/// </summary>
[TestFixture]
public sealed class AutoPausePolicyTests
{
    [Test]
    public void Critical_alert_produces_auto_pause_command()
    {
        var alerts = new[]
        {
            new AlertItem(5, 12.0, "KILL_CONFIRMED", "Hostile destroyed", "u1", AlertSeverity.Critical),
        };

        var command = AutoPausePolicy.Evaluate(alerts, isReplay: false);

        Assert.That(command, Is.Not.Null);
        Assert.That(command!.Reason, Is.EqualTo(AutoPausePolicy.CriticalAlertReason));
        Assert.That(command.TriggerSequenceId, Is.EqualTo(5UL));
        Assert.That(command.SourceUnitId, Is.EqualTo("u1"));
    }

    [Test]
    public void Notable_and_routine_alerts_do_not_request_auto_pause()
    {
        var alerts = new[]
        {
            new AlertItem(1, 1.0, "CONTACT", "New contact", "u1", AlertSeverity.Notable),
            new AlertItem(2, 2.0, "FUEL", "Fuel burn", "u2", AlertSeverity.Routine),
        };

        Assert.That(AutoPausePolicy.Evaluate(alerts, isReplay: false), Is.Null);
    }

    [Test]
    public void Replay_suppresses_auto_pause_even_for_critical_alert()
    {
        var alerts = new[]
        {
            new AlertItem(9, 9.0, "POLICY_DENIAL", "Fire denied", "u9", AlertSeverity.Critical),
        };

        Assert.That(AutoPausePolicy.Evaluate(alerts, isReplay: true), Is.Null);
    }

    [Test]
    public void First_critical_alert_in_batch_wins_when_multiple_present()
    {
        var alerts = new[]
        {
            new AlertItem(1, 1.0, "CONTACT", "New contact", "u1", AlertSeverity.Notable),
            new AlertItem(2, 2.0, "KILL_CONFIRMED", "Hostile destroyed", "u2", AlertSeverity.Critical),
            new AlertItem(3, 3.0, "POLICY_DENIAL", "Fire denied", "u3", AlertSeverity.Critical),
        };

        var command = AutoPausePolicy.Evaluate(alerts, isReplay: false);

        Assert.That(command, Is.Not.Null);
        Assert.That(command!.TriggerSequenceId, Is.EqualTo(2UL));
    }

    [Test]
    public void Empty_batch_produces_no_command()
    {
        Assert.That(AutoPausePolicy.Evaluate(System.Array.Empty<AlertItem>(), isReplay: false), Is.Null);
    }
}
