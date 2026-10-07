namespace ProjectAegis.Delegation.EngagementExplanation;

/// <summary>Why-state of one engagement leg as reported by the combat-event contract (DRG-168).</summary>
public enum EngagementExplanationStatus
{
    /// <summary>Intent accepted, but no authorization, refusal, or execution has been reported.</summary>
    Unknown = 0,

    /// <summary>Authorized and not yet fired.</summary>
    Available = 1,

    /// <summary>Selected and executed: firing, in flight, or terminal.</summary>
    Selected = 2,

    /// <summary>Explicit authorization refusal.</summary>
    Refused = 3,
}

/// <summary>Constraint family shown on the explanation surface.</summary>
public enum EngagementConstraintKind
{
    /// <summary>Physical / track / weapon facts that no order can override.</summary>
    Hard = 1,

    /// <summary>ROE, weapons release, targeting authority, and doctrine policy.</summary>
    Doctrine = 2,
}

/// <summary>Reported state of one constraint. Missing evidence stays <see cref="Unknown"/>.</summary>
public enum EngagementConstraintState
{
    Unknown = 0,
    Satisfied = 1,
    Violated = 2,
}

/// <summary>Stable constraint row names for host binding.</summary>
public static class EngagementConstraintNames
{
    public const string Track = "Track";
    public const string FiringSolution = "FiringSolution";
    public const string Engagement = "Engagement";
    public const string Roe = "Roe";
    public const string TargetingAuthority = "TargetingAuthority";
    public const string Policy = "Policy";
}

/// <summary>One hard or doctrine constraint row. <see cref="Code"/> is the stable reason code when known.</summary>
public sealed record EngagementConstraint(
    EngagementConstraintKind Kind,
    string Name,
    EngagementConstraintState State,
    string? Code,
    string Text);

/// <summary>
/// DRG-168: presentation model explaining why an engagement is available, selected, or refused.
/// Built only from the combat-event snapshot and the engagement explanation contract (ADR-010 §2–3).
/// </summary>
public sealed record EngagementExplanationSurface(
    string ShooterId,
    string TargetId,
    ulong CorrelationId,
    double SimTime,
    EngagementExplanationStatus Status,
    string Headline,
    IReadOnlyList<EngagementConstraint> HardConstraints,
    IReadOnlyList<EngagementConstraint> DoctrineConstraints,
    string ContactConfidence,
    string Weapon,
    string FiringSolution,
    string? ActionableReason,
    string ExplanationRef)
{
    /// <summary>No matching engagement leg.</summary>
    public static EngagementExplanationSurface Empty { get; } = new(
        string.Empty,
        string.Empty,
        0,
        0,
        EngagementExplanationStatus.Unknown,
        "Select an engagement to inspect.",
        Array.Empty<EngagementConstraint>(),
        Array.Empty<EngagementConstraint>(),
        EngagementExplanationProjection.Unknown,
        EngagementExplanationProjection.Unknown,
        EngagementExplanationProjection.Unknown,
        null,
        string.Empty);
}
