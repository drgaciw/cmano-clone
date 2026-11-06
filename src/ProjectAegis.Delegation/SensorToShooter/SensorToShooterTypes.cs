namespace ProjectAegis.Delegation.SensorToShooter;

using ProjectAegis.Sim.Scenario;

/// <summary>Inspectable link kinds in the sensor → shooter chain (DRG-207).</summary>
public enum SensorToShooterLinkKind
{
    Sensor = 1,
    Track = 2,
    Targetability = 3,
    EligibleShooter = 4,
}

/// <summary>Explicit break causes for broken or stale chain links.</summary>
public enum SensorToShooterBreakCause
{
    None = 0,
    LostSensor = 1,
    StaleTrack = 2,
    NoFireControl = 3,
    NoEligibleShooter = 4,
    DegradedTrack = 5,
}

/// <summary>Sim-authored shooter candidacy for a target. Not UI selection (ADR-010).</summary>
public interface ISensorToShooterShooterSource
{
    /// <summary>Returns engage facts for candidate shooters on <paramref name="targetId"/>.</summary>
    IReadOnlyList<SensorToShooterShooterCandidate> GetCandidatesForTarget(string targetId);
}

/// <summary>One shooter candidate with scenario engage defaults and magazine rounds.</summary>
public sealed record SensorToShooterShooterCandidate(
    string ShooterUnitId,
    ScenarioEngageDefaults EngageDefaults,
    int RoundsRemaining);

/// <summary>One link in the sensor-to-shooter chain.</summary>
public sealed record SensorToShooterChainLink(
    SensorToShooterLinkKind Kind,
    bool IsLinked,
    SensorToShooterBreakCause BreakCause,
    string? UnitId,
    string ContactId,
    string TargetId,
    string? Detail)
{
    /// <summary>Plain-language break cause for broken links; empty when linked.</summary>
    public string CauseLabel => SensorToShooterBreakCauseLabels.Format(BreakCause);
}

/// <summary>Replay-stable sensor → track → targetability → shooter chain for one contact.</summary>
public sealed record SensorToShooterChain(
    string ContactId,
    string TargetId,
    string ObserverId,
    bool IsComplete,
    SensorToShooterBreakCause PrimaryBreakCause,
    IReadOnlyList<SensorToShooterChainLink> Links)
{
    public string PrimaryCauseLabel => SensorToShooterBreakCauseLabels.Format(PrimaryBreakCause);
}

/// <summary>Headless projection snapshot for Combat UX Slice A (DRG-207).</summary>
public sealed record SensorToShooterSnapshot(IReadOnlyList<SensorToShooterChain> Chains)
{
    public static SensorToShooterSnapshot Empty { get; } =
        new(Array.Empty<SensorToShooterChain>());
}

/// <summary>Stable display labels for break causes.</summary>
public static class SensorToShooterBreakCauseLabels
{
    public const string LostSensor = "lost sensor";
    public const string StaleTrack = "stale track";
    public const string NoFireControl = "no FC";
    public const string NoEligibleShooter = "no eligible shooter";
    public const string DegradedTrack = "degraded track";

    public static string Format(SensorToShooterBreakCause cause) =>
        cause switch
        {
            SensorToShooterBreakCause.LostSensor => LostSensor,
            SensorToShooterBreakCause.StaleTrack => StaleTrack,
            SensorToShooterBreakCause.NoFireControl => NoFireControl,
            SensorToShooterBreakCause.NoEligibleShooter => NoEligibleShooter,
            SensorToShooterBreakCause.DegradedTrack => DegradedTrack,
            _ => string.Empty,
        };
}
