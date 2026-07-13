namespace ProjectAegis.Data.Catalog;

using ProjectAegis.Data.Platform;

/// <summary>Preview post-approve catalog state for kill-chain commit gates (read-only overlay).</summary>
public sealed class CatalogStagingOverlayReader : ICatalogReader
{
    private readonly ICatalogReader _inner;
    private readonly IReadOnlyList<CatalogMount>? _mountOverlay;
    private readonly IReadOnlyDictionary<string, WeaponEnvelopeDto>? _weaponEnvelopes;

    private CatalogStagingOverlayReader(
        ICatalogReader inner,
        IReadOnlyList<CatalogMount>? mountOverlay,
        IReadOnlyDictionary<string, WeaponEnvelopeDto>? weaponEnvelopes)
    {
        _inner = inner;
        _mountOverlay = mountOverlay;
        _weaponEnvelopes = weaponEnvelopes;
    }

    public static ICatalogReader PreviewAfterMounts(ICatalogReader live, IReadOnlyList<CatalogMount> stagedMounts) =>
        new CatalogStagingOverlayReader(live, stagedMounts, weaponEnvelopes: null);

    public static ICatalogReader PreviewAfterWeapons(ICatalogReader live, IReadOnlyList<CatalogWeaponRecord> stagedWeapons) =>
        new CatalogStagingOverlayReader(
            live,
            mountOverlay: null,
            weaponEnvelopes: stagedWeapons.ToDictionary(
                w => w.WeaponId,
                w => new WeaponEnvelopeDto(w.MinRangeMeters, w.MaxRangeMeters),
                StringComparer.Ordinal));

    public static ICatalogReader PreviewAfterPlatforms(ICatalogReader live, IReadOnlyList<CatalogPlatformBinding> _) =>
        new CatalogStagingOverlayReader(live, mountOverlay: null, weaponEnvelopes: null);

    public string LayerVersion => _inner.LayerVersion;

    public IReadOnlyList<CatalogSensorBinding> GetSortedSensorBindings() => _inner.GetSortedSensorBindings();

    public bool TryGetBasePd(string platformId, string sensorId, out double basePd) =>
        _inner.TryGetBasePd(platformId, sensorId, out basePd);

    public bool TryResolveDbRef(string dbRef, out string resolvedSnapshotId) =>
        _inner.TryResolveDbRef(dbRef, out resolvedSnapshotId);

    public bool TryGetSnapshotBranch(string snapshotId, out string branch) =>
        _inner.TryGetSnapshotBranch(snapshotId, out branch);

    public bool TryResolveSnapshotForTlBranch(string tlBranch, out string snapshotId, out string dbRef) =>
        _inner.TryResolveSnapshotForTlBranch(tlBranch, out snapshotId, out dbRef);

    public bool TryGetCombatRadiusNm(string platformId, out double combatRadiusNm) =>
        _inner.TryGetCombatRadiusNm(platformId, out combatRadiusNm);

    public bool TryGetPlatformPosition(string platformId, out double latDeg, out double lonDeg) =>
        _inner.TryGetPlatformPosition(platformId, out latDeg, out lonDeg);

    public bool TryGetWeaponEnvelope(string weaponId, out WeaponEnvelopeDto envelope)
    {
        if (_weaponEnvelopes != null &&
            _weaponEnvelopes.TryGetValue(weaponId, out envelope))
        {
            return true;
        }

        return _inner.TryGetWeaponEnvelope(weaponId, out envelope);
    }

    public IReadOnlyList<CatalogMobility> GetSortedMobility() => _inner.GetSortedMobility();

    public IReadOnlyList<CatalogSignature> GetSortedSignatures() => _inner.GetSortedSignatures();

    public IReadOnlyList<CatalogEmcon> GetSortedEmcon() => _inner.GetSortedEmcon();

    public IReadOnlyList<CatalogPlatformDamage> GetSortedPlatformDamage() => _inner.GetSortedPlatformDamage();

    public IReadOnlyList<CatalogMount> GetSortedMounts() =>
        _mountOverlay == null ? _inner.GetSortedMounts() : MergeMounts(_inner.GetSortedMounts(), _mountOverlay);

    public IReadOnlyList<CatalogLoadout> GetSortedLoadouts() => _inner.GetSortedLoadouts();

    public IReadOnlyList<CatalogMagazineEntry> GetSortedMagazines() => _inner.GetSortedMagazines();

    public IReadOnlyList<CatalogCommsBinding> GetSortedComms() => _inner.GetSortedComms();

    public IReadOnlyList<CatalogLinkEntry> GetSortedLinks() => _inner.GetSortedLinks();

    public bool TryGetLinkLatencyMs(string linkId, out int latencyMsNominal) =>
        _inner.TryGetLinkLatencyMs(linkId, out latencyMsNominal);

    public IReadOnlyList<CatalogDependencyEdge> GetSortedDependencyEdges() =>
        CatalogDependencyGraphIndex.BuildFrom(this);

    public bool TryGetMobility(string platformId, out CatalogMobility mobility) =>
        _inner.TryGetMobility(platformId, out mobility);

    public bool TryGetSignature(string platformId, out CatalogSignature signature) =>
        _inner.TryGetSignature(platformId, out signature);

    public bool TryGetEmcon(string platformId, string condition, string emitterId, out CatalogEmcon emcon) =>
        _inner.TryGetEmcon(platformId, condition, emitterId, out emcon);

    public bool TryGetPlatformDamage(string platformId, out CatalogPlatformDamage damage) =>
        _inner.TryGetPlatformDamage(platformId, out damage);

    public PlatformCatalogExportData LoadExportData(string? maxTlTier = null)
    {
        var data = _inner.LoadExportData(maxTlTier);
        if (_mountOverlay == null)
        {
            return data;
        }

        return data with { Mounts = GetSortedMounts() };
    }

    private static IReadOnlyList<CatalogMount> MergeMounts(
        IReadOnlyList<CatalogMount> live,
        IReadOnlyList<CatalogMount> staged)
    {
        var merged = live.ToDictionary(m => MountKey(m.PlatformId, m.MountId), m => m, StringComparer.Ordinal);
        foreach (var row in staged)
        {
            merged[MountKey(row.PlatformId, row.MountId)] = row;
        }

        return merged.Values
            .OrderBy(m => m.PlatformId, StringComparer.Ordinal)
            .ThenBy(m => m.MountId, StringComparer.Ordinal)
            .ToArray();
    }

    private static string MountKey(string platformId, string mountId) => $"{platformId}\0{mountId}";
}