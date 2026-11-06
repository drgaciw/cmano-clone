namespace ProjectAegis.Delegation.Skills;

using ProjectAegis.Sim.Policy;

/// <summary>
/// DRG-209 / DRG-182: combat authority verbs projected for Slice A headless UX.
/// Each action carries an explicit disposition — never an implicit silent deny.
/// </summary>
public enum C2AuthorityActionKind
{
    Observe = 0,
    Recommend = 1,
    Approve = 2,
    Engage = 3,
    Abort = 4,
    Retask = 5,
}

/// <summary>
/// Explicit authority outcome for an action or targeting leg.
/// </summary>
public enum C2AuthorityDisposition
{
    Permitted = 0,
    Withheld = 1,
    ApprovalRequired = 2,
}

/// <summary>Per-verb authority state for DRG-182 chrome.</summary>
public readonly record struct C2AuthorityActionState(
    C2AuthorityActionKind Action,
    C2AuthorityDisposition Disposition,
    string? ReasonCode);

/// <summary>Targeting leg: permitted fire, explicit withhold, or approval gate.</summary>
public sealed record C2TargetingAuthority(
    C2AuthorityDisposition Disposition,
    string? ReasonCode,
    RequiredApproval? PendingApproval);

/// <summary>
/// Headless authority projection for an actor at a skill boundary.
/// Consumes the DRG-196 skill contract; does not enqueue orders.
/// </summary>
public sealed record C2AuthorityProjection(
    RoeProjection Roe,
    C2TargetingAuthority Targeting,
    IReadOnlyList<C2AuthorityActionState> Actions);

/// <summary>Inputs for <see cref="C2AuthorityProjector.Project"/>.</summary>
public sealed record C2AuthorityProjectionContext(
    RoeLevel Roe,
    SkillLane Lane,
    RequiredApproval RequiredApproval,
    TrackSource TrackSource,
    bool FireControlSatisfied,
    string? CommandId = null,
    bool HumanControlled = true)
{
    /// <summary>Build from an envelope authority basis plus resolved ROE.</summary>
    public static C2AuthorityProjectionContext FromEnvelope(
        AuthorityBasis basis,
        SkillLane lane,
        RequiredApproval requiredApproval,
        string? commandId = null,
        RoeLevel? roeOverride = null) =>
        new(
            roeOverride ?? C2AuthorityProjector.ParseRoeLabel(basis.Roe),
            lane,
            requiredApproval,
            basis.TrackSource,
            basis.FireControlSatisfied,
            commandId,
            HumanControlled: true);
}
