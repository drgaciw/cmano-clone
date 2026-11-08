namespace ProjectAegis.Delegation.Projection;

using Comms;
using Core;

/// <summary>
/// Where a contact's information came from (DRG-271).
/// <see cref="Unknown"/> is a named gap, not an organic sensor and not a datalink track.
/// </summary>
public enum ContactInformationOrigin
{
    Unknown = 0,
    Organic = 1,
    Datalink = 2,
}

/// <summary>
/// Positional claim. Identity confidence never selects this value and never supplies a radius.
/// <see cref="Unknown"/> has no coordinates; a measured fix at the origin is a different claim.
/// </summary>
public enum ContactPositionClaimKind
{
    /// <summary>No position fact. Distinct from a coordinate of zero.</summary>
    Unknown = 0,

    /// <summary>Caller geometry that is not the DRG-417 production feed. Always labeled.</summary>
    FixtureOnly = 1,

    /// <summary>Last reported fix from a production feed. Never synthesized during comms loss.</summary>
    LastReportedHeld = 2,
}

/// <summary>Headless position claim. No uncertainty ellipse is stored or derived.</summary>
public sealed record ContactPositionClaim(
    ContactPositionClaimKind Kind,
    double? EastMeters,
    double? NorthMeters,
    ulong? ReportedSimTick,
    string? FixtureLabel,
    bool IsFreshFix)
{
    /// <summary>No coordinates, no label, not a fresh fix.</summary>
    public static ContactPositionClaim Unknown { get; } =
        new(ContactPositionClaimKind.Unknown, null, null, null, null, false);
}

/// <summary>Caller-supplied position sample. Catalog platform rows are not samples.</summary>
public sealed record ContactPositionSample(
    string ContactId,
    ulong ReportedSimTick,
    double? EastMeters,
    double? NorthMeters,
    bool IsProductionFeed,
    string? FixtureLabel = null);

/// <summary>Explicit information origin. Omitted contacts stay <see cref="ContactInformationOrigin.Unknown"/>.</summary>
public sealed record ContactOriginFact(
    string ContactId,
    ContactInformationOrigin Origin);

/// <summary>
/// Unit command disposition for a queued-order fact (DRG-271).
/// Only <see cref="Queued"/> is a genuinely pending command.
/// </summary>
public enum UnitOrderDisposition
{
    None = 0,
    Queued = 1,
    Delayed = 2,
    Rejected = 3,
    Expired = 4,
    Executed = 5,
}

/// <summary>
/// Terminal order fact the order log does not carry as its own row.
/// Expiry has no log row; rejected and executed are also read from policy denials and launched engagements.
/// A <see cref="UnitOrderDisposition.Queued"/> fact is ignored — pending requires a player order.
/// </summary>
public sealed record UnitOrderTerminalFact(
    string UnitId,
    ulong SimTick,
    UnitOrderDisposition Disposition,
    OrderKind? Kind = null);

/// <summary>One unit's current command disposition. Not a contact badge and not UI selection.</summary>
public sealed record UnitOrderQueueState(
    string UnitId,
    UnitOrderDisposition Disposition,
    OrderKind? Kind,
    ulong? IssuedSimTick,
    ulong? ExecuteSimTick,
    bool IsGenuinelyPending);

/// <summary>
/// Per-contact information-quality cue (DRG-271). Projection-only.
/// Confidence, freshness, comms, and position are independent fields.
/// </summary>
public sealed record ContactCueState(
    string ContactId,
    ContactProvenanceConfidence Confidence,
    ContactProvenanceFreshness Freshness,
    ulong? SimAgeTicks,
    double? AgeSimTime,
    CommsState Comms,
    bool OutOfCommsUnknown,
    ContactProvenanceQualityState QualityState,
    ContactInformationOrigin Origin,
    bool IsDatalinkSourced,
    bool IsStale,
    ContactPositionClaim Position);

/// <summary>Replay-stable cue picture: contact rows plus per-unit order disposition.</summary>
public sealed record ContactCueSnapshot(
    IReadOnlyList<ContactCueState> Contacts,
    IReadOnlyList<UnitOrderQueueState> UnitOrders)
{
    public static ContactCueSnapshot Empty { get; } =
        new(Array.Empty<ContactCueState>(), Array.Empty<UnitOrderQueueState>());
}

/// <summary>
/// Pure selection result. Holds only the requested contact and unit.
/// A later <see cref="ContactCueProjection.Select"/> call does not read this value.
/// </summary>
public readonly record struct ContactCueSelection(
    string? ContactId,
    string? UnitId,
    ContactCueState? Contact,
    UnitOrderQueueState? UnitOrder);
