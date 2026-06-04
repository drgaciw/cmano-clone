namespace ProjectAegis.Data.Catalog;

public interface ICatalogReader
{
    string LayerVersion { get; }

    /// <summary>Sorted by platform_id then sensor_id (ordinal) for deterministic iteration.</summary>
    IReadOnlyList<CatalogSensorBinding> GetSortedSensorBindings();

    bool TryGetBasePd(string platformId, string sensorId, out double basePd);

    /// <summary>ADR-008: resolve scenario dbRef to catalog snapshot id.</summary>
    bool TryResolveDbRef(string dbRef, out string resolvedSnapshotId) =>
        TryResolveDbRefCore(dbRef, out resolvedSnapshotId);

    /// <summary>ADR-008: platform combat radius (nm), round-trip budget per GDD §4.1.</summary>
    bool TryGetCombatRadiusNm(string platformId, out double combatRadiusNm) =>
        TryGetCombatRadiusNmCore(platformId, out combatRadiusNm);

    /// <summary>ADR-008: WGS84 position for reachability checks.</summary>
    bool TryGetPlatformPosition(string platformId, out double latDeg, out double lonDeg) =>
        TryGetPlatformPositionCore(platformId, out latDeg, out lonDeg);

    /// <summary>DATA-4: weapon min/max range (meters) for engage envelope wiring.</summary>
    bool TryGetWeaponEnvelope(string weaponId, out WeaponEnvelopeDto envelope) =>
        TryGetWeaponEnvelopeCore(weaponId, out envelope);

    bool TryGetWeaponEnvelopeCore(string weaponId, out WeaponEnvelopeDto envelope)
    {
        envelope = default;
        return false;
    }

    bool TryResolveDbRefCore(string dbRef, out string resolvedSnapshotId)
    {
        resolvedSnapshotId = "";
        return false;
    }

    bool TryGetCombatRadiusNmCore(string platformId, out double combatRadiusNm)
    {
        combatRadiusNm = 0;
        return false;
    }

    bool TryGetPlatformPositionCore(string platformId, out double latDeg, out double lonDeg)
    {
        latDeg = 0;
        lonDeg = 0;
        return false;
    }
}
