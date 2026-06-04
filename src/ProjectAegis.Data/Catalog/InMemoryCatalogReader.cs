namespace ProjectAegis.Data.Catalog;

/// <summary>Fixture catalog for headless tests and Baltic harness until SQLite import lands.</summary>
public sealed class InMemoryCatalogReader : ICatalogReader
{
    private readonly CatalogSensorBinding[] _bindings;
    private readonly Dictionary<DetectionBindingKey, double> _lookup;
    private readonly Dictionary<string, CatalogPlatformEntry> _platforms;

    public InMemoryCatalogReader(
        IEnumerable<CatalogSensorBinding> bindings,
        string layerVersion = "p0-inmemory",
        IEnumerable<CatalogPlatformEntry>? platforms = null)
    {
        LayerVersion = layerVersion;
        _bindings = bindings
            .OrderBy(b => b.PlatformId, StringComparer.Ordinal)
            .ThenBy(b => b.SensorId, StringComparer.Ordinal)
            .ToArray();
        _lookup = _bindings.ToDictionary(
            b => new DetectionBindingKey(b.PlatformId, b.SensorId),
            b => b.BasePd);
        _platforms = (platforms ?? Array.Empty<CatalogPlatformEntry>())
            .ToDictionary(p => p.PlatformId, StringComparer.Ordinal);
    }

    public string LayerVersion { get; }

    public static InMemoryCatalogReader BalticPatrolFixture() =>
        new(
        [
            new CatalogSensorBinding("u1", "radar-1", 1.0, "baltic-fixture-radar1"),
            new CatalogSensorBinding("u1", "radar-2", 0.75, "baltic-fixture-radar2"),
        ],
        "p0-baltic-fixture",
        CatalogValidationDefaults.BalticPlatforms());

    public IReadOnlyList<CatalogSensorBinding> GetSortedSensorBindings() => _bindings;

    public bool TryGetBasePd(string platformId, string sensorId, out double basePd) =>
        _lookup.TryGetValue(new DetectionBindingKey(platformId, sensorId), out basePd);

    public bool TryResolveDbRef(string dbRef, out string resolvedSnapshotId) =>
        CatalogValidationDefaults.TryResolveBalticDbRef(dbRef, out resolvedSnapshotId);

    public bool TryGetCombatRadiusNm(string platformId, out double combatRadiusNm)
    {
        if (_platforms.TryGetValue(platformId, out var entry))
        {
            combatRadiusNm = entry.CombatRadiusNm;
            return true;
        }

        combatRadiusNm = 0;
        return false;
    }

    public bool TryGetPlatformPosition(string platformId, out double latDeg, out double lonDeg)
    {
        if (_platforms.TryGetValue(platformId, out var entry))
        {
            latDeg = entry.LatDeg;
            lonDeg = entry.LonDeg;
            return true;
        }

        latDeg = 0;
        lonDeg = 0;
        return false;
    }

    public bool TryGetWeaponEnvelope(string weaponId, out WeaponEnvelopeDto envelope) =>
        CatalogWeaponDefaults.TryResolve(weaponId, out envelope);
}