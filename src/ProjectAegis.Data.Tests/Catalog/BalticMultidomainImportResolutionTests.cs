using Microsoft.Data.Sqlite;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Import;
using NUnit.Framework;

namespace ProjectAegis.Data.Tests.Catalog;

/// <summary>
/// Proves cmo-db.com multi-domain harvest fixtures parse with real ranges and that the
/// production catalog (after staged import) resolves new platforms, non-zero weapon envelopes,
/// and magazine rows (showcase Visby).
/// </summary>
[Runner.CatalogSqliteCollection]
public sealed class BalticMultidomainImportResolutionTests
{
    [Test]
    public void Live_fixture_markdown_parses_multi_domain_platforms()
    {
        var path = Path.Combine(
            CatalogJsonImporter.ResolveRepoRelative("tools/cmano-db-crawler/fixtures"),
            "baltic-multidomain-live.md");
        Assert.That(File.Exists(path), Is.True, $"Missing fixture: {path}");

        var platforms = CmoMarkdownImporter.ReadPlatformBindings(path, mapBalticIds: false);
        Assert.That(platforms.Count >= 10, Is.True, $"Expected multi-domain slice, got {platforms.Count}");

        var domains = platforms.Select(p => p.Domain).Distinct(StringComparer.Ordinal).OrderBy(d => d, StringComparer.Ordinal).ToArray();
        Assert.That(domains, Does.Contain("surface"));
        Assert.That(domains, Does.Contain("air"));
        Assert.That(domains, Does.Contain("subsurface"));

        Assert.That(platforms.Any(p => p.PlatformId.Contains("visby", StringComparison.OrdinalIgnoreCase)), Is.True);
        Assert.That(platforms.Any(p =>
            p.PlatformId.Contains("gripen", StringComparison.OrdinalIgnoreCase)
            || p.PlatformId.Contains("gotland", StringComparison.OrdinalIgnoreCase)), Is.True);
    }

    [Test]
    public void Expansion_wave2_fixture_parses_additional_ship_air_sub_platforms()
    {
        var path = Path.Combine(
            CatalogJsonImporter.ResolveRepoRelative("tools/cmano-db-crawler/fixtures"),
            "baltic-multidomain-expand.md");
        Assert.That(File.Exists(path), Is.True, $"Missing expansion fixture: {path}");

        var platforms = CmoMarkdownImporter.ReadPlatformBindings(path, mapBalticIds: false);
        Assert.That(platforms.Count >= 20, Is.True, $"Expected expansion slice ≥20, got {platforms.Count}");

        var domains = platforms.Select(p => p.Domain).ToHashSet(StringComparer.Ordinal);
        Assert.That(domains, Does.Contain("surface"));
        Assert.That(domains, Does.Contain("air"));
        Assert.That(domains, Does.Contain("subsurface"));

        Assert.That(platforms.Any(p => p.PlatformId.Contains("stockholm", StringComparison.OrdinalIgnoreCase)), Is.True);
        Assert.That(platforms.Any(p => p.PlatformId.Contains("gripen", StringComparison.OrdinalIgnoreCase)), Is.True);
        Assert.That(platforms.Any(p => p.PlatformId.Contains("gotland", StringComparison.OrdinalIgnoreCase)
            || p.PlatformId.Contains("lada", StringComparison.OrdinalIgnoreCase)), Is.True);
    }

    [Test]
    public void Weapon_fixture_parses_non_zero_max_ranges_via_importer()
    {
        var path = Path.Combine(
            CatalogJsonImporter.ResolveRepoRelative("tools/cmano-db-crawler/fixtures"),
            "baltic-multidomain-weapons-live.md");
        Assert.That(File.Exists(path), Is.True, $"Missing fixture: {path}");

        var weapons = CmoMarkdownImporter.ReadWeaponBindings(path);
        Assert.That(weapons.Count >= 20, Is.True, $"Expected broad weapon slice, got {weapons.Count}");

        var withRange = weapons.Where(w => w.MaxRangeMeters > 0).ToArray();
        Assert.That(withRange.Length, Is.EqualTo(weapons.Count));

        // Visby showcase weapons from live cmo-db scrape (ship/439 → weapon/455, weapon/1140)
        Assert.That(weapons.Any(w =>
            string.Equals(w.WeaponId, "cmo-weapon-455", StringComparison.Ordinal)
            && w.MaxRangeMeters > 0), Is.True);
        Assert.That(weapons.Any(w =>
            string.Equals(w.WeaponId, "cmo-weapon-1140", StringComparison.Ordinal)
            && w.MaxRangeMeters >= 2000), Is.True); // 2.2 km Air Max from scrape
    }

    [Test]
    public void Platform_fixture_magazines_resolve_visby_weapons_via_lookup()
    {
        var platformPath = Path.Combine(
            CatalogJsonImporter.ResolveRepoRelative("tools/cmano-db-crawler/fixtures"),
            "baltic-multidomain-live.md");
        var weaponPath = Path.Combine(
            CatalogJsonImporter.ResolveRepoRelative("tools/cmano-db-crawler/fixtures"),
            "baltic-multidomain-weapons-live.md");

        var weapons = CmoMarkdownImporter.ReadWeaponBindings(weaponPath);
        var lookup = CmoMarkdownImporter.BuildWeaponNameLookup(weapons);
        var (magazines, quarantined) = CmoMarkdownImporter.PartitionPlatformMagazines(
            platformPath,
            mapBalticIds: false,
            lookup,
            Path.GetFileName(platformPath));

        Assert.That(quarantined, Is.Empty);
        Assert.That(magazines.Any(m =>
            m.PlatformId.Contains("visby", StringComparison.OrdinalIgnoreCase)
            && string.Equals(m.WeaponId, "cmo-weapon-1140", StringComparison.Ordinal)), Is.True);
        Assert.That(magazines.Any(m =>
            m.PlatformId.Contains("visby", StringComparison.OrdinalIgnoreCase)
            && string.Equals(m.WeaponId, "cmo-weapon-455", StringComparison.Ordinal)), Is.True);
    }

    [Test]
    public void Production_catalog_resolves_imported_visby_weapons_and_magazines()
    {
        var dbPath = CatalogJsonImporter.ResolveRepoRelative(
            Path.Combine("assets", "data", "catalog", "baltic_patrol.db"));
        Assert.That(File.Exists(dbPath), Is.True, $"Catalog DB missing: {dbPath}");

        using (var conn = new SqliteConnection($"Data Source={dbPath};Pooling=false"))
        {
            conn.Open();
            using var countCmd = conn.CreateCommand();
            countCmd.CommandText = "SELECT COUNT(*) FROM platform";
            var count = Convert.ToInt32(countCmd.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture);
            Assert.That(count > 3, Is.True, $"Expected >3 platforms after import, got {count}");

            using var domainCmd = conn.CreateCommand();
            domainCmd.CommandText =
                "SELECT COUNT(DISTINCT domain) FROM platform WHERE domain IS NOT NULL AND domain != ''";
            var domainCount = Convert.ToInt32(
                domainCmd.ExecuteScalar(),
                System.Globalization.CultureInfo.InvariantCulture);
            Assert.That(domainCount >= 2, Is.True, $"Expected multi-domain catalog, distinct domains={domainCount}");

            using var rangeCmd = conn.CreateCommand();
            rangeCmd.CommandText =
                "SELECT COUNT(*) FROM weapon_catalog WHERE weapon_id LIKE 'cmo-weapon-%' AND max_range_meters <= 0";
            var zeroRange = Convert.ToInt32(
                rangeCmd.ExecuteScalar(),
                System.Globalization.CultureInfo.InvariantCulture);
            Assert.That(zeroRange, Is.EqualTo(0));

            using var magCmd = conn.CreateCommand();
            magCmd.CommandText =
                """
                SELECT COUNT(*) FROM platform_magazine
                WHERE platform_id = 'k-31-visby-2009'
                  AND weapon_id IN ('cmo-weapon-455', 'cmo-weapon-1140')
                """;
            var visbyMags = Convert.ToInt32(
                magCmd.ExecuteScalar(),
                System.Globalization.CultureInfo.InvariantCulture);
            Assert.That(visbyMags, Is.EqualTo(2));
        }

        using var reader = new SqliteCatalogReader(dbPath, "qa-multidomain-resolution");
        const string visbyId = "k-31-visby-2009";
        Assert.That(reader.TryGetCombatRadiusNm(visbyId, out var radius), Is.True, "Visby combat radius unresolved");
        Assert.That(radius > 0, Is.True);

        Assert.That(reader.TryGetWeaponEnvelope("cmo-weapon-1140", out var bofors), Is.True);
        Assert.That(bofors.MaxRangeMeters > 0, Is.True);
        Assert.That(reader.TryGetWeaponEnvelope("cmo-weapon-455", out var dualTrap), Is.True);
        Assert.That(dualTrap.MaxRangeMeters > 0, Is.True);

        var magazines = reader.GetSortedMagazines()
            .Where(m => string.Equals(m.PlatformId, visbyId, StringComparison.Ordinal))
            .ToArray();
        Assert.That(magazines, Is.Not.Empty);
        Assert.That(magazines.Any(m => string.Equals(m.WeaponId, "cmo-weapon-1140", StringComparison.Ordinal)), Is.True);
        Assert.That(magazines.Any(m => string.Equals(m.WeaponId, "cmo-weapon-455", StringComparison.Ordinal)), Is.True);

        Assert.That(reader.TryGetCombatRadiusNm("jas-39c-gripen-2005", out _), Is.True);
        Assert.That(reader.TryGetCombatRadiusNm("a-19-gotland-2020", out _), Is.True);

        // Expansion wave-2 showcase IDs (must remain after further imports)
        Assert.That(reader.TryGetCombatRadiusNm("k-11-stockholm-spica-iii-1986", out _), Is.True);
        Assert.That(reader.TryGetCombatRadiusNm("jas-39a-gripen-1997", out _), Is.True);
        Assert.That(reader.TryGetCombatRadiusNm("a-19-gotland-1996", out _), Is.True);

        // Expansion wave-3 multi-domain (cmo-db.com /en/cmo/ harvest)
        Assert.That(reader.TryGetCombatRadiusNm("d-32-daring-type-45-batch-1", out _), Is.True);
        Assert.That(reader.TryGetCombatRadiusNm("f-35a-lightning-ii", out _), Is.True);
        Assert.That(reader.TryGetCombatRadiusNm("ssn-774-virginia-blk-i-ii", out _), Is.True);
        Assert.That(reader.TryGetWeaponEnvelope("cmo-weapon-80001", out var aster), Is.True);
        Assert.That(aster.MaxRangeMeters > 0, Is.True);
        Assert.That(reader.TryGetWeaponEnvelope("cmo-weapon-80004", out var amraam), Is.True);
        Assert.That(amraam.MaxRangeMeters > 0, Is.True);
        Assert.That(reader.TryGetWeaponEnvelope("cmo-weapon-80007", out var mk48), Is.True);
        Assert.That(mk48.MaxRangeMeters > 0, Is.True);

        var stockholmMags = reader.GetSortedMagazines()
            .Where(m => string.Equals(m.PlatformId, "k-11-stockholm-spica-iii-1986", StringComparison.Ordinal))
            .ToArray();
        Assert.That(stockholmMags, Is.Not.Empty);

        var daringMags = reader.GetSortedMagazines()
            .Where(m => string.Equals(m.PlatformId, "d-32-daring-type-45-batch-1", StringComparison.Ordinal))
            .ToArray();
        Assert.That(daringMags, Is.Not.Empty);
        Assert.That(daringMags.Any(m => string.Equals(m.WeaponId, "cmo-weapon-80001", StringComparison.Ordinal)), Is.True);
    }

    [Test]
    public void Expansion_wave3_fixture_parses_ship_air_sub_with_correct_domains()
    {
        var path = Path.Combine(
            CatalogJsonImporter.ResolveRepoRelative("tools/cmano-db-crawler/fixtures"),
            "baltic-multidomain-wave3.md");
        Assert.That(File.Exists(path), Is.True, $"Missing wave-3 fixture: {path}");

        var platforms = CmoMarkdownImporter.ReadPlatformBindings(path, mapBalticIds: false);
        Assert.That(platforms.Count >= 10, Is.True, $"Expected wave-3 slice ≥10, got {platforms.Count}");

        var domains = platforms.Select(p => p.Domain).ToHashSet(StringComparer.Ordinal);
        Assert.That(domains, Does.Contain("surface"));
        Assert.That(domains, Does.Contain("air"));
        Assert.That(domains, Does.Contain("subsurface"));

        Assert.That(platforms.Any(p => p.PlatformId.Contains("daring", StringComparison.OrdinalIgnoreCase)), Is.True);
        Assert.That(platforms.Any(p => p.PlatformId.Contains("f-35", StringComparison.OrdinalIgnoreCase)), Is.True);
        Assert.That(platforms.Any(p => p.PlatformId.Contains("virginia", StringComparison.OrdinalIgnoreCase)), Is.True);

        var weapons = CmoMarkdownImporter.ReadWeaponBindings(
            Path.Combine(
                CatalogJsonImporter.ResolveRepoRelative("tools/cmano-db-crawler/fixtures"),
                "baltic-multidomain-weapons-wave3.md"));
        Assert.That(weapons.Any(w => w.WeaponId == "cmo-weapon-80001" && w.MaxRangeMeters > 0), Is.True);
        Assert.That(weapons.Any(w => w.WeaponId == "cmo-weapon-80004" && w.MaxRangeMeters > 0), Is.True);
        Assert.That(weapons.Any(w => w.WeaponId == "cmo-weapon-80007" && w.MaxRangeMeters > 0), Is.True);
    }
    [TestCase("Aircraft - Multirole (Fighter/Attack)", "air")]
    [TestCase("Aircraft - Helicopter ASW (NFH)", "air")]
    [TestCase("Anti-Submarine Warfare (ASW)", "air")]
    [TestCase("Aircraft - Helicopter ASW (Helix)", "air")]
    [TestCase("Submarine - Attack Submarine", "subsurface")]
    [TestCase("SSK - Hunter-Killer Submarine", "subsurface")]
    [TestCase("DDG - Guided Missile Destroyer", "surface")]
    // Wave 2: surface auxiliaries with "submarine" in Type must not map to subsurface.
    [TestCase("ASR - Submarine Rescue Ship", "surface")]
    [TestCase("AS - Submarine Tender", "surface")]
    [TestCase("PC - Submarine Chaser", "surface")]
    public void InferDomain_maps_cmo_db_type_labels(string platformClass, string expectedDomain)
    {
        Assert.That(CmoMarkdownImporter.InferDomain(platformClass), Is.EqualTo(expectedDomain));
    }

    [Test]
    public void Russia_1990_wave_fixture_parses_multi_domain_and_resolves_showcase_ids()
    {
        var fixtures = CatalogJsonImporter.ResolveRepoRelative("tools/cmano-db-crawler/fixtures");
        var path = Path.Combine(fixtures, "baltic-russia-1990-platforms.md");
        Assert.That(File.Exists(path), Is.True, $"Missing Russia 1990+ fixture: {path}");

        var platforms = CmoMarkdownImporter.ReadPlatformBindings(path, mapBalticIds: false);
        Assert.That(platforms.Count >= 12, Is.True, $"Expected ≥12 Russia platforms, got {platforms.Count}");

        var domains = platforms.Select(p => p.Domain).ToHashSet(StringComparer.Ordinal);
        Assert.That(domains, Does.Contain("surface"));
        Assert.That(domains, Does.Contain("air"));
        Assert.That(domains, Does.Contain("subsurface"));

        Assert.That(platforms.Any(p => p.PlatformId.Contains("gorshkov", StringComparison.OrdinalIgnoreCase)), Is.True);
        Assert.That(platforms.Any(p => p.PlatformId.Contains("felon", StringComparison.OrdinalIgnoreCase)
            || p.PlatformId.Contains("su-57", StringComparison.OrdinalIgnoreCase)), Is.True);
        Assert.That(platforms.Any(p => p.PlatformId.Contains("yasen", StringComparison.OrdinalIgnoreCase)), Is.True);

        // Anti-Submarine helicopter must classify as air (not subsurface).
        Assert.That(platforms.Any(p =>
            p.PlatformId.Contains("helix", StringComparison.OrdinalIgnoreCase)
            && string.Equals(p.Domain, "subsurface", StringComparison.Ordinal)), Is.False);

        var weapons = CmoMarkdownImporter.ReadWeaponBindings(
            Path.Combine(fixtures, "baltic-russia-1990-weapons.md"));
        Assert.That(weapons.Count >= 10, Is.True);
        foreach (var w in weapons)
{
Assert.That(w.MaxRangeMeters > 0, Is.True, w.WeaponId);
}

        var dbPath = CatalogJsonImporter.ResolveRepoRelative(
            Path.Combine("assets", "data", "catalog", "baltic_patrol.db"));
        using var reader = new SqliteCatalogReader(dbPath, "qa-russia-1990-resolution");
        Assert.That(reader.TryGetCombatRadiusNm("skr-admiral-sergey-gorshkov-pr-2235-0", out _), Is.True);
        Assert.That(reader.TryGetCombatRadiusNm("su-57-felon", out _), Is.True);
        Assert.That(reader.TryGetCombatRadiusNm("pla-885-severodvinsk-yasen", out _), Is.True);
        Assert.That(reader.TryGetCombatRadiusNm("ka-27m-helix-a", out _), Is.True);

        Assert.That(reader.TryGetWeaponEnvelope("cmo-weapon-81001", out var w1), Is.True);
        Assert.That(w1.MaxRangeMeters > 0, Is.True);

        var gorshkovMags = reader.GetSortedMagazines()
            .Where(m => m.PlatformId.Contains("gorshkov", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        Assert.That(gorshkovMags, Is.Not.Empty);
    }

    [Test]
    public void Sweden_1990_wave_fixture_parses_multi_domain_and_resolves_showcase_ids()
    {
        var fixtures = CatalogJsonImporter.ResolveRepoRelative("tools/cmano-db-crawler/fixtures");
        var path = Path.Combine(fixtures, "baltic-sweden-1990-platforms.md");
        Assert.That(File.Exists(path), Is.True, $"Missing Sweden 1990+ fixture: {path}");

        var platforms = CmoMarkdownImporter.ReadPlatformBindings(path, mapBalticIds: false);
        Assert.That(platforms.Count >= 8, Is.True, $"Expected ≥8 Sweden platforms, got {platforms.Count}");

        var domains = platforms.Select(p => p.Domain).ToHashSet(StringComparer.Ordinal);
        Assert.That(domains, Does.Contain("surface"));
        Assert.That(domains, Does.Contain("air"));
        Assert.That(domains, Does.Contain("subsurface"));

        Assert.That(platforms.Any(p => p.PlatformId.Contains("gavle", StringComparison.OrdinalIgnoreCase)
            || p.PlatformId.Contains("goteborg", StringComparison.OrdinalIgnoreCase)), Is.True);
        Assert.That(platforms.Any(p => p.PlatformId.Contains("sodermanland", StringComparison.OrdinalIgnoreCase)
            || p.PlatformId.Contains("södermanland", StringComparison.OrdinalIgnoreCase)
            || p.PlatformId.Contains("blekinge", StringComparison.OrdinalIgnoreCase)), Is.True);
        // Combat air: NH90 ASW (HKp 14F) — chrome Argus/trainer/cargo excluded from combat set
        Assert.That(platforms.Any(p => p.PlatformId.Contains("hkp-14f", StringComparison.OrdinalIgnoreCase)), Is.True);

        var weapons = CmoMarkdownImporter.ReadWeaponBindings(
            Path.Combine(fixtures, "baltic-sweden-1990-weapons.md"));
        Assert.That(weapons.Count >= 8, Is.True);
        foreach (var w in weapons)
{
Assert.That(w.MaxRangeMeters > 0, Is.True, w.WeaponId);
}

        // Honest munitions must include real cmo-db RB 15M Mk2 (not synthetic invent IDs)
        Assert.That(weapons.Any(w => w.WeaponId == "cmo-weapon-1455"
            && w.DisplayName.Contains("RB 15M Mk2", StringComparison.OrdinalIgnoreCase)), Is.True);
        Assert.That(weapons.Any(w => w.WeaponId == "cmo-weapon-1475"
            && w.DisplayName.Contains("Penguin", StringComparison.OrdinalIgnoreCase)), Is.True);

        var dbPath = CatalogJsonImporter.ResolveRepoRelative(
            Path.Combine("assets", "data", "catalog", "baltic_patrol.db"));
        using var reader = new SqliteCatalogReader(dbPath, "qa-sweden-1990-resolution");
        Assert.That(reader.TryGetCombatRadiusNm("k-22-gavle-ex-goteborg-class", out _), Is.True);
        Assert.That(reader.TryGetCombatRadiusNm("a-26-blekinge", out _), Is.True);
        Assert.That(reader.TryGetCombatRadiusNm("hkp-14f-nh90-ttt", out _), Is.True);

        Assert.That(reader.TryGetWeaponEnvelope("cmo-weapon-1455", out var rb15), Is.True);
        Assert.That(rb15.MaxRangeMeters > 50_000, Is.True, $"RB 15M Mk2 range too low: {rb15.MaxRangeMeters}");

        var gavleMags = reader.GetSortedMagazines()
            .Where(m => m.PlatformId.Contains("gavle", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        Assert.That(gavleMags, Is.Not.Empty);
        Assert.That(gavleMags.Any(m => m.WeaponId == "cmo-weapon-1455"), Is.True);

        // Gavle ASuW is RB 15, not Penguin; Penguin is Hugin-only
        var huginMags = reader.GetSortedMagazines()
            .Where(m => m.PlatformId.Contains("hugin", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        Assert.That(huginMags.Any(m => m.WeaponId == "cmo-weapon-1475"), Is.True);
        Assert.That(gavleMags.Any(m => m.WeaponId == "cmo-weapon-1475"), Is.False);
    }
}
