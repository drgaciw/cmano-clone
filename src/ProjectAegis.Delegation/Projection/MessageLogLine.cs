namespace ProjectAegis.Delegation.Projection;

/// <summary>Player-facing message log line projected from order log (no duplicate storage).</summary>
public sealed record MessageLogLine(
    ulong SequenceId,
    double SimTime,
    string Category,
    string Text,
    string? UnitId = null);