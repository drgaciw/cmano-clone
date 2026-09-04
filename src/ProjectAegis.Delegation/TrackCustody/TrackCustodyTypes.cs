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

/// <summary>
/// AEGIS-305 (DRG-235): Explicit attribution breakdown for why track custody was lost or degraded.
/// </summary>
public enum TrackCustodyBreakdown
{
    None = 0,
    JammingDegraded = 1,
    LineOfSightLoss = 2,
    PlatformDestroyed = 3,

    // Aliases for domain specifications
    JammingSjThreshold = 1,
    HorizonMasking = 2,
}

/// <summary>
/// Alias enum for <see cref="TrackCustodyBreakdown"/> (AEGIS-305 / DRG-235).
/// </summary>
public enum TrackCustodyLossReason
{
    None = 0,
    JammingDegraded = 1,
    LineOfSightLoss = 2,
    PlatformDestroyed = 3,

    // Aliases for domain specifications
    JammingSjThreshold = 1,
    HorizonMasking = 2,
}

/// <summary>Stable plain-language labels for track custody breakdown attribution (DRG-235).</summary>
public static class TrackCustodyBreakdownLabels
{
    public const string None = "";
    public const string JammingDegraded = "jamming";
    public const string LineOfSightLoss = "line of sight loss";
    public const string PlatformDestroyed = "platform destroyed";
    public const string JammingSjThreshold = "jamming (S/J threshold)";
    public const string HorizonMasking = "horizon masking";

    public static string Format(TrackCustodyBreakdown breakdown) =>
        breakdown switch
        {
            TrackCustodyBreakdown.JammingDegraded => JammingDegraded,
            TrackCustodyBreakdown.LineOfSightLoss => LineOfSightLoss,
            TrackCustodyBreakdown.PlatformDestroyed => PlatformDestroyed,
            _ => string.Empty,
        };

    public static string Format(TrackCustodyLossReason reason) =>
        Format((TrackCustodyBreakdown)reason);
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
    ulong CorrelationSequenceId,
    TrackCustodyBreakdown Breakdown = TrackCustodyBreakdown.None)
{
    /// <summary>Plain-language cause; empty only when custody is held with no break.</summary>
    public string CauseLabel => TrackCustodyCauseLabels.Format(Cause);

    public TrackCustodyBreakdown TrackCustodyBreakdown => Breakdown;
    public TrackCustodyLossReason LossReason => (TrackCustodyLossReason)Breakdown;
    public TrackCustodyLossReason TrackCustodyLossReason => (TrackCustodyLossReason)Breakdown;
    public string BreakdownLabel => TrackCustodyBreakdownLabels.Format(Breakdown);
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
    ulong CorrelationSequenceId,
    TrackCustodyBreakdown Breakdown = TrackCustodyBreakdown.None)
{
    public string CauseLabel => TrackCustodyCauseLabels.Format(Cause);
    public TrackCustodyBreakdown TrackCustodyBreakdown => Breakdown;
    public TrackCustodyLossReason LossReason => (TrackCustodyLossReason)Breakdown;
    public TrackCustodyLossReason TrackCustodyLossReason => (TrackCustodyLossReason)Breakdown;
    public string BreakdownLabel => TrackCustodyBreakdownLabels.Format(Breakdown);
}

/// <summary>Replay-stable custody + drop-reason ledger snapshot (DRG-222).</summary>
public sealed record TrackCustodySnapshot(
    IReadOnlyList<TrackCustodyRow> Rows,
    IReadOnlyList<TrackCustodyLedgerEntry> Entries)
{
    public static TrackCustodySnapshot Empty { get; } =
        new(Array.Empty<TrackCustodyRow>(), Array.Empty<TrackCustodyLedgerEntry>());
}
