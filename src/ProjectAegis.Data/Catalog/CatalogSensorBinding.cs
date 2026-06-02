namespace ProjectAegis.Data.Catalog;

/// <summary>Platform sensor row used to resolve detection basePd (sorted by platform_id, sensor_id).</summary>
public sealed record CatalogSensorBinding(
    string PlatformId,
    string SensorId,
    double BasePd,
    string SourceFactId = "fixture",
    double Confidence = 1.0,
    string ImportBatchId = "",
    string SourceFile = "",
    string ReviewState = CatalogReviewStates.Approved,
    int TrlLevel = 9);