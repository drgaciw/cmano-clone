namespace ProjectAegis.Data.Catalog;

/// <summary>
/// Machine-checkable gauntlet oracle expects (policy <c>gauntlet.expect</c>).
/// Null numeric fields mean "no bound".
/// Optional fingerprint token gates fail closed when inject / multi-domain launch
/// tokens are stripped from the batch CSV.
/// </summary>
/// <param name="Side">Optional side whose results are evaluated.</param>
/// <param name="MinKills">Optional minimum kill count.</param>
/// <param name="MaxMissilesFired">Optional maximum missile count.</param>
/// <param name="MinDenials">Optional minimum denial count.</param>
/// <param name="MaxDenials">Optional maximum denial count.</param>
/// <param name="MinScore">Optional minimum score.</param>
/// <param name="MaxScore">Optional maximum score.</param>
/// <param name="RequireNonEmptyFingerprint">Whether the row fingerprint must be non-empty.</param>
/// <param name="RequireFingerprintSubstrings">Each substring must appear in the row fingerprint (for example, CommsStateChange or Degraded).</param>
/// <param name="RequireTrueLaunchedShooters">Each unit id must appear as shooter on an Engagement|…|True|Launched fingerprint token for the multi-domain concurrent-launch gate.</param>
public sealed record GauntletOracleExpect(
    string? Side = null,
    int? MinKills = null,
    int? MaxMissilesFired = null,
    int? MinDenials = null,
    int? MaxDenials = null,
    double? MinScore = null,
    double? MaxScore = null,
    bool RequireNonEmptyFingerprint = true,
    IReadOnlyList<string>? RequireFingerprintSubstrings = null,
    IReadOnlyList<string>? RequireTrueLaunchedShooters = null);
