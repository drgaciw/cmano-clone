namespace ProjectAegis.Delegation.Gameplay;

/// <summary>One observable mission decision or outcome, using simulation time only.</summary>
public sealed record EscortDecisiveMoment(double SimTime, string Title, string Detail);

/// <summary>Read-only player mission view. No field grants write access to authoritative state.</summary>
public sealed record EscortMissionPresentation(
    string Title, string Briefing, string Objective, string Status, string Guidance,
    string Posture, string Radar, double Progress, double RemainingSeconds, int Score,
    bool IsComplete, bool Succeeded, IReadOnlyList<EscortDecisiveMoment> Moments,
    IReadOnlyList<string> Alerts, int RoundsRemaining, string ContactState, string Debrief);

/// <summary>Validated mission command result, suitable for a status label or headless caller.</summary>
public sealed record EscortCommandResult(bool Accepted, string Message);

/// <summary>Mission-specific delegation policy selected by the commander.</summary>
public enum EscortPosture
{
    /// <summary>Only explicit player weapon orders are issued.</summary>
    Manual,
    /// <summary>Retain the cautious defensive magazine reserve.</summary>
    Cautious,
    /// <summary>Retain the balanced defensive magazine reserve.</summary>
    Balanced,
    /// <summary>Permit expenditure of all available rounds on an identified threat.</summary>
    Assertive,
}
