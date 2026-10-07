namespace ProjectAegis.Data.Catalog;

/// <summary>
/// Platform sensor row used to resolve detection basePd (sorted by platform_id, sensor_id).
/// <see cref="Modality"/> is S111-02 / DRG-10 extend-only (Radar default; Infrared / Visual fixtures).
/// <see cref="RadarScanType"/>, <see cref="FrequencyAgile"/> and <see cref="RadarTechGeneration"/> are
/// EW-01 / DRG-386 extend-only ECCM attributes (empty / false / 0 = unspecified, no jam adjustment).
/// </summary>
public sealed record CatalogSensorBinding(
    string PlatformId,
    string SensorId,
    double BasePd,
    string SourceFactId = "fixture",
    double Confidence = 1.0,
    string ImportBatchId = "",
    string SourceFile = "",
    string ReviewState = CatalogReviewStates.Approved,
    int TrlLevel = 9,
    string ValueTier = CatalogProvenanceTier.GameplayAbstraction,
    string ReviewerId = "",
    long RevisedUtcTicks = 0,
    string CitationRef = "",
    double JamStrength = 0.0,
    double EccmFactor = 1.0,
    string Modality = CatalogSensorModalities.Radar,
    string RadarScanType = CatalogRadarScanTypes.Unspecified,
    bool FrequencyAgile = false,
    int RadarTechGeneration = 0);
