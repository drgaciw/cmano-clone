namespace ProjectAegis.Delegation.Projection;

/// <summary>Sim-clock freshness for an active contact track (CMD-29.6).</summary>
public enum ContactProvenanceFreshness
{
    Fresh = 0,
    Stale = 1,
}

/// <summary>Identification confidence derived from sensor lifecycle, not UI selection.</summary>
public enum ContactProvenanceConfidence
{
    Unknown = 0,
    Low = 1,
    Medium = 2,
    High = 3,
}

/// <summary>
/// Named information-quality states for contact provenance (DRG-206).
/// Combinable flags — never leave catalog-miss / stale / silent-comms implicit or empty.
/// </summary>
[Flags]
public enum ContactProvenanceQualityState
{
    None = 0,
    CatalogMiss = 1,
    Stale = 2,
    SilentComms = 4,
}

/// <summary>Detection source: observer platform and resolved target identity.</summary>
public sealed record ContactProvenanceSource(
    string ObserverId,
    string TargetId,
    string SourceRef);

/// <summary>Last-known contact facts at the most recent order-log update.</summary>
public sealed record ContactProvenanceLastKnown(
    string LifecycleState,
    string TargetId,
    ulong LastSimTick,
    double LastSimTime);

/// <summary>
/// DRG-206: headless contact provenance row for Combat UX Slice A.
/// Projection-only — hosts bind these fields; they must not re-derive sim truth (ADR-010).
/// </summary>
public sealed record ContactProvenanceState(
    string ContactId,
    ContactProvenanceSource Source,
    ContactProvenanceConfidence Confidence,
    ContactProvenanceFreshness Freshness,
    ulong AgeTicks,
    ContactProvenanceLastKnown LastKnown,
    bool OutOfCommsUnknown,
    ContactProvenanceQualityState QualityState);

/// <summary>Replay-stable provenance picture for all active contacts.</summary>
public sealed record ContactProvenanceSnapshot(IReadOnlyList<ContactProvenanceState> Contacts)
{
    public static ContactProvenanceSnapshot Empty { get; } =
        new(Array.Empty<ContactProvenanceState>());
}
