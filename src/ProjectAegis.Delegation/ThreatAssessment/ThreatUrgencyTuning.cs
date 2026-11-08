namespace ProjectAegis.Delegation.ThreatAssessment;

/// <summary>Immutable scenario-authorable advisory scoring parameters, separate from simulation authority.</summary>
public sealed record ThreatUrgencyTuning(
    double InZoneBase,
    double ApproachingBase,
    double OutOfZoneBase,
    double DefensiveBonus,
    double HypersonicBonus,
    double MissingFireControlPenalty,
    double ImmediateFloor,
    double FocusFloor,
    double WatchFloor,
    double LowFloor,
    double LowConfidenceFloor)
{
    /// <summary>Compatibility fallback for callers that do not opt into authored advisory tuning.</summary>
    public static ThreatUrgencyTuning Default { get; } = new(
        InZoneBase: 0.80, ApproachingBase: 0.55, OutOfZoneBase: 0.25,
        DefensiveBonus: 0.15, HypersonicBonus: 0.10, MissingFireControlPenalty: 0.10,
        ImmediateFloor: 0.85, FocusFloor: 0.70, WatchFloor: 0.45, LowFloor: 0.20,
        LowConfidenceFloor: 0.5);
}
