namespace ProjectAegis.Data.Catalog;

/// <summary>
/// Machine-checkable gauntlet oracle expects (policy <c>gauntlet.expect</c>).
/// Null numeric fields mean "no bound".
/// </summary>
public sealed record GauntletOracleExpect(
    string? Side = null,
    int? MinKills = null,
    int? MaxMissilesFired = null,
    int? MinDenials = null,
    int? MaxDenials = null,
    double? MinScore = null,
    double? MaxScore = null,
    bool RequireNonEmptyFingerprint = true);
