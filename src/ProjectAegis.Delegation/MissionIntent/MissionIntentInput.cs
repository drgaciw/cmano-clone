namespace ProjectAegis.Delegation.MissionIntent;

/// <summary>Read-only facts for headless mission-command intent projection (injected input).</summary>
public sealed record MissionIntentInput(
    string GroupId,
    string UnitId,
    string IntentCode,
    IReadOnlyList<string> ActiveConstraints,
    MissionIntentRetaskAdvice AdvisoryRetask = MissionIntentRetaskAdvice.None);
