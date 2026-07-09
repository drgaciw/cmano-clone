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
    }
}
