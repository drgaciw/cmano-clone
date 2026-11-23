namespace ProjectAegis.Data.Tests.Snapshots;

using NUnit.Framework;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Platform;
using ProjectAegis.Data.Snapshots;

[TestFixture]
public sealed class CatalogFullSnapshotHasherTests
{
    [Test]
    public void GetSortedWeapons_returns_sorted_weapons_from_sqlite_and_inmemory()
    {
        var dbPath = CatalogReaderFactory.ResolveBalticPatrolDatabasePath();
        Assert.That(File.Exists(dbPath), Is.True, $"Missing baltic_patrol.db at {dbPath}");

        using var sqlite = new SqliteCatalogReader(dbPath, "h6-test");
        var sqliteWeapons = sqlite.GetSortedWeapons();
        Assert.That(sqliteWeapons.Count, Is.EqualTo(267));
        for (var i = 1; i < sqliteWeapons.Count; i++)
        {
            Assert.That(
                string.CompareOrdinal(sqliteWeapons[i - 1].WeaponId, sqliteWeapons[i].WeaponId),
                Is.LessThan(0),
                "SqliteCatalogReader.GetSortedWeapons must be strictly ordinal-sorted by WeaponId.");
        }

        var inMem = new InMemoryCatalogReader(
            Array.Empty<CatalogSensorBinding>(),
            weapons:
            [
                new CatalogWeaponRecord("w-zeta", "Zeta", 100, 5000, "Missile", "ARH"),
                new CatalogWeaponRecord("w-alpha", "Alpha", 50, 2000, "Gun", "Ballistic"),
            ]);
        var memWeapons = inMem.GetSortedWeapons();
        Assert.That(memWeapons.Select(w => w.WeaponId), Is.EqualTo(new[] { "w-alpha", "w-zeta" }));
    }

    [Test]
    public void GetSortedPlatforms_returns_sorted_platforms_with_domain_from_sqlite_and_inmemory()
    {
        var dbPath = CatalogReaderFactory.ResolveBalticPatrolDatabasePath();
        using var sqlite = new SqliteCatalogReader(dbPath, "h6-test");
        var sqlitePlatforms = sqlite.GetSortedPlatforms();
        Assert.That(sqlitePlatforms, Is.Not.Empty);
        for (var i = 1; i < sqlitePlatforms.Count; i++)
        {
            Assert.That(
                string.CompareOrdinal(sqlitePlatforms[i - 1].PlatformId, sqlitePlatforms[i].PlatformId),
                Is.LessThanOrEqualTo(0));
        }

        var inMem = new InMemoryCatalogReader(
            Array.Empty<CatalogSensorBinding>(),
            platforms:
            [
                new CatalogPlatformEntry("p-2", 58.0, 21.0, 200.0, "air"),
                new CatalogPlatformEntry("p-1", 57.0, 20.0, 400.0, "surface"),
            ]);
        var memPlatforms = inMem.GetSortedPlatforms();
        Assert.That(memPlatforms.Select(p => p.PlatformId), Is.EqualTo(new[] { "p-1", "p-2" }));
        Assert.That(memPlatforms[0].Domain, Is.EqualTo("surface"));
    }

    [Test]
    public void ComputeSha256Hex_is_order_independent_for_unsorted_custom_reader()
    {
        var forward = new CustomOrderCatalogReader(reverse: false);
        var reversed = new CustomOrderCatalogReader(reverse: true);

        var hashForward = CatalogFullSnapshotHasher.ComputeSha256Hex(forward);
        var hashReversed = CatalogFullSnapshotHasher.ComputeSha256Hex(reversed);

        Assert.That(CatalogFullSnapshotHasher.IsValidNonEmptySha256Hex(hashForward), Is.True);
        Assert.That(hashReversed, Is.EqualTo(hashForward));
    }

    [Test]
    public void ComputeSha256Hex_changes_when_any_of_the_13_domains_mutates()
    {
        var baseline = new MutableCatalogFixture();
        var baseHash = CatalogFullSnapshotHasher.ComputeSha256Hex(baseline);
        Assert.That(CatalogFullSnapshotHasher.IsValidNonEmptySha256Hex(baseHash), Is.True);

        var seenHashes = new HashSet<string>(StringComparer.Ordinal) { baseHash };
        for (var domainIndex = 0; domainIndex < 13; domainIndex++)
        {
            var mutated = new MutableCatalogFixture(mutatedDomainIndex: domainIndex);
            var mutatedHash = CatalogFullSnapshotHasher.ComputeSha256Hex(mutated);
            Assert.That(
                mutatedHash,
                Is.Not.EqualTo(baseHash),
                $"Domain index {domainIndex} mutation must change whole-catalog SHA-256.");
            Assert.That(
                seenHashes.Add(mutatedHash),
                Is.True,
                $"Domain index {domainIndex} produced a colliding hash.");
        }
    }

    [Test]
    public void AreCompatibleHashes_and_IsValidNonEmptySha256Hex_reject_empty_whitespace_and_null_hashes()
    {
        var valid = new string('a', 64);
        var validUpper = new string('A', 64);

        Assert.That(CatalogFullSnapshotHasher.IsValidNonEmptySha256Hex(valid), Is.True);
        Assert.That(CatalogFullSnapshotHasher.IsValidNonEmptySha256Hex(""), Is.False);
        Assert.That(CatalogFullSnapshotHasher.IsValidNonEmptySha256Hex("   "), Is.False);
        Assert.That(CatalogFullSnapshotHasher.IsValidNonEmptySha256Hex(null), Is.False);
        Assert.That(CatalogFullSnapshotHasher.IsValidNonEmptySha256Hex(new string('g', 64)), Is.False);

        Assert.That(CatalogFullSnapshotHasher.AreCompatibleHashes(valid, validUpper), Is.True);
        Assert.That(CatalogFullSnapshotHasher.AreCompatibleHashes("", ""), Is.False);
        Assert.That(CatalogFullSnapshotHasher.AreCompatibleHashes("", valid), Is.False);
        Assert.That(CatalogFullSnapshotHasher.AreCompatibleHashes(valid, ""), Is.False);
        Assert.That(CatalogFullSnapshotHasher.AreCompatibleHashes("   ", "   "), Is.False);
        Assert.That(CatalogFullSnapshotHasher.AreCompatibleHashes(null, valid), Is.False);
    }

    [Test]
    public void TryValidateBoundSnapshot_rejects_empty_content_hash_in_baltic_patrol_db_and_accepts_nonempty_hash()
    {
        var balticDb = CatalogReaderFactory.ResolveBalticPatrolDatabasePath();
        using var balticStore = new DbSnapshotStore(balticDb);
        var okEmpty = CatalogFullSnapshotHasher.TryValidateBoundSnapshot(
            balticStore,
            CatalogValidationDefaults.BalticSnapshotId,
            out var reason);
        Assert.That(okEmpty, Is.False);
        Assert.That(reason, Does.Contain("EMPTY_CATALOG_CONTENT_HASH"));

        var tempDir = Path.Combine(Path.GetTempPath(), "aegis-h6-db-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        try
        {
            var tempDb = Path.Combine(tempDir, "bound.db");
            CatalogSeedBootstrap.SeedBalticPatrol(tempDb, overwrite: true);
            using var reader = new SqliteCatalogReader(tempDb, "bound");
            var fullHash = CatalogFullSnapshotHasher.ComputeSha256Hex(reader);
            using var store = new DbSnapshotStore(tempDb);
            store.RecordRelease("rel-1.0", "snap-bound-1", fullHash, 123456789L);

            var okValid = CatalogFullSnapshotHasher.TryValidateBoundSnapshot(store, "snap-bound-1", out var okReason);
            Assert.That(okValid, Is.True);
            Assert.That(okReason, Is.Null);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    [Test]
    public void Existing_CatalogSnapshotHasher_sensor_only_behavior_is_preserved()
    {
        var sensors = new[]
        {
            new CatalogSensorBinding("u1", "radar-1", 0.9, "fact-1", 1.0, "batch-1", "src.md"),
        };

        var readerA = new InMemoryCatalogReader(
            sensors,
            weapons: [new CatalogWeaponRecord("w1", "Weapon 1", 100, 1000)]);
        var readerB = new InMemoryCatalogReader(
            sensors,
            weapons: [new CatalogWeaponRecord("w2", "Weapon 2", 200, 5000)]);

        var sensorHashA = CatalogSnapshotHasher.ComputeSha256Hex(readerA.GetSortedSensorBindings());
        var sensorHashB = CatalogSnapshotHasher.ComputeSha256Hex(readerB.GetSortedSensorBindings());
        Assert.That(sensorHashB, Is.EqualTo(sensorHashA), "CatalogSnapshotHasher must remain sensor-only.");

        var fullHashA = CatalogFullSnapshotHasher.ComputeSha256Hex(readerA);
        var fullHashB = CatalogFullSnapshotHasher.ComputeSha256Hex(readerB);
        Assert.That(fullHashB, Is.Not.EqualTo(fullHashA), "CatalogFullSnapshotHasher must distinguish weapons.");
    }

    private sealed class CustomOrderCatalogReader : ICatalogReader
    {
        private readonly bool _reverse;

        public CustomOrderCatalogReader(bool reverse) => _reverse = reverse;

        public string LayerVersion => "custom-order";

        public PlatformCatalogExportData LoadExportData(string? maxTlTier = null) =>
            new(
                Platforms: GetSortedPlatforms(),
                Sensors: GetSortedSensorBindings(),
                Mounts: GetSortedMounts(),
                Loadouts: GetSortedLoadouts(),
                Magazines: GetSortedMagazines(),
                Comms: GetSortedComms(),
                Links: GetSortedLinks(),
                Mobility: GetSortedMobility(),
                Signatures: GetSortedSignatures(),
                Emcon: GetSortedEmcon(),
                Damage: GetSortedPlatformDamage(),
                Swarms: GetSortedSwarmPlatforms());

        public IReadOnlyList<CatalogPlatformEntry> GetSortedPlatforms() => Order(
            new CatalogPlatformEntry("p1", 57.0, 20.0, 400.0, "surface"),
            new CatalogPlatformEntry("p2", 58.0, 21.0, 200.0, "air"));

        public IReadOnlyList<CatalogSensorBinding> GetSortedSensorBindings() => Order(
            new CatalogSensorBinding("p1", "s1", 0.8),
            new CatalogSensorBinding("p2", "s2", 0.7));

        public IReadOnlyList<CatalogWeaponRecord> GetSortedWeapons() => Order(
            new CatalogWeaponRecord("w1", "W1", 100, 1000, "Missile", "ARH"),
            new CatalogWeaponRecord("w2", "W2", 200, 2000, "Torpedo", "Wire"));

        public IReadOnlyList<CatalogMount> GetSortedMounts() => Order(
            new CatalogMount("p1", "m1", "VLS", 360, 8),
            new CatalogMount("p2", "m2", "Rail", 180, 2));

        public IReadOnlyList<CatalogLoadout> GetSortedLoadouts() => Order(
            new CatalogLoadout("p1", "l1", "ASuW", "asuw", true),
            new CatalogLoadout("p2", "l2", "CAP", "cap", false));

        public IReadOnlyList<CatalogMagazineEntry> GetSortedMagazines() => Order(
            new CatalogMagazineEntry("p1", "l1", "m1", "w1", 8, 10, 1),
            new CatalogMagazineEntry("p2", "l2", "m2", "w2", 2, 0, 0));

        public IReadOnlyList<CatalogMobility> GetSortedMobility() => Order(
            new CatalogMobility("p1", 30, 18, 0, 0, 100, 4000, 100),
            new CatalogMobility("p2", 500, 350, 40000, 0, 50, 1200, 4));

        public IReadOnlyList<CatalogPlatformDamage> GetSortedPlatformDamage() => Order(
            new CatalogPlatformDamage("p1", 100, 25, 1),
            new CatalogPlatformDamage("p2", 40, 50, 2));

        public IReadOnlyList<CatalogCommsBinding> GetSortedComms() => Order(
            new CatalogCommsBinding("p1", "lk1", "txrx", true),
            new CatalogCommsBinding("p2", "lk2", "rx", false));

        public IReadOnlyList<CatalogLinkEntry> GetSortedLinks() => Order(
            new CatalogLinkEntry("lk1", "Link 1", CatalogLinkTypes.Tactical, 50),
            new CatalogLinkEntry("lk2", "Link 2", CatalogLinkTypes.Satcom, 250));

        public IReadOnlyList<CatalogSignature> GetSortedSignatures() => Order(
            new CatalogSignature("p1", 10, 5, 110, 2),
            new CatalogSignature("p2", -5, 8, 90, 0));

        public IReadOnlyList<CatalogEmcon> GetSortedEmcon() => Order(
            new CatalogEmcon("p1", "free", "s1", "active"),
            new CatalogEmcon("p2", "silent", "s2", "off"));

        public IReadOnlyList<CatalogSwarmPlatform> GetSortedSwarmPlatforms() => Order(
            new CatalogSwarmPlatform("p1", 10, true),
            new CatalogSwarmPlatform("p2", 20, true));

        public bool TryGetBasePd(string platformId, string sensorId, out double basePd)
        {
            basePd = 0.8;
            return true;
        }

        private IReadOnlyList<T> Order<T>(T first, T second) =>
            _reverse ? [second, first] : [first, second];
    }

    private sealed class MutableCatalogFixture : ICatalogReader
    {
        private readonly int _mutatedDomainIndex;

        public MutableCatalogFixture(int mutatedDomainIndex = -1) => _mutatedDomainIndex = mutatedDomainIndex;

        public string LayerVersion => "mutable-fixture";

        public IReadOnlyList<CatalogPlatformEntry> GetSortedPlatforms() =>
        [
            new CatalogPlatformEntry("p1", _mutatedDomainIndex == 0 ? 57.5 : 57.0, 20.0, 400.0, "surface"),
        ];

        public IReadOnlyList<CatalogSensorBinding> GetSortedSensorBindings() =>
        [
            new CatalogSensorBinding("p1", "s1", _mutatedDomainIndex == 1 ? 0.95 : 0.8),
        ];

        public IReadOnlyList<CatalogWeaponRecord> GetSortedWeapons() =>
        [
            new CatalogWeaponRecord("w1", "W1", 100, _mutatedDomainIndex == 2 ? 9999 : 1000, "Missile", "ARH"),
        ];

        public IReadOnlyList<CatalogMount> GetSortedMounts() =>
        [
            new CatalogMount("p1", "m1", "VLS", 360, _mutatedDomainIndex == 3 ? 16 : 8),
        ];

        public IReadOnlyList<CatalogLoadout> GetSortedLoadouts() =>
        [
            new CatalogLoadout("p1", "l1", _mutatedDomainIndex == 4 ? "Strike" : "ASuW", "asuw", true),
        ];

        public IReadOnlyList<CatalogMagazineEntry> GetSortedMagazines() =>
        [
            new CatalogMagazineEntry("p1", "l1", "m1", "w1", _mutatedDomainIndex == 5 ? 12 : 8, 10, 1),
        ];

        public IReadOnlyList<CatalogMobility> GetSortedMobility() =>
        [
            new CatalogMobility("p1", _mutatedDomainIndex == 6 ? 35 : 30, 18),
        ];

        public IReadOnlyList<CatalogPlatformDamage> GetSortedPlatformDamage() =>
        [
            new CatalogPlatformDamage("p1", _mutatedDomainIndex == 7 ? 200 : 100, 25, 1),
        ];

        public IReadOnlyList<CatalogCommsBinding> GetSortedComms() =>
        [
            new CatalogCommsBinding("p1", "lk1", _mutatedDomainIndex == 8 ? "rx" : "txrx", true),
        ];

        public IReadOnlyList<CatalogLinkEntry> GetSortedLinks() =>
        [
            new CatalogLinkEntry("lk1", "Link 1", CatalogLinkTypes.Tactical, _mutatedDomainIndex == 9 ? 125 : 50),
        ];

        public IReadOnlyList<CatalogSignature> GetSortedSignatures() =>
        [
            new CatalogSignature("p1", _mutatedDomainIndex == 10 ? 15 : 10, 5, 110, 2),
        ];

        public IReadOnlyList<CatalogEmcon> GetSortedEmcon() =>
        [
            new CatalogEmcon("p1", "free", "s1", _mutatedDomainIndex == 11 ? "off" : "active"),
        ];

        public IReadOnlyList<CatalogSwarmPlatform> GetSortedSwarmPlatforms() =>
        [
            new CatalogSwarmPlatform("p1", _mutatedDomainIndex == 12 ? 40 : 10, true),
        ];

        public bool TryGetBasePd(string platformId, string sensorId, out double basePd)
        {
            basePd = 0.8;
            return true;
        }
    }
}
