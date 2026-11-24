using NUnit.Framework;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using ProjectAegis.Delegation.UnityAdapter.Gameplay;
using ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Bridge;

[TestFixture]
public sealed class TimeCompressionSafeStopTests
{
    private sealed record ParityOutcome(
        int DropStep,
        int PauseStep,
        int TotalSteps,
        int FinalFactor,
        bool FinalPaused,
        string Fingerprint,
        IReadOnlyList<ulong> DropSeqIds,
        IReadOnlyList<string> DropReasons,
        IReadOnlyList<ulong> PauseSeqIds,
        IReadOnlyList<string> PauseReasons);

    [Test]
    public void Parity_1x_vs_2x_4x_8x_produces_identical_SimTick_DecisionLog_fingerprint_and_stop_reasons()
    {
        var baseline = RunParityAtFactor(1);
        foreach (var factor in new[] { 2, 4, 8 })
        {
            var actual = RunParityAtFactor(factor);
            Assert.That(actual.DropStep, Is.EqualTo(baseline.DropStep), $"DropStep mismatch at {factor}x");
            Assert.That(actual.PauseStep, Is.EqualTo(baseline.PauseStep), $"PauseStep mismatch at {factor}x");
            Assert.That(actual.TotalSteps, Is.EqualTo(baseline.TotalSteps), $"TotalSteps mismatch at {factor}x");
            Assert.That(actual.FinalFactor, Is.EqualTo(1), $"FinalFactor mismatch at {factor}x");
            Assert.That(actual.FinalPaused, Is.True, $"FinalPaused mismatch at {factor}x");
            Assert.That(actual.Fingerprint, Is.EqualTo(baseline.Fingerprint), $"Fingerprint mismatch at {factor}x");
            Assert.That(actual.DropSeqIds, Is.EqualTo(baseline.DropSeqIds), $"DropSeqIds mismatch at {factor}x");
            Assert.That(actual.DropReasons, Is.EqualTo(baseline.DropReasons), $"DropReasons mismatch at {factor}x");
            Assert.That(actual.PauseSeqIds, Is.EqualTo(baseline.PauseSeqIds), $"PauseSeqIds mismatch at {factor}x");
            Assert.That(actual.PauseReasons, Is.EqualTo(baseline.PauseReasons), $"PauseReasons mismatch at {factor}x");
        }
    }

    [Test]
    public void Coalesced_safe_stop_batch_retains_all_event_SequenceIds_and_reasons()
    {
        var bridge = new DelegationBridge(42, mvpEngagement: true);
        var session = bridge.Session!;
        Assert.That(C2ClockCommand.TrySetAcceleration(session, 8, out _), Is.True);

        var log = bridge.Orchestrator.DecisionLog;
        log.AppendEngagement(new EngagementRecord(
            0, 10.0, 10, new TargetId("u1"), 1, true, null, new TargetId("hostile-1"), "NSM"));
        log.AppendPlatformDamageChange(new PlatformDamageChangeRecord(
            0, 10.0, 10, new TargetId("u1"), 100.0, 75.0, "Hit", 1));

        var coordinator = new MessageLogNotificationCoordinator();
        var eval = C2ClockCommand.EvaluateAndApplyNotificationStop(
            session,
            coordinator,
            MessageLogBridge.ProjectLive(bridge),
            isReplayViewerAttached: false,
            out var reason);

        Assert.That(reason, Is.Null);
        Assert.That(eval.ClockAction, Is.EqualTo(ClockStopAction.Pause));
        Assert.That(eval.ShouldPause, Is.True);
        Assert.That(eval.ShouldDropToRealTime, Is.True);
        Assert.That(eval.TriggeredSequenceIds, Has.Count.EqualTo(2));
        Assert.That(eval.Reasons, Has.Count.EqualTo(2));
        Assert.That(session.TimeAccelerationFactor, Is.EqualTo(1));
        Assert.That(session.IsSimPaused, Is.True);
    }

    [Test]
    public void AttachReplayViewer_refuses_C2ClockCommand_TrySetAcceleration_TryPause_TryResume_and_ApplyNotificationStop()
    {
        var bridge = new DelegationBridge(42, mvpEngagement: true);
        var session = bridge.Session!;
        bridge.AttachReplayViewer = true;
        var factorBefore = session.TimeAccelerationFactor;
        var pausedBefore = session.IsSimPaused;

        Assert.That(C2ClockCommand.TrySetAcceleration(session, 4, out var accelReason), Is.False);
        Assert.That(accelReason, Is.EqualTo(C2ClockCommand.ReasonReplayReadOnly));
        Assert.That(session.TimeAccelerationFactor, Is.EqualTo(factorBefore));

        Assert.That(C2ClockCommand.TryPause(session, out var pauseReason), Is.False);
        Assert.That(pauseReason, Is.EqualTo(C2ClockCommand.ReasonReplayReadOnly));
        Assert.That(session.IsSimPaused, Is.EqualTo(pausedBefore));

        Assert.That(C2ClockCommand.TryResume(session, explicitOverride: true, out var resumeReason), Is.False);
        Assert.That(resumeReason, Is.EqualTo(C2ClockCommand.ReasonReplayReadOnly));

        bridge.Orchestrator.DecisionLog.AppendPlatformDamageChange(new PlatformDamageChangeRecord(
            0, 5.0, 5, new TargetId("u1"), 100.0, 60.0, "Hit", 2));
        var coordinator = new MessageLogNotificationCoordinator();
        var eval = C2ClockCommand.EvaluateAndApplyNotificationStop(
            session,
            coordinator,
            MessageLogBridge.ProjectLive(bridge),
            isReplayViewerAttached: true,
            out var stopReason);

        Assert.That(eval.ReplaySuppressed, Is.True);
        Assert.That(eval.ClockAction, Is.EqualTo(ClockStopAction.None));
        Assert.That(stopReason, Is.EqualTo(C2ClockCommand.ReasonReplayReadOnly));
        Assert.That(session.TimeAccelerationFactor, Is.EqualTo(factorBefore));
        Assert.That(session.IsSimPaused, Is.EqualTo(pausedBefore));
        Assert.That(coordinator.HasUnresolvedPause, Is.False);
    }

    [Test]
    public void C2ClockCommand_TryResume_blocked_by_unresolved_coordinator_pause_unless_explicit_override()
    {
        var bridge = new DelegationBridge(42, mvpEngagement: true);
        var session = bridge.Session!;
        var coordinator = new MessageLogNotificationCoordinator();
        bridge.Orchestrator.DecisionLog.AppendPlatformDamageChange(new PlatformDamageChangeRecord(
            0, 5.0, 5, new TargetId("u1"), 100.0, 60.0, "Hit", 2));

        var eval = C2ClockCommand.EvaluateAndApplyNotificationStop(
            session,
            coordinator,
            MessageLogBridge.ProjectLive(bridge),
            isReplayViewerAttached: false,
            out _);

        Assert.That(eval.ShouldPause, Is.True);
        Assert.That(session.IsSimPaused, Is.True);

        Assert.That(C2ClockCommand.TryResume(session, coordinator, explicitOverride: false, out var blocked), Is.False);
        Assert.That(blocked, Is.EqualTo(C2ClockCommand.ReasonResumeBlocked));
        Assert.That(session.IsSimPaused, Is.True);

        Assert.That(C2ClockCommand.TryResume(session, coordinator, explicitOverride: true, out var ok), Is.True);
        Assert.That(ok, Is.Null);
        Assert.That(session.IsSimPaused, Is.False);
        Assert.That(coordinator.HasUnresolvedPause, Is.False);
    }

    [Test]
    public void Source_fence_SimulationSession_cs_has_no_safe_stop_policy_couplings()
    {
        var source = UiIaSourceReader.ReadUnder(
            "src", "ProjectAegis.Delegation", "Orchestration", "SimulationSession.cs");

        Assert.That(source, Does.Not.Contain("MessageCategoryPolicy"));
        Assert.That(source, Does.Not.Contain("MessageLogNotificationCoordinator"));
        Assert.That(source, Does.Not.Contain("C2ClockCommand"));
    }

    private static ParityOutcome RunParityAtFactor(int initialFactor)
    {
        var bridge = new DelegationBridge(42, mvpEngagement: true);
        var session = bridge.Session!;
        session.ExternallyScheduledSteps = true;
        bridge.Registry.RegisterUnit(new EntityKey(1), "u1");
        bridge.Registry.RegisterUnit(new EntityKey(2), "hostile-1");
        bridge.BeginExecution();

        Assert.That(C2ClockCommand.TrySetAcceleration(session, initialFactor, out _), Is.True);

        var coordinator = new MessageLogNotificationCoordinator();
        var runner = new FixedStepFrameRunner(fixedDeltaSeconds: 1.0, maxStepsPerFrame: 64);
        var step = 0;
        var dropStep = -1;
        var pauseStep = -1;
        IReadOnlyList<ulong> dropSeqIds = Array.Empty<ulong>();
        IReadOnlyList<string> dropReasons = Array.Empty<string>();
        IReadOnlyList<ulong> pauseSeqIds = Array.Empty<ulong>();
        IReadOnlyList<string> pauseReasons = Array.Empty<string>();

        for (var frame = 0; frame < 30 && !session.IsSimPaused; frame++)
        {
            runner.Advance(
                1.0,
                () => session.TimeAccelerationFactor,
                () => !session.IsSimPaused,
                () =>
                {
                    step++;
                    var log = bridge.Orchestrator.DecisionLog;
                    if (step == 5)
                    {
                        log.AppendEngagement(new EngagementRecord(
                            0, step, (ulong)step, new TargetId("u1"), 1, true, null, new TargetId("hostile-1"), "NSM"));
                    }
                    else if (step == 12)
                    {
                        log.AppendPlatformDamageChange(new PlatformDamageChangeRecord(
                            0, step, (ulong)step, new TargetId("hostile-1"), 100, 70, "Hit", 1));
                    }

                    var eval = C2ClockCommand.EvaluateAndApplyNotificationStop(
                        session,
                        coordinator,
                        MessageLogBridge.ProjectLive(bridge),
                        isReplayViewerAttached: false,
                        out _);

                    if (eval.ClockAction == ClockStopAction.DropToRealTime && dropStep < 0)
                    {
                        dropStep = step;
                        dropSeqIds = eval.TriggeredSequenceIds;
                        dropReasons = eval.Reasons;
                    }
                    else if (eval.ClockAction == ClockStopAction.Pause && pauseStep < 0)
                    {
                        pauseStep = step;
                        pauseSeqIds = eval.TriggeredSequenceIds;
                        pauseReasons = eval.Reasons;
                    }
                });
        }

        return new ParityOutcome(
            dropStep,
            pauseStep,
            step,
            session.TimeAccelerationFactor,
            session.IsSimPaused,
            bridge.Orchestrator.DecisionLog.ComputeFingerprint(),
            dropSeqIds,
            dropReasons,
            pauseSeqIds,
            pauseReasons);
    }
}
