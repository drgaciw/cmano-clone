using System.Linq;

namespace ProjectAegis.Data.Catalog;

/// <summary>Fixture catalog for headless tests and Baltic harness until SQLite import lands.</summary>
public sealed class InMemoryCatalogReader : ICatalogReader
{
    private readonly CatalogSensorBinding[] _bindings;
    private readonly Dictionary<DetectionBindingKey, double> _lookup;
    private readonly Dictionary<string, CatalogPlatformEntry> _platforms;
    private readonly CatalogMobility[] _mobility;
    private readonly CatalogSignature[] _signatures;
    private readonly CatalogEmcon[] _emcon;
    private readonly CatalogPlatformDamage[] _damage;
    private readonly CatalogMount[] _mounts;
    private readonly CatalogLoadout[] _loadouts;
    private readonly CatalogMagazineEntry[] _magazines;
    private readonly CatalogCommsBinding[] _comms;
    private readonly CatalogLinkEntry[] _links;
    private readonly Dictionary<string, int> _linkLatencyLookup;
    private readonly CatalogDependencyEdge[] _dependencyEdges;
    private readonly HashSet<string> _doctrinePlatforms;

    public InMemoryCatalogReader(
        IEnumerable<CatalogSensorBinding> bindings,
        string layerVersion = "p0-inmemory",
        IEnumerable<CatalogPlatformEntry>? platforms = null,
        IEnumerable<CatalogMobility>? mobility = null,
        IEnumerable<CatalogSignature>? signatures = null,
        IEnumerable<CatalogEmcon>? emcon = null,
        IEnumerable<CatalogPlatformDamage>? damage = null,
        IEnumerable<CatalogMount>? mounts = null,
        IEnumerable<CatalogLoadout>? loadouts = null,
        IEnumerable<CatalogMagazineEntry>? magazines = null,
        IEnumerable<CatalogCommsBinding>? comms = null,
        IEnumerable<CatalogLinkEntry>? links = null,
        IEnumerable<string>? doctrinePlatforms = null)
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
        _mobility = (mobility ?? Array.Empty<CatalogMobility>())
            .OrderBy(m => m.PlatformId, StringComparer.Ordinal)
            .ToArray();
        _signatures = (signatures ?? Array.Empty<CatalogSignature>())
            .OrderBy(s => s.PlatformId, StringComparer.Ordinal)
            .ToArray();
        _emcon = (emcon ?? Array.Empty<CatalogEmcon>())
            .OrderBy(e => e.PlatformId, StringComparer.Ordinal)
            .ThenBy(e => e.Condition, StringComparer.Ordinal)
            .ThenBy(e => e.EmitterId, StringComparer.Ordinal)
            .ToArray();
        _damage = (damage ?? Array.Empty<CatalogPlatformDamage>())
            .OrderBy(d => d.PlatformId, StringComparer.Ordinal)
            .ToArray();
        _mounts = (mounts ?? Array.Empty<CatalogMount>())
            .OrderBy(m => m.PlatformId, StringComparer.Ordinal)
            .ThenBy(m => m.MountId, StringComparer.Ordinal)
            .ToArray();
        _loadouts = (loadouts ?? Array.Empty<CatalogLoadout>())
            .OrderBy(l => l.PlatformId, StringComparer.Ordinal)
            .ThenBy(l => l.LoadoutId, StringComparer.Ordinal)
            .ToArray();
        _magazines = (magazines ?? Array.Empty<CatalogMagazineEntry>())
            .OrderBy(m => m.PlatformId, StringComparer.Ordinal)
            .ThenBy(m => m.LoadoutId, StringComparer.Ordinal)
            .ThenBy(m => m.MountId, StringComparer.Ordinal)
            .ThenBy(m => m.WeaponId, StringComparer.Ordinal)
            .ToArray();
        _comms = (comms ?? Array.Empty<CatalogCommsBinding>())
            .OrderBy(c => c.PlatformId, StringComparer.Ordinal)
            .ThenBy(c => c.LinkId, StringComparer.Ordinal)
            .ToArray();
        _links = CatalogSortKeyComparer.SortLinks(links ?? Array.Empty<CatalogLinkEntry>()).ToArray();
        _linkLatencyLookup = _links.ToDictionary(l => l.LinkId, l => l.LatencyMsNominal, StringComparer.Ordinal);
        _dependencyEdges = CatalogDependencyGraphIndex.BuildFrom(_mounts, _magazines, _bindings, _comms, _links).ToArray();
        _doctrinePlatforms = (doctrinePlatforms ?? Array.Empty<string>()).ToHashSet(StringComparer.Ordinal);
    }

    public string LayerVersion { get; }

    public static InMemoryCatalogReader BalticPatrolFixture() =>
        new(
        [
            new CatalogSensorBinding("u1", "radar-1", 1.0, "baltic-fixture-radar1"),
            new CatalogSensorBinding("u1", "radar-2", 0.75, "baltic-fixture-radar2"),
        ],
        "p0-baltic-fixture",
        CatalogValidationDefaults.BalticPlatforms().Concat(new[] { new CatalogPlatformEntry("legacy-patrol-ship", 57.0, 20.0, 100.0) }).ToList(),
        mounts:
        [
            new CatalogMount("legacy-patrol-ship", "main-gun", "Gun Mount"), // legacy for obsolete proof
        ],
        links: CatalogValidationDefaults.BalticLinks(),
        doctrinePlatforms: new[] { "legacy-patrol-ship" });

    /// <summary>Baltic v3: patrol ships + UCAV per side with Recon [Internal IR] loadout,
    /// plus one attack submarine per side (Virginia-class-derived hull sonar + towed array;
    /// QA-gauntlet Tier-3 fixture addition, see production/qa/gauntlet/gauntlet-20260709-1242/tier-3/).</summary>
    public static InMemoryCatalogReader BalticV3Fixture() =>
        new(
        [
            new CatalogSensorBinding("u1", "radar-1", 1.0, "baltic-fixture-radar1"),
            new CatalogSensorBinding("u1", "radar-2", 0.75, "baltic-fixture-radar2"),
            new CatalogSensorBinding("ucav-blue", "internal-ir", 0.85, "baltic-v3-ucav-blue-ir"),
            new CatalogSensorBinding("ucav-blue", "recon-radar", 0.70, "baltic-v3-ucav-blue-radar"),
            new CatalogSensorBinding("ucav-red", "internal-ir", 0.75, "baltic-v3-ucav-red-ir"),
            new CatalogSensorBinding("ucav-red", "recon-radar", 0.65, "baltic-v3-ucav-red-radar"),
            new CatalogSensorBinding("usub-blue", "hull-sonar", 0.80, "baltic-v3-usub-blue-hull-sonar"),
            new CatalogSensorBinding("usub-blue", "towed-array-sonar", 0.90, "baltic-v3-usub-blue-twa-sonar"),
            new CatalogSensorBinding("usub-red", "hull-sonar", 0.75, "baltic-v3-usub-red-hull-sonar"),
            new CatalogSensorBinding("usub-red", "towed-array-sonar", 0.85, "baltic-v3-usub-red-twa-sonar"),
        ],
        "p0-baltic-v3-fixture",
        CatalogValidationDefaults.BalticV3Platforms(),
        loadouts:
        [
            new CatalogLoadout("ucav-blue", "recon-internal-ir", "Recon [Internal IR]", "recon", IsDefault: true),
            new CatalogLoadout("ucav-red", "recon-internal-ir", "Recon [Internal IR]", "recon", IsDefault: true),
            new CatalogLoadout("usub-blue", "asw-strike", "ASW/Strike [Torpedo + VLS]", "asw", IsDefault: true),
            new CatalogLoadout("usub-red", "asw-strike", "ASW/Strike [Torpedo + VLS]", "asw", IsDefault: true),
        ],
        links: CatalogValidationDefaults.BalticLinks(),
        doctrinePlatforms: Array.Empty<string>());

    /// <summary>Baltic patrol + Phase B mobility/signature/EMCON rows for Req-21 sim consumption tests.</summary>
    public static InMemoryCatalogReader BalticPhaseBFixture(
        double maxSpeedKnots = 32,
        double rangeNm = 4200,
        double rcsBandDbsm = -12,
        string emconPosture = "active") =>
        new(
        [
            new CatalogSensorBinding("u1", "radar-1", 1.0, "baltic-fixture-radar1"),
            new CatalogSensorBinding("u1", "radar-2", 0.75, "baltic-fixture-radar2"),
        ],
        "p0-baltic-phase-b-fixture",
        CatalogValidationDefaults.BalticPlatforms(),
        mobility: [new CatalogMobility("u1", MaxSpeedKnots: maxSpeedKnots, RangeNm: rangeNm)],
        signatures: [new CatalogSignature("u1", RcsBandDbsm: rcsBandDbsm)],
        emcon:
        [
            new CatalogEmcon("u1", "free", "radar-1", emconPosture),
            new CatalogEmcon("u1", "silent", "radar-1", "off"),
        ]);

    /// <summary>Baltic patrol + default loadout/magazine rows for Req-16 engage readiness tests.</summary>
    public static InMemoryCatalogReader BalticMagazineFixture(int magazineQuantity = 2) =>
        new(
        [
            new CatalogSensorBinding("u1", "radar-1", 1.0, "baltic-fixture-radar1"),
            new CatalogSensorBinding("u1", "radar-2", 0.75, "baltic-fixture-radar2"),
        ],
        "p0-baltic-magazine-fixture",
        CatalogValidationDefaults.BalticPlatforms(),
        loadouts:
        [
            new CatalogLoadout("u1", "asuw-default", "ASUW Default", "asuw", IsDefault: true),
        ],
        magazines:
        [
            new CatalogMagazineEntry("u1", "asuw-default", "vls-fwd", CatalogWeaponIds.MvpDefault, magazineQuantity),
        ]);

    public IReadOnlyList<CatalogSensorBinding> GetSortedSensorBindings() => _bindings;

    public bool TryGetBasePd(string platformId, string sensorId, out double basePd) =>
        _lookup.TryGetValue(new DetectionBindingKey(platformId, sensorId), out basePd);

    public bool TryResolveDbRef(string dbRef, out string resolvedSnapshotId) =>
        CatalogValidationDefaults.TryResolveBalticDbRef(dbRef, out resolvedSnapshotId);

    public bool TryGetSnapshotBranch(string snapshotId, out string branch)
    {
        branch = CatalogTlTier.Default;
        if (string.IsNullOrWhiteSpace(snapshotId))
        {
            return false;
        }

        if (string.Equals(snapshotId, CatalogValidationDefaults.BalticSnapshotId, StringComparison.Ordinal) ||
            CatalogValidationDefaults.TryResolveBalticDbRef(snapshotId, out _))
        {
            return true;
        }

        return false;
    }

    public bool TryResolveSnapshotForTlBranch(string tlBranch, out string snapshotId, out string dbRef)
    {
        snapshotId = "";
        dbRef = "";

        var normalized = CatalogTlTier.Normalize(tlBranch);
        if (!string.Equals(normalized, CatalogTlTier.Tl0, StringComparison.Ordinal))
        {
            return false;
        }

        snapshotId = CatalogValidationDefaults.BalticSnapshotId;
        dbRef = CatalogValidationDefaults.BalticSnapshotId;
        return true;
    }

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
        if (string.Equals(platformId, "legacy-patrol-ship", StringComparison.OrdinalIgnoreCase))
        {
            if (LayerVersion != null && LayerVersion.Contains("v3", StringComparison.OrdinalIgnoreCase))
            {
                latDeg = 0;
                lonDeg = 0;
                return false;
            }
            latDeg = 57.0;
            lonDeg = 20.0;
            return true;
        }
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

    public IReadOnlyList<CatalogMobility> GetSortedMobility() => _mobility;

    public IReadOnlyList<CatalogSignature> GetSortedSignatures() => _signatures;

    public IReadOnlyList<CatalogEmcon> GetSortedEmcon() => _emcon;

    public IReadOnlyList<CatalogPlatformDamage> GetSortedPlatformDamage() => _damage;

    public IReadOnlyList<CatalogMount> GetSortedMounts() => _mounts;

    public IReadOnlyList<CatalogLoadout> GetSortedLoadouts() => _loadouts;

    public IReadOnlyList<CatalogMagazineEntry> GetSortedMagazines() => _magazines;

    public IReadOnlyList<CatalogCommsBinding> GetSortedComms() => _comms;

    public IReadOnlyList<CatalogLinkEntry> GetSortedLinks() => _links;

    public bool TryGetLinkLatencyMs(string linkId, out int latencyMsNominal) =>
        _linkLatencyLookup.TryGetValue(linkId, out latencyMsNominal);

    public IReadOnlyList<CatalogDependencyEdge> GetSortedDependencyEdges() => _dependencyEdges;

    public bool TryGetMobility(string platformId, out CatalogMobility mobility)
    {
        foreach (var row in _mobility)
        {
            if (string.Equals(row.PlatformId, platformId, StringComparison.Ordinal))
            {
                mobility = row;
                return true;
            }
        }

        mobility = new CatalogMobility(platformId);
        return false;
    }

    public bool TryGetSignature(string platformId, out CatalogSignature signature)
    {
        foreach (var row in _signatures)
        {
            if (string.Equals(row.PlatformId, platformId, StringComparison.Ordinal))
            {
                signature = row;
                return true;
            }
        }

        signature = new CatalogSignature(platformId);
        return false;
    }

    public bool TryGetEmcon(string platformId, string condition, string emitterId, out CatalogEmcon emcon)
    {
        foreach (var row in _emcon)
        {
            if (string.Equals(row.PlatformId, platformId, StringComparison.Ordinal) &&
                string.Equals(row.Condition, condition, StringComparison.Ordinal) &&
                string.Equals(row.EmitterId, emitterId, StringComparison.Ordinal))
            {
                emcon = row;
                return true;
            }
        }

        emcon = new CatalogEmcon(platformId, condition, emitterId);
        return false;
    }

    public bool TryGetPlatformDamage(string platformId, out CatalogPlatformDamage damage)
    {
        foreach (var row in _damage)
        {
            if (string.Equals(row.PlatformId, platformId, StringComparison.Ordinal))
            {
                damage = row;
                return true;
            }
        }

        damage = new CatalogPlatformDamage(platformId);
        return false;
    }

    public bool PlatformHasDoctrine(string platformId) => _doctrinePlatforms.Contains(platformId);
}