namespace ProjectAegis.Data.Catalog;

public sealed class NullCatalogReader : ICatalogReader
{
    public static readonly NullCatalogReader Instance = new();

    private static readonly CatalogSensorBinding[] Empty = Array.Empty<CatalogSensorBinding>();

    private NullCatalogReader()
    {
    }

    public string LayerVersion => "p0-scaffold";

    public IReadOnlyList<CatalogSensorBinding> GetSortedSensorBindings() => Empty;

    public bool TryGetBasePd(string platformId, string sensorId, out double basePd)
    {
        basePd = 0;
        return false;
    }
}
