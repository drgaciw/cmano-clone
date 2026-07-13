namespace ProjectAegis.Data.Catalog;

/// <summary>Req-21 / S22-04: weapon catalog row parsed from CMO weapon markdown sections.</summary>
public sealed record CatalogWeaponRecord(
    string WeaponId,
    string DisplayName = "",
    double MinRangeMeters = 0,
    double MaxRangeMeters = 0,
    string WeaponType = "",
    string Guidance = "",
    string ReviewState = CatalogReviewStates.Provisional,
    string SourceFactId = "",
    string ImportBatchId = "",
    string SourceFile = "");