namespace ProjectAegis.Delegation.UnityAdapter.Bridge;

using ProjectAegis.Delegation.Orchestration;

/// <summary>
/// CMD-04 / CMD-39 Track A: UI command façade for pause and time compression.
/// Sim clock on <see cref="SimulationSession"/> remains authoritative (ADR-010).
/// </summary>
public static class C2ClockCommand
{
    public static readonly int[] Presets = { 1, 2, 4, 8 };

    public const string ReasonNoSession = "NO_SESSION";
    public const string ReasonResumeBlocked = "RESUME_BLOCKED";

    /// <summary>Presentation label bound by the top bar — never a second clock authority.</summary>
    public static string FormatCompressionLabel(bool isPaused, int factor)
    {
        if (isPaused)
        {
            return "TIME: PAUSED";
        }

        var clamped = factor < 1 ? 1 : factor;
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

        session.PauseSim();
        reason = null;
        return true;
    }

    /// <summary>Resume follows <c>WatchAutoPauseGate.CanResume</c> unless <paramref name="explicitOverride"/>.</summary>
    public static bool TryResume(SimulationSession? session, bool explicitOverride, out string? reason)
    {
        if (session is null)
        {
            reason = ReasonNoSession;
            return false;
        }

        if (!session.TryResumeSim(explicitOverride))
        {
            reason = ReasonResumeBlocked;
            return false;
        }

        reason = null;
        return true;
    }
}
