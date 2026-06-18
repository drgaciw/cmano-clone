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

    /// <summary>Req-21 Phase B: sorted mobility rows (platform_id).</summary>
    IReadOnlyList<CatalogMobility> GetSortedMobility() => GetSortedMobilityCore();

    /// <summary>Req-21 Phase B: sorted signature rows (platform_id).</summary>
    IReadOnlyList<CatalogSignature> GetSortedSignatures() => GetSortedSignaturesCore();

    /// <summary>Req-21 Phase B: sorted EMCON rows (platform_id, condition, emitter_id).</summary>
    IReadOnlyList<CatalogEmcon> GetSortedEmcon() => GetSortedEmconCore();

    bool TryGetMobility(string platformId, out CatalogMobility mobility) =>
        TryGetMobilityCore(platformId, out mobility);

    bool TryGetSignature(string platformId, out CatalogSignature signature) =>
        TryGetSignatureCore(platformId, out signature);

    bool TryGetEmcon(string platformId, string condition, string emitterId, out CatalogEmcon emcon) =>
        TryGetEmconCore(platformId, condition, emitterId, out emcon);

    /// <summary>Req-21 Phase B: sorted platform damage rows (platform_id).</summary>
    IReadOnlyList<CatalogPlatformDamage> GetSortedPlatformDamage() => GetSortedPlatformDamageCore();

    bool TryGetPlatformDamage(string platformId, out CatalogPlatformDamage damage) =>
        TryGetPlatformDamageCore(platformId, out damage);

    /// <summary>Req-21 Phase A: sorted mount rows (platform_id, mount_id).</summary>
    IReadOnlyList<CatalogMount> GetSortedMounts() => GetSortedMountsCore();

    IReadOnlyList<CatalogMount> GetSortedMountsCore() => [];

    IReadOnlyList<CatalogMobility> GetSortedMobilityCore() => [];

    IReadOnlyList<CatalogSignature> GetSortedSignaturesCore() => [];

    IReadOnlyList<CatalogEmcon> GetSortedEmconCore() => [];

    IReadOnlyList<CatalogPlatformDamage> GetSortedPlatformDamageCore() => [];

    bool TryGetMobilityCore(string platformId, out CatalogMobility mobility)
    {
        mobility = new CatalogMobility(platformId);
        return false;
    }

    bool TryGetSignatureCore(string platformId, out CatalogSignature signature)
    {
        signature = new CatalogSignature(platformId);
        return false;
    }

    bool TryGetEmconCore(string platformId, string condition, string emitterId, out CatalogEmcon emcon)
    {
        emcon = new CatalogEmcon(platformId, condition, emitterId);
        return false;
    }

    bool TryGetPlatformDamageCore(string platformId, out CatalogPlatformDamage damage)
    {
        damage = new CatalogPlatformDamage(platformId);
        return false;
    }

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
