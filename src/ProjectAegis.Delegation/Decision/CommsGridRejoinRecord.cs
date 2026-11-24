namespace ProjectAegis.Delegation.Decision;

/// <summary>One informative contact-picture merge applied on comms-grid rejoin (DRG-391 / C3-02).</summary>
public sealed record CommsGridRejoinMergeDelta(
    string TargetId,
    string ContactId,
    string PreviousState,
    string NewState);

/// <summary>
/// Order-log summary emitted when an off-grid unit rejoins the comms grid and contributes
/// one or more informative contact-picture merges (DRG-391 / C3-02).
/// </summary>
public sealed record CommsGridRejoinRecord(
    ulong SequenceId,
    double SimTime,
    ulong SimTick,
    string UnitId,
    ulong OffGridSinceTick,
    ulong RejoinTick,
    IReadOnlyList<CommsGridRejoinMergeDelta> InformativeMerges,
    int SuppressedCount);
