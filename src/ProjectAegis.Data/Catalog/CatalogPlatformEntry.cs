namespace ProjectAegis.Data.Catalog;

public sealed record CatalogPlatformEntry(
    string PlatformId,
    double LatDeg,
    double LonDeg,
    double CombatRadiusNm);