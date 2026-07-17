namespace ProjectAegis.Data.Catalog;

/// <summary>
/// Machine-checkable gauntlet oracle expects (policy <c>gauntlet.expect</c>).
/// Null numeric fields mean "no bound".
/// Optional fingerprint token gates fail closed when inject / multi-domain launch
/// tokens are stripped from the batch CSV.
/// </summary>
public sealed record GauntletOracleExpect(
    string? Side = null,
    int? MinKills = null,
    int? MaxMissilesFired = null,
    int? MinDenials = null,
    int? MaxDenials = null,
    double? MinScore = null,
    double? MaxScore = null,
    bool RequireNonEmptyFingerprint = true,
    /// <summary>Each substring must appear in the row fingerprint (e.g. CommsStateChange, Degraded).</summary>
    IReadOnlyList<string>? RequireFingerprintSubstrings = null,
    /// <summary>
    /// Each unit id must appear as shooter on an Engagement|…|True|Launched fingerprint token
    /// (multi-domain concurrent launch gate).
    /// </summary>
    IReadOnlyList<string>? RequireTrueLaunchedShooters = null);
