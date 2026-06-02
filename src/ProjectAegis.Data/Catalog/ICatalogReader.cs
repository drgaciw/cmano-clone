namespace ProjectAegis.Data.Catalog;

public interface ICatalogReader
{
    string LayerVersion { get; }

    /// <summary>Sorted by platform_id then sensor_id (ordinal) for deterministic iteration.</summary>
    IReadOnlyList<CatalogSensorBinding> GetSortedSensorBindings();

    bool TryGetBasePd(string platformId, string sensorId, out double basePd);
}
