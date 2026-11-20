using Microsoft.Data.Sqlite;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.WriteGate;
using NUnit.Framework;

namespace ProjectAegis.Data.Tests.WriteGate;

[ProjectAegis.Data.Tests.Runner.CatalogSqliteCollection]
public sealed class LinkCatalogStagingTests
{
    [Test]
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
        Assert.That(links[0].LinkId, Is.EqualTo("NATO_TADIL_J"));
        Assert.That(links[1].LinkId, Is.EqualTo("SATCOM_B"));
    }

    [Test]
    public void Baltic_fixture_seeds_NATO_and_SATCOM_links_with_latency()
    {
        var reader = InMemoryCatalogReader.BalticPatrolFixture();
        var links = reader.GetSortedLinks();

        Assert.That(links.Count, Is.EqualTo(2));
        Assert.That(links[0].LinkId, Is.EqualTo("NATO_TADIL_J"));
        Assert.That(links[1].LinkId, Is.EqualTo("SATCOM_B"));
        Assert.That(reader.TryGetLinkLatencyMs("NATO_TADIL_J", out var tacticalMs), Is.True);
        Assert.That(tacticalMs, Is.EqualTo(50));
        Assert.That(reader.TryGetLinkLatencyMs("SATCOM_B", out var satcomMs), Is.True);
        Assert.That(satcomMs, Is.EqualTo(250));
    }

    [Test]
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
                Assert.That(decision.Committed, Is.True);
            }

            using var reader = new SqliteCatalogReader(dbPath, "link-approve-test");
            var committed = ProjectAegis.Data.Tests.AssertEx.Single(reader.GetSortedLinks(), l => l.LinkId == "CUSTOM_LINK");
            Assert.That(committed.DisplayName, Is.EqualTo("Custom Datalink"));
            Assert.That(committed.LinkType, Is.EqualTo(CatalogLinkTypes.Tactical));
            Assert.That(committed.LatencyMsNominal, Is.EqualTo(120));
            Assert.That(reader.TryGetLinkLatencyMs("CUSTOM_LINK", out var latency), Is.True);
            Assert.That(latency, Is.EqualTo(120));
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Test]
    public void Approve_link_batch_extend_only_preserves_existing_catalog_rows()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-link-extend-{Guid.NewGuid():N}.db");
        try
        {
            var seed = CatalogValidationDefaults.BalticLinks();
            using (var gate = new CatalogWriteGate(dbPath, new FixedCatalogClock(3404)))
            {
                var seedBatch = gate.ProposeLinkCatalogBatch(seed, "agent", "seed-links");
                Assert.That(gate.ApproveBatch(seedBatch, "human", "qa-reviewer").Committed, Is.True);

                var extension = new CatalogLinkEntry(
                    "CUSTOM_LINK",
                    "Custom Datalink",
                    CatalogLinkTypes.Tactical,
                    LatencyMsNominal: 120);
                var extendBatch = gate.ProposeLinkCatalogBatch([extension], "agent", "extend-links");
                Assert.That(gate.ApproveBatch(extendBatch, "human", "qa-reviewer").Committed, Is.True);
            }

            using var reader = new SqliteCatalogReader(dbPath, "link-extend-only-test");
            var links = reader.GetSortedLinks();
            Assert.That(links.Count, Is.EqualTo(3));
            Assert.That(links.Any(l => l.LinkId == "NATO_TADIL_J"), Is.True);
            Assert.That(links.Any(l => l.LinkId == "SATCOM_B"), Is.True);
            Assert.That(links.Any(l => l.LinkId == "CUSTOM_LINK"), Is.True);
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Test]
    public void CatalogWriteGate_has_zero_forbidden_link_catalog_delete_surfaces()
    {
        var gateSourcePath = Path.GetFullPath(
            Path.Combine(
                AppContext.BaseDirectory,
                "..", "..", "..", "..",
                "ProjectAegis.Data",
                "WriteGate",
                "CatalogWriteGate.cs"));

        Assert.That(File.Exists(gateSourcePath), Is.True, $"Missing source file: {gateSourcePath}");
        var source = File.ReadAllText(gateSourcePath);

        Assert.That(source, Does.Not.Contain("DELETE FROM link_catalog"));
        Assert.That(source, Does.Contain("INSERT OR REPLACE INTO link_catalog"));
    }

    [Test]
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
                Assert.That(gate.RejectBatch(batchId, "human", "qa").Committed, Is.False);
            }

            using var connection = new SqliteConnection($"Data Source={dbPath};Pooling=false");
            connection.Open();
            Assert.That(CountRows(connection, "catalog_staging_link"), Is.EqualTo(0));
            Assert.That(CountRows(connection, "link_catalog", "link_id = 'REJECT_LINK'"), Is.EqualTo(0));

            using var reader = new SqliteCatalogReader(dbPath, "link-reject-test");
            Assert.That(reader.TryGetLinkLatencyMs("REJECT_LINK", out _), Is.False);
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