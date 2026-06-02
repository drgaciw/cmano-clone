namespace ProjectAegis.Data.Catalog;

/// <summary>Fixture catalog for headless tests and Baltic harness until SQLite import lands.</summary>
public sealed class InMemoryCatalogReader : ICatalogReader
{
    private readonly CatalogSensorBinding[] _bindings;
    private readonly Dictionary<DetectionBindingKey, double> _lookup;

    public InMemoryCatalogReader(IEnumerable<CatalogSensorBinding> bindings, string layerVersion = "p0-inmemory")
    {
        LayerVersion = layerVersion;
        _bindings = bindings
            .OrderBy(b => b.PlatformId, StringComparer.Ordinal)
            .ThenBy(b => b.SensorId, StringComparer.Ordinal)
            .ToArray();
        _lookup = _bindings.ToDictionary(
            b => new DetectionBindingKey(b.PlatformId, b.SensorId),
            b => b.BasePd);
    }

    public string LayerVersion { get; }

    public static InMemoryCatalogReader BalticPatrolFixture() =>
        new(
        [
            new CatalogSensorBinding("u1", "radar-1", 1.0, "baltic-fixture"),
        ],
        "p0-baltic-fixture");

    public IReadOnlyList<CatalogSensorBinding> GetSortedSensorBindings() => _bindings;

    public bool TryGetBasePd(string platformId, string sensorId, out double basePd) =>
        _lookup.TryGetValue(new DetectionBindingKey(platformId, sensorId), out basePd);
}