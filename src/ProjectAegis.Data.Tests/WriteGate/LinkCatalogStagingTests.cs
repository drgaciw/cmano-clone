using Microsoft.Data.Sqlite;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.WriteGate;
using Xunit;

namespace ProjectAegis.Data.Tests.WriteGate;

[Collection("CatalogSqlite")]
public sealed class LinkCatalogStagingTests
{
    [Fact]
    public void GetSortedLinks_orders_by_link_id_ordinal()
    {
        var reader = new InMemoryCatalogReader(
            [],
            links:
            [
                new CatalogLinkEntry("SATCOM_B", "SATCOM", CatalogLinkTypes.Satcom, 250),
                new CatalogLinkEntry("NATO_TADIL_J", "Link 16", CatalogLinkTypes.Tactical, 50),
            ]);

        var links = reader.GetSortedLinks();
        Assert.Equal("NATO_TADIL_J", links[0].LinkId);
        Assert.Equal("SATCOM_B", links[1].LinkId);
    }

    [Fact]
    public void Baltic_fixture_seeds_NATO_and_SATCOM_links_with_latency()
    {
        var reader = InMemoryCatalogReader.BalticPatrolFixture();
        var links = reader.GetSortedLinks();

        Assert.Equal(2, links.Count);
        Assert.Equal("NATO_TADIL_J", links[0].LinkId);
        Assert.Equal("SATCOM_B", links[1].LinkId);
        Assert.True(reader.TryGetLinkLatencyMs("NATO_TADIL_J", out var tacticalMs));
        Assert.Equal(50, tacticalMs);
        Assert.True(reader.TryGetLinkLatencyMs("SATCOM_B", out var satcomMs));
        Assert.Equal(250, satcomMs);
    }

    [Fact]
    public void Propose_approve_commits_link_catalog_row()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-link-approve-{Guid.NewGuid():N}.db");
        try
        {
            var link = new CatalogLinkEntry(
                "CUSTOM_LINK",
                "Custom Datalink",
                CatalogLinkTypes.Tactical,
                LatencyMsNominal: 120);

            using (var gate = new CatalogWriteGate(dbPath, new FixedCatalogClock(3402)))
            {
                var batchId = gate.ProposeLinkCatalogBatch([link], "agent", "link-test");
                var decision = gate.ApproveBatch(batchId, "human", "qa-reviewer");
                Assert.True(decision.Committed);
            }

            using var reader = new SqliteCatalogReader(dbPath, "link-approve-test");
            var committed = Assert.Single(reader.GetSortedLinks(), l => l.LinkId == "CUSTOM_LINK");
            Assert.Equal("Custom Datalink", committed.DisplayName);
            Assert.Equal(CatalogLinkTypes.Tactical, committed.LinkType);
            Assert.Equal(120, committed.LatencyMsNominal);
            Assert.True(reader.TryGetLinkLatencyMs("CUSTOM_LINK", out var latency));
            Assert.Equal(120, latency);
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Fact]
    public void Approve_link_batch_extend_only_preserves_existing_catalog_rows()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-link-extend-{Guid.NewGuid():N}.db");
        try
        {
            var seed = CatalogValidationDefaults.BalticLinks();
            using (var gate = new CatalogWriteGate(dbPath, new FixedCatalogClock(3404)))
            {
                var seedBatch = gate.ProposeLinkCatalogBatch(seed, "agent", "seed-links");
                Assert.True(gate.ApproveBatch(seedBatch, "human", "qa-reviewer").Committed);

                var extension = new CatalogLinkEntry(
                    "CUSTOM_LINK",
                    "Custom Datalink",
                    CatalogLinkTypes.Tactical,
                    LatencyMsNominal: 120);
                var extendBatch = gate.ProposeLinkCatalogBatch([extension], "agent", "extend-links");
                Assert.True(gate.ApproveBatch(extendBatch, "human", "qa-reviewer").Committed);
            }

            using var reader = new SqliteCatalogReader(dbPath, "link-extend-only-test");
            var links = reader.GetSortedLinks();
            Assert.Equal(3, links.Count);
            Assert.Contains(links, l => l.LinkId == "NATO_TADIL_J");
            Assert.Contains(links, l => l.LinkId == "SATCOM_B");
            Assert.Contains(links, l => l.LinkId == "CUSTOM_LINK");
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Fact]
    public void CatalogWriteGate_has_zero_forbidden_link_catalog_delete_surfaces()
    {
        var gateSourcePath = Path.GetFullPath(
            Path.Combine(
                AppContext.BaseDirectory,
                "..", "..", "..", "..",
                "ProjectAegis.Data",
                "WriteGate",
                "CatalogWriteGate.cs"));

        Assert.True(File.Exists(gateSourcePath), $"Missing source file: {gateSourcePath}");
        var source = File.ReadAllText(gateSourcePath);

        Assert.DoesNotContain("DELETE FROM link_catalog", source, StringComparison.Ordinal);
        Assert.Contains("INSERT OR REPLACE INTO link_catalog", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Reject_batch_purges_link_staging_without_commit()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-link-reject-{Guid.NewGuid():N}.db");
        try
        {
            using (var gate = new CatalogWriteGate(dbPath, new FixedCatalogClock(3403)))
            {
                var batchId = gate.ProposeLinkCatalogBatch(
                    [new CatalogLinkEntry("REJECT_LINK", "Reject Me", CatalogLinkTypes.Satcom, 999)],
                    "agent",
                    "reject-test");
                Assert.False(gate.RejectBatch(batchId, "human", "qa").Committed);
            }

            using var connection = new SqliteConnection($"Data Source={dbPath};Pooling=false");
            connection.Open();
            Assert.Equal(0, CountRows(connection, "catalog_staging_link"));
            Assert.Equal(0, CountRows(connection, "link_catalog", "link_id = 'REJECT_LINK'"));

            using var reader = new SqliteCatalogReader(dbPath, "link-reject-test");
            Assert.False(reader.TryGetLinkLatencyMs("REJECT_LINK", out _));
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    private static int CountRows(SqliteConnection connection, string table, string? whereClause = null)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = whereClause is null
            ? $"SELECT COUNT(*) FROM {table}"
            : $"SELECT COUNT(*) FROM {table} WHERE {whereClause}";
        return Convert.ToInt32(cmd.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture);
    }

    private static void Cleanup(string dbPath)
    {
        SqliteConnection.ClearAllPools();
        if (File.Exists(dbPath))
        {
            File.Delete(dbPath);
        }
    }
}