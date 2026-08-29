namespace ProjectAegis.Delegation.IdentityClass;

/// <summary>Headless identity posture for a contact track (DRG-225).</summary>
public enum IdentityClassification
{
    Unknown = 0,
    Classified = 1,
    Tentative = 2,
}

/// <summary>Confidence band for identity classification. Sim-derived, not UI selection.</summary>
public enum IdentityConfidenceBand
{
    Unknown = 0,
    Low = 1,
    Medium = 2,
    High = 3,
}

/// <summary>Stable named reason codes for identity ledger rows. Never silent.</summary>
public static class IdentityClassReasonCodes
{
    public const string LifecycleUnknown = "LifecycleUnknown";
    public const string LifecycleDetected = "LifecycleDetected";
    public const string LifecycleClassified = "LifecycleClassified";
    public const string LifecycleIdentified = "LifecycleIdentified";
    public const string CommsGap = "CommsGap";
    public const string CatalogMiss = "CatalogMiss";
    public const string StaleTrack = "StaleTrack";
}

/// <summary>Plain-language labels for identity classification reasons (DRG-225).</summary>
public static class IdentityClassReasonLabels
{
    public const string LifecycleUnknown = "lifecycle unknown";
    public const string LifecycleDetected = "lifecycle detected";
    public const string LifecycleClassified = "lifecycle classified";
    public const string LifecycleIdentified = "lifecycle identified";
    public const string CommsGap = "comms gap";
    public const string CatalogMiss = "catalog miss";
    public const string StaleTrack = "stale track";

    public static string Format(string reasonCode) =>
        reasonCode switch
        {
            IdentityClassReasonCodes.LifecycleUnknown => LifecycleUnknown,
            IdentityClassReasonCodes.LifecycleDetected => LifecycleDetected,
            IdentityClassReasonCodes.LifecycleClassified => LifecycleClassified,
            IdentityClassReasonCodes.LifecycleIdentified => LifecycleIdentified,
            IdentityClassReasonCodes.CommsGap => CommsGap,
            IdentityClassReasonCodes.CatalogMiss => CatalogMiss,
            IdentityClassReasonCodes.StaleTrack => StaleTrack,
            _ => reasonCode,
        };
}

/// <summary>
/// One advisory identity-classification row for a contact. Sim-clock only — no fire orders
/// or authorization side effects.
/// </summary>
public sealed record IdentityClassRow(
    string ContactId,
    IdentityClassification Classification,
    string ReasonCode,
    IdentityConfidenceBand ConfidenceBand,
    ulong SimTick)
{
    /// <summary>Plain-language reason; never empty for published rows.</summary>
    public string ReasonLabel => IdentityClassReasonLabels.Format(ReasonCode);
}

/// <summary>Replay-stable unknown-vs-known identity ledger snapshot (DRG-225).</summary>
public sealed record IdentityClassSnapshot(IReadOnlyList<IdentityClassRow> Rows)
{
    public static IdentityClassSnapshot Empty { get; } =
        new(Array.Empty<IdentityClassRow>());
}
