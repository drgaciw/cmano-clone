using Microsoft.Data.Sqlite;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Import;
using Xunit;

namespace ProjectAegis.Data.Tests.Catalog;

/// <summary>
/// Proves cmo-db.com multi-domain harvest fixtures parse and that the production catalog
/// (after staged import) resolves new surface/air/subsurface platform IDs via real reader APIs.
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
    public void Production_catalog_resolves_imported_visby_and_related_rows()
    {
        var dbPath = CatalogJsonImporter.ResolveRepoRelative(
            Path.Combine("assets", "data", "catalog", "baltic_patrol.db"));
        Assert.True(File.Exists(dbPath), $"Catalog DB missing: {dbPath}");

        // Count via SQLite (authoritative live store) then resolve through shipped reader APIs.
        using (var conn = new SqliteConnection($"Data Source={dbPath};Pooling=false"))
        {
            conn.Open();
            using var countCmd = conn.CreateCommand();
            countCmd.CommandText = "SELECT COUNT(*) FROM platform";
            var count = Convert.ToInt32(countCmd.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture);
            Assert.True(count > 3, $"Expected >3 platforms after import, got {count}");

            using var domainCmd = conn.CreateCommand();
            domainCmd.CommandText = "SELECT COUNT(DISTINCT domain) FROM platform WHERE domain IS NOT NULL AND domain != ''";
            var domainCount = Convert.ToInt32(domainCmd.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture);
            Assert.True(domainCount >= 2, $"Expected multi-domain catalog, distinct domains={domainCount}");
        }

        using var reader = new SqliteCatalogReader(dbPath, "qa-multidomain-resolution");
        const string visbyId = "k-31-visby-2009";
        Assert.True(reader.TryGetCombatRadiusNm(visbyId, out var radius), "Visby combat radius unresolved");
        Assert.True(radius > 0);

        var sensors = reader.GetSortedSensorBindings()
            .Where(s => string.Equals(s.PlatformId, visbyId, StringComparison.Ordinal))
            .ToArray();
        Assert.NotEmpty(sensors);

        var mounts = reader.GetSortedMounts()
            .Where(m => string.Equals(m.PlatformId, visbyId, StringComparison.Ordinal))
            .ToArray();
        Assert.NotEmpty(mounts);

        // Air + subsurface IDs from the same import wave
        Assert.True(reader.TryGetCombatRadiusNm("jas-39c-gripen-2005", out _));
        Assert.True(reader.TryGetCombatRadiusNm("a-19-gotland-2020", out _));
    }
}
