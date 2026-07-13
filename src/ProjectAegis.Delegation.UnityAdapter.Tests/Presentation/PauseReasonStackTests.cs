using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Presentation;
using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

/// <summary>Phase 2b (req 20 rev 2 §Alerting, TR-c2-007 / AC-9): the presentation-layer
/// <see cref="PauseReasonStack"/> that actuates auto-pause as a session/UI tick gate — never a sim
/// mutation.</summary>
[TestFixture]
public sealed class PauseReasonStackTests
{
    [Test]
    public void New_stack_is_not_paused()
    {
        var stack = new PauseReasonStack();
        Assert.That(stack.IsPaused, Is.False);
        Assert.That(stack.Reasons, Is.Empty);
    }

    [Test]
    public void Push_pauses_and_is_idempotent_per_reason()
    {
        var stack = new PauseReasonStack();

        Assert.That(stack.Push("c2.alert.critical"), Is.True);
        Assert.That(stack.IsPaused, Is.True);
        Assert.That(stack.Push("c2.alert.critical"), Is.False, "same reason does not stack twice");
        Assert.That(stack.Reasons, Has.Count.EqualTo(1));
    }

    [Test]
    public void Sim_stays_paused_until_every_reason_is_removed()
    {
        var stack = new PauseReasonStack();
        stack.Push("c2.alert.critical");
        stack.Push("manual");

        stack.Remove("c2.alert.critical");
        Assert.That(stack.IsPaused, Is.True, "manual reason still holds the pause");

        stack.Remove("manual");
        Assert.That(stack.IsPaused, Is.False);
    }

    [Test]
    public void ApplyAutoPause_pushes_the_command_reason()
    {
        var stack = new PauseReasonStack();
        var command = new AutoPauseCommand(AutoPausePolicy.CriticalAlertReason, TriggerSequenceId: 7, SourceUnitId: "u1");

        Assert.That(stack.ApplyAutoPause(command), Is.True);
        Assert.That(stack.Contains(AutoPausePolicy.CriticalAlertReason), Is.True);
    }

    [Test]
    public void ApplyAutoPause_null_command_is_a_noop()
    {
        // AutoPausePolicy.Evaluate returns null in replay or when no Critical alert is present,
        // so a null command must never pause the sim.
        var stack = new PauseReasonStack();
        Assert.That(stack.ApplyAutoPause(null), Is.False);
        Assert.That(stack.IsPaused, Is.False);
    }

    [Test]
    public void Replay_produces_no_pause_end_to_end()
    {
        // AutoPausePolicy suppresses the command in replay → ApplyAutoPause is a no-op → sim never pauses.
        var alerts = new[]
        {
            new AlertItem(SequenceId: 1, SimTime: 1.0, Category: "KILL_CONFIRMED", Text: "unit lost", UnitId: "u1", Severity: AlertSeverity.Critical),
        };
        var stack = new PauseReasonStack();

        stack.ApplyAutoPause(AutoPausePolicy.Evaluate(alerts, isReplay: true));
        Assert.That(stack.IsPaused, Is.False);

        stack.ApplyAutoPause(AutoPausePolicy.Evaluate(alerts, isReplay: false));
        Assert.That(stack.IsPaused, Is.True, "outside replay the same Critical alert does pause");
    }
}
