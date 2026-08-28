namespace ProjectAegis.Delegation.TrackCustody;

/// <summary>Whether the operator still holds custody of a contact track (DRG-222).</summary>
public enum TrackCustodyState
{
    Held = 0,
    Dropped = 1,
}

/// <summary>
/// Named drop or custody-break cause. Withheld and dropped rows must never use
/// <see cref="None"/> — operators see why a track died, not a silent hole.
/// </summary>
public enum TrackCustodyCause
{
    None = 0,
    LostSensor = 1,
    Stale = 2,
    CommsDenied = 3,
    ExplicitDrop = 4,
    Unknown = 5,
}

/// <summary>Stable plain-language labels for custody ledger causes (DRG-222).</summary>
public static class TrackCustodyCauseLabels
{
    public const string LostSensor = "lost sensor";
    public const string Stale = "stale";
    public const string CommsDenied = "comms denied";
    public const string ExplicitDrop = "explicit drop";
    public const string Unknown = "unknown";

    public static string Format(TrackCustodyCause cause) =>
        cause switch
        {
            TrackCustodyCause.LostSensor => LostSensor,
            TrackCustodyCause.Stale => Stale,
            TrackCustodyCause.CommsDenied => CommsDenied,
            TrackCustodyCause.ExplicitDrop => ExplicitDrop,
            TrackCustodyCause.Unknown => Unknown,
            _ => string.Empty,
        };
}

/// <summary>Current custody picture row for one contact track.</summary>
public sealed record TrackCustodyRow(
    string ContactId,
    string TargetId,
    string ObserverId,
    TrackCustodyState Custody,
    TrackCustodyCause Cause,
    ulong LastKnownTick,
    double LastKnownSimTime,
    ulong CorrelationSequenceId)
{
    /// <summary>Plain-language cause; empty only when custody is held with no break.</summary>
    public string CauseLabel => TrackCustodyCauseLabels.Format(Cause);
}

/// <summary>One published custody break or drop correlated to order-log sequence.</summary>
public sealed record TrackCustodyLedgerEntry(
    string ContactId,
    string TargetId,
    string ObserverId,
    TrackCustodyState Custody,
    TrackCustodyCause Cause,
    ulong SimTick,
    double SimTime,
    ulong CorrelationSequenceId)
{
    public string CauseLabel => TrackCustodyCauseLabels.Format(Cause);
}

/// <summary>Replay-stable custody + drop-reason ledger snapshot (DRG-222).</summary>
public sealed record TrackCustodySnapshot(
    IReadOnlyList<TrackCustodyRow> Rows,
    IReadOnlyList<TrackCustodyLedgerEntry> Entries)
{
    public static TrackCustodySnapshot Empty { get; } =
        new(Array.Empty<TrackCustodyRow>(), Array.Empty<TrackCustodyLedgerEntry>());
}
