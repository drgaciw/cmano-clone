using Microsoft.Data.Sqlite;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Import;
using Xunit;

namespace ProjectAegis.Data.Tests.Catalog;

/// <summary>
/// Proves cmo-db.com multi-domain harvest fixtures parse with real ranges and that the
/// production catalog (after staged import) resolves new platforms, non-zero weapon envelopes,
/// and magazine rows (showcase Visby).
/// </summary>
[Collection("CatalogSqlite")]
public sealed class BalticMultidomainImportResolutionTests
{
    [Fact]
    public void Live_fixture_markdown_parses_multi_domain_platforms()
    {
        var path = Path.Combine(
            CatalogJsonImporter.ResolveRepoRelative("tools/cmano-db-crawler/fixtures"),
            "baltic-multidomain-live.md");
        Assert.True(File.Exists(path), $"Missing fixture: {path}");

        var platforms = CmoMarkdownImporter.ReadPlatformBindings(path, mapBalticIds: false);
        Assert.True(platforms.Count >= 10, $"Expected multi-domain slice, got {platforms.Count}");

        var domains = platforms.Select(p => p.Domain).Distinct(StringComparer.Ordinal).OrderBy(d => d).ToArray();
        Assert.Contains("surface", domains);
        Assert.Contains("air", domains);
        Assert.Contains("subsurface", domains);

        Assert.Contains(platforms, p => p.PlatformId.Contains("visby", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(platforms, p =>
            p.PlatformId.Contains("gripen", StringComparison.OrdinalIgnoreCase)
            || p.PlatformId.Contains("gotland", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Expansion_wave2_fixture_parses_additional_ship_air_sub_platforms()
    {
        var path = Path.Combine(
            CatalogJsonImporter.ResolveRepoRelative("tools/cmano-db-crawler/fixtures"),
            "baltic-multidomain-expand.md");
        Assert.True(File.Exists(path), $"Missing expansion fixture: {path}");

        var platforms = CmoMarkdownImporter.ReadPlatformBindings(path, mapBalticIds: false);
        Assert.True(platforms.Count >= 20, $"Expected expansion slice ≥20, got {platforms.Count}");

        var domains = platforms.Select(p => p.Domain).ToHashSet(StringComparer.Ordinal);
        Assert.Contains("surface", domains);
        Assert.Contains("air", domains);
        Assert.Contains("subsurface", domains);

        Assert.Contains(platforms, p => p.PlatformId.Contains("stockholm", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(platforms, p => p.PlatformId.Contains("gripen", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(platforms, p => p.PlatformId.Contains("gotland", StringComparison.OrdinalIgnoreCase)
            || p.PlatformId.Contains("lada", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Weapon_fixture_parses_non_zero_max_ranges_via_importer()
    {
        var path = Path.Combine(
            CatalogJsonImporter.ResolveRepoRelative("tools/cmano-db-crawler/fixtures"),
            "baltic-multidomain-weapons-live.md");
        Assert.True(File.Exists(path), $"Missing fixture: {path}");

        var weapons = CmoMarkdownImporter.ReadWeaponBindings(path);
        Assert.True(weapons.Count >= 20, $"Expected broad weapon slice, got {weapons.Count}");

        var withRange = weapons.Where(w => w.MaxRangeMeters > 0).ToArray();
        Assert.Equal(weapons.Count, withRange.Length);

        // Visby showcase weapons from live cmo-db scrape (ship/439 → weapon/455, weapon/1140)
        Assert.Contains(weapons, w =>
            string.Equals(w.WeaponId, "cmo-weapon-455", StringComparison.Ordinal)
            && w.MaxRangeMeters > 0);
        Assert.Contains(weapons, w =>
            string.Equals(w.WeaponId, "cmo-weapon-1140", StringComparison.Ordinal)
            && w.MaxRangeMeters >= 2000); // 2.2 km Air Max from scrape
    }

    [Fact]
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

        Assert.Empty(quarantined);
        Assert.Contains(magazines, m =>
            m.PlatformId.Contains("visby", StringComparison.OrdinalIgnoreCase)
            && string.Equals(m.WeaponId, "cmo-weapon-1140", StringComparison.Ordinal));
        Assert.Contains(magazines, m =>
            m.PlatformId.Contains("visby", StringComparison.OrdinalIgnoreCase)
            && string.Equals(m.WeaponId, "cmo-weapon-455", StringComparison.Ordinal));
    }

    [Fact]
    public void Production_catalog_resolves_imported_visby_weapons_and_magazines()
    {
        var dbPath = CatalogJsonImporter.ResolveRepoRelative(
            Path.Combine("assets", "data", "catalog", "baltic_patrol.db"));
        Assert.True(File.Exists(dbPath), $"Catalog DB missing: {dbPath}");

        using (var conn = new SqliteConnection($"Data Source={dbPath};Pooling=false"))
        {
            conn.Open();
            using var countCmd = conn.CreateCommand();
            countCmd.CommandText = "SELECT COUNT(*) FROM platform";
            var count = Convert.ToInt32(countCmd.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture);
            Assert.True(count > 3, $"Expected >3 platforms after import, got {count}");

            using var domainCmd = conn.CreateCommand();
            domainCmd.CommandText =
                "SELECT COUNT(DISTINCT domain) FROM platform WHERE domain IS NOT NULL AND domain != ''";
            var domainCount = Convert.ToInt32(
                domainCmd.ExecuteScalar(),
                System.Globalization.CultureInfo.InvariantCulture);
            Assert.True(domainCount >= 2, $"Expected multi-domain catalog, distinct domains={domainCount}");

            using var rangeCmd = conn.CreateCommand();
            rangeCmd.CommandText =
                "SELECT COUNT(*) FROM weapon_catalog WHERE weapon_id LIKE 'cmo-weapon-%' AND max_range_meters <= 0";
            var zeroRange = Convert.ToInt32(
                rangeCmd.ExecuteScalar(),
                System.Globalization.CultureInfo.InvariantCulture);
            Assert.Equal(0, zeroRange);

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
            Assert.Equal(2, visbyMags);
        }

        using var reader = new SqliteCatalogReader(dbPath, "qa-multidomain-resolution");
        const string visbyId = "k-31-visby-2009";
        Assert.True(reader.TryGetCombatRadiusNm(visbyId, out var radius), "Visby combat radius unresolved");
        Assert.True(radius > 0);

        Assert.True(reader.TryGetWeaponEnvelope("cmo-weapon-1140", out var bofors));
        Assert.True(bofors.MaxRangeMeters > 0);
        Assert.True(reader.TryGetWeaponEnvelope("cmo-weapon-455", out var dualTrap));
        Assert.True(dualTrap.MaxRangeMeters > 0);

        var magazines = reader.GetSortedMagazines()
            .Where(m => string.Equals(m.PlatformId, visbyId, StringComparison.Ordinal))
            .ToArray();
        Assert.NotEmpty(magazines);
        Assert.Contains(magazines, m => string.Equals(m.WeaponId, "cmo-weapon-1140", StringComparison.Ordinal));
        Assert.Contains(magazines, m => string.Equals(m.WeaponId, "cmo-weapon-455", StringComparison.Ordinal));

        Assert.True(reader.TryGetCombatRadiusNm("jas-39c-gripen-2005", out _));
        Assert.True(reader.TryGetCombatRadiusNm("a-19-gotland-2020", out _));

        // Expansion wave-2 showcase IDs (must remain after further imports)
        Assert.True(reader.TryGetCombatRadiusNm("k-11-stockholm-spica-iii-1986", out _));
        Assert.True(reader.TryGetCombatRadiusNm("jas-39a-gripen-1997", out _));
        Assert.True(reader.TryGetCombatRadiusNm("a-19-gotland-1996", out _));

        // Expansion wave-3 multi-domain (cmo-db.com /en/cmo/ harvest)
        Assert.True(reader.TryGetCombatRadiusNm("d-32-daring-type-45-batch-1", out _));
        Assert.True(reader.TryGetCombatRadiusNm("f-35a-lightning-ii", out _));
        Assert.True(reader.TryGetCombatRadiusNm("ssn-774-virginia-blk-i-ii", out _));
        Assert.True(reader.TryGetWeaponEnvelope("cmo-weapon-80001", out var aster));
        Assert.True(aster.MaxRangeMeters > 0);
        Assert.True(reader.TryGetWeaponEnvelope("cmo-weapon-80004", out var amraam));
        Assert.True(amraam.MaxRangeMeters > 0);
        Assert.True(reader.TryGetWeaponEnvelope("cmo-weapon-80007", out var mk48));
        Assert.True(mk48.MaxRangeMeters > 0);

        var stockholmMags = reader.GetSortedMagazines()
            .Where(m => string.Equals(m.PlatformId, "k-11-stockholm-spica-iii-1986", StringComparison.Ordinal))
            .ToArray();
        Assert.NotEmpty(stockholmMags);

        var daringMags = reader.GetSortedMagazines()
            .Where(m => string.Equals(m.PlatformId, "d-32-daring-type-45-batch-1", StringComparison.Ordinal))
            .ToArray();
        Assert.NotEmpty(daringMags);
        Assert.Contains(daringMags, m => string.Equals(m.WeaponId, "cmo-weapon-80001", StringComparison.Ordinal));
    }

    [Fact]
    public void Expansion_wave3_fixture_parses_ship_air_sub_with_correct_domains()
    {
        var path = Path.Combine(
            CatalogJsonImporter.ResolveRepoRelative("tools/cmano-db-crawler/fixtures"),
            "baltic-multidomain-wave3.md");
        Assert.True(File.Exists(path), $"Missing wave-3 fixture: {path}");

        var platforms = CmoMarkdownImporter.ReadPlatformBindings(path, mapBalticIds: false);
        Assert.True(platforms.Count >= 10, $"Expected wave-3 slice ≥10, got {platforms.Count}");

        var domains = platforms.Select(p => p.Domain).ToHashSet(StringComparer.Ordinal);
        Assert.Contains("surface", domains);
        Assert.Contains("air", domains);
        Assert.Contains("subsurface", domains);

        Assert.Contains(platforms, p => p.PlatformId.Contains("daring", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(platforms, p => p.PlatformId.Contains("f-35", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(platforms, p => p.PlatformId.Contains("virginia", StringComparison.OrdinalIgnoreCase));

        var weapons = CmoMarkdownImporter.ReadWeaponBindings(
            Path.Combine(
                CatalogJsonImporter.ResolveRepoRelative("tools/cmano-db-crawler/fixtures"),
                "baltic-multidomain-weapons-wave3.md"));
        Assert.Contains(weapons, w => w.WeaponId == "cmo-weapon-80001" && w.MaxRangeMeters > 0);
        Assert.Contains(weapons, w => w.WeaponId == "cmo-weapon-80004" && w.MaxRangeMeters > 0);
        Assert.Contains(weapons, w => w.WeaponId == "cmo-weapon-80007" && w.MaxRangeMeters > 0);
    }

    [Theory]
    [InlineData("Aircraft - Multirole (Fighter/Attack)", "air")]
    [InlineData("Aircraft - Helicopter ASW (NFH)", "air")]
    [InlineData("Anti-Submarine Warfare (ASW)", "air")]
    [InlineData("Aircraft - Helicopter ASW (Helix)", "air")]
    [InlineData("Submarine - Attack Submarine", "subsurface")]
    [InlineData("SSK - Hunter-Killer Submarine", "subsurface")]
    [InlineData("DDG - Guided Missile Destroyer", "surface")]
    // Wave 2: surface auxiliaries with "submarine" in Type must not map to subsurface.
    [InlineData("ASR - Submarine Rescue Ship", "surface")]
    [InlineData("AS - Submarine Tender", "surface")]
    [InlineData("PC - Submarine Chaser", "surface")]
    public void InferDomain_maps_cmo_db_type_labels(string platformClass, string expectedDomain)
    {
        Assert.Equal(expectedDomain, CmoMarkdownImporter.InferDomain(platformClass));
    }

    [Fact]
    public void Russia_1990_wave_fixture_parses_multi_domain_and_resolves_showcase_ids()
    {
        var fixtures = CatalogJsonImporter.ResolveRepoRelative("tools/cmano-db-crawler/fixtures");
        var path = Path.Combine(fixtures, "baltic-russia-1990-platforms.md");
        Assert.True(File.Exists(path), $"Missing Russia 1990+ fixture: {path}");

        var platforms = CmoMarkdownImporter.ReadPlatformBindings(path, mapBalticIds: false);
        Assert.True(platforms.Count >= 12, $"Expected ≥12 Russia platforms, got {platforms.Count}");

        var domains = platforms.Select(p => p.Domain).ToHashSet(StringComparer.Ordinal);
        Assert.Contains("surface", domains);
        Assert.Contains("air", domains);
        Assert.Contains("subsurface", domains);

        Assert.Contains(platforms, p => p.PlatformId.Contains("gorshkov", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(platforms, p => p.PlatformId.Contains("felon", StringComparison.OrdinalIgnoreCase)
            || p.PlatformId.Contains("su-57", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(platforms, p => p.PlatformId.Contains("yasen", StringComparison.OrdinalIgnoreCase));

        // Anti-Submarine helicopter must classify as air (not subsurface).
        Assert.DoesNotContain(platforms, p =>
            p.PlatformId.Contains("helix", StringComparison.OrdinalIgnoreCase)
            && string.Equals(p.Domain, "subsurface", StringComparison.Ordinal));

        var weapons = CmoMarkdownImporter.ReadWeaponBindings(
            Path.Combine(fixtures, "baltic-russia-1990-weapons.md"));
        Assert.True(weapons.Count >= 10);
        Assert.All(weapons, w => Assert.True(w.MaxRangeMeters > 0, w.WeaponId));

        var dbPath = CatalogJsonImporter.ResolveRepoRelative(
            Path.Combine("assets", "data", "catalog", "baltic_patrol.db"));
        using var reader = new SqliteCatalogReader(dbPath, "qa-russia-1990-resolution");
        Assert.True(reader.TryGetCombatRadiusNm("skr-admiral-sergey-gorshkov-pr-2235-0", out _));
        Assert.True(reader.TryGetCombatRadiusNm("su-57-felon", out _));
        Assert.True(reader.TryGetCombatRadiusNm("pla-885-severodvinsk-yasen", out _));
        Assert.True(reader.TryGetCombatRadiusNm("ka-27m-helix-a", out _));

        Assert.True(reader.TryGetWeaponEnvelope("cmo-weapon-81001", out var w1));
        Assert.True(w1.MaxRangeMeters > 0);

        var gorshkovMags = reader.GetSortedMagazines()
            .Where(m => m.PlatformId.Contains("gorshkov", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        Assert.NotEmpty(gorshkovMags);
    }

    [Fact]
    public void Sweden_1990_wave_fixture_parses_multi_domain_and_resolves_showcase_ids()
    {
        var fixtures = CatalogJsonImporter.ResolveRepoRelative("tools/cmano-db-crawler/fixtures");
        var path = Path.Combine(fixtures, "baltic-sweden-1990-platforms.md");
        Assert.True(File.Exists(path), $"Missing Sweden 1990+ fixture: {path}");

        var platforms = CmoMarkdownImporter.ReadPlatformBindings(path, mapBalticIds: false);
        Assert.True(platforms.Count >= 8, $"Expected ≥8 Sweden platforms, got {platforms.Count}");

        var domains = platforms.Select(p => p.Domain).ToHashSet(StringComparer.Ordinal);
        Assert.Contains("surface", domains);
        Assert.Contains("air", domains);
        Assert.Contains("subsurface", domains);

        Assert.Contains(platforms, p => p.PlatformId.Contains("gavle", StringComparison.OrdinalIgnoreCase)
            || p.PlatformId.Contains("goteborg", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(platforms, p => p.PlatformId.Contains("sodermanland", StringComparison.OrdinalIgnoreCase)
            || p.PlatformId.Contains("södermanland", StringComparison.OrdinalIgnoreCase)
            || p.PlatformId.Contains("blekinge", StringComparison.OrdinalIgnoreCase));
        // Combat air: NH90 ASW (HKp 14F) — chrome Argus/trainer/cargo excluded from combat set
        Assert.Contains(platforms, p => p.PlatformId.Contains("hkp-14f", StringComparison.OrdinalIgnoreCase));

        var weapons = CmoMarkdownImporter.ReadWeaponBindings(
            Path.Combine(fixtures, "baltic-sweden-1990-weapons.md"));
        Assert.True(weapons.Count >= 8);
        Assert.All(weapons, w => Assert.True(w.MaxRangeMeters > 0, w.WeaponId));

        // Honest munitions must include real cmo-db RB 15M Mk2 (not synthetic invent IDs)
        Assert.Contains(weapons, w => w.WeaponId == "cmo-weapon-1455"
            && w.DisplayName.Contains("RB 15M Mk2", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(weapons, w => w.WeaponId == "cmo-weapon-1475"
            && w.DisplayName.Contains("Penguin", StringComparison.OrdinalIgnoreCase));

        var dbPath = CatalogJsonImporter.ResolveRepoRelative(
            Path.Combine("assets", "data", "catalog", "baltic_patrol.db"));
        using var reader = new SqliteCatalogReader(dbPath, "qa-sweden-1990-resolution");
        Assert.True(reader.TryGetCombatRadiusNm("k-22-gavle-ex-goteborg-class", out _));
        Assert.True(reader.TryGetCombatRadiusNm("a-26-blekinge", out _));
        Assert.True(reader.TryGetCombatRadiusNm("hkp-14f-nh90-ttt", out _));

        Assert.True(reader.TryGetWeaponEnvelope("cmo-weapon-1455", out var rb15));
        Assert.True(rb15.MaxRangeMeters > 50_000, $"RB 15M Mk2 range too low: {rb15.MaxRangeMeters}");

        var gavleMags = reader.GetSortedMagazines()
            .Where(m => m.PlatformId.Contains("gavle", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        Assert.NotEmpty(gavleMags);
        Assert.Contains(gavleMags, m => m.WeaponId == "cmo-weapon-1455");

        // Gavle ASuW is RB 15, not Penguin; Penguin is Hugin-only
        var huginMags = reader.GetSortedMagazines()
            .Where(m => m.PlatformId.Contains("hugin", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        Assert.Contains(huginMags, m => m.WeaponId == "cmo-weapon-1475");
        Assert.DoesNotContain(gavleMags, m => m.WeaponId == "cmo-weapon-1475");
    }
}
