namespace ProjectAegis.Delegation.UnityAdapter.Bridge;

using System;
using System.Collections.Generic;
using Orchestration;
using Projection;

/// <summary>
/// CMD-04 / CMD-39 / DRG-398: UI command façade for pause and time compression.
/// Sim clock on <see cref="SimulationSession"/> remains authoritative (ADR-010).
/// </summary>
public static class C2ClockCommand
{
    public static readonly int[] Presets = { 1, 2, 4, 8 };

    public const string ReasonNoSession = "NO_SESSION";
    public const string ReasonResumeBlocked = "RESUME_BLOCKED";
    public const string ReasonReplayReadOnly = "REPLAY_VIEWER_READ_ONLY";

    /// <summary>Presentation label bound by the top bar — never a second clock authority.</summary>
    public static string FormatCompressionLabel(bool isPaused, int factor)
    {
        var clamped = factor < 1 ? 1 : factor;
        if (isPaused)
        {
            // UAT D27: surface the selected rate so -/+ while paused is visible.
            return "TIME: PAUSED (" + clamped + "x)";
        }

        return "TIME: " + clamped + "x";
    }

    public static int NextFaster(int current)
    {
        for (var i = 0; i < Presets.Length; i++)
        {
            if (Presets[i] > current)
            {
                return Presets[i];
            }
        }

        return Presets[Presets.Length - 1];
    }

    public static int NextSlower(int current)
    {
        for (var i = Presets.Length - 1; i >= 0; i--)
        {
            if (Presets[i] < current)
            {
                return Presets[i];
            }
        }

        return Presets[0];
    }

    public static bool TrySetAcceleration(SimulationSession? session, int factor, out string? reason)
    {
        if (session is null)
        {
            reason = ReasonNoSession;
            return false;
        }

        if (session.AttachReplayViewer)
        {
            reason = ReasonReplayReadOnly;
            return false;
        }

        session.SetTimeAccelerationFactor(factor);
        reason = null;
        return true;
    }

    public static bool TryPause(SimulationSession? session, out string? reason)
    {
        if (session is null)
        {
            reason = ReasonNoSession;
            return false;
        }

        if (session.AttachReplayViewer)
        {
            reason = ReasonReplayReadOnly;
            return false;
        }

        session.PauseSim();
        reason = null;
        return true;
    }

    /// <summary>Resume follows <c>WatchAutoPauseGate.CanResume</c> unless <paramref name="explicitOverride"/>.</summary>
    public static bool TryResume(SimulationSession? session, bool explicitOverride, out string? reason) =>
        TryResume(session, coordinator: null, explicitOverride, out reason);

    /// <summary>
    /// Resume gated on both <c>WatchAutoPauseGate</c> and <see cref="MessageLogNotificationCoordinator"/>
    /// unless <paramref name="explicitOverride"/> is true (DRG-408 / DRG-398).
    /// </summary>
    public static bool TryResume(
        SimulationSession? session,
        MessageLogNotificationCoordinator? coordinator,
        bool explicitOverride,
        out string? reason)
    {
        if (session is null)
        {
            reason = ReasonNoSession;
            return false;
        }

        if (session.AttachReplayViewer)
        {
            reason = ReasonReplayReadOnly;
            return false;
        }

        if (!explicitOverride && coordinator is not null && !coordinator.CanResume(explicitOverride: false))
        {
            reason = ReasonResumeBlocked;
            return false;
        }

        if (!session.TryResumeSim(explicitOverride))
        {
            reason = ReasonResumeBlocked;
            return false;
        }

        if (explicitOverride)
        {
            coordinator?.AcknowledgeAllPauses();
        }

        reason = null;
        return true;
    }

    /// <summary>
    /// Evaluates <paramref name="liveLines"/> through the shared DRG-408 coordinator and applies
    /// deterministic <c>Pause &gt; DropToRealTime</c> clock transitions when not in replay mode.
    /// </summary>
    public static NotificationBatchEvaluation EvaluateAndApplyNotificationStop(
        SimulationSession? session,
        MessageLogNotificationCoordinator coordinator,
        IReadOnlyList<MessageLogLine> liveLines,
        bool isReplayViewerAttached,
        out string? reason)
    {
        if (coordinator is null)
        {
            throw new ArgumentNullException(nameof(coordinator));
        }

        var replay = isReplayViewerAttached || (session?.AttachReplayViewer ?? false);
        var eval = coordinator.Evaluate(liveLines, replay);

        if (session is null)
        {
            reason = ReasonNoSession;
            return eval;
        }

        if (eval.ReplaySuppressed || session.AttachReplayViewer)
        {
            reason = ReasonReplayReadOnly;
            return eval;
        }

        if (eval.ShouldDropToRealTime && session.TimeAccelerationFactor != 1)
        {
            TrySetAcceleration(session, 1, out _);
        }

        if (eval.ShouldPause && !session.IsSimPaused)
        {
            TryPause(session, out _);
        }

        reason = null;
        return eval;
    }
}
