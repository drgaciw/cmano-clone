using System.Text.Json;
using Microsoft.Data.Sqlite;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.WriteGate;
using NUnit.Framework;

namespace ProjectAegis.MissionEditor.Cli.Tests;

[TestFixture]
public sealed class CatalogDependencyGraphCommandTests
{
    [Test]
    public void DependencyGraph_catalog_dependency_graph_emits_sorted_edges()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-cli-dep-graph-{Guid.NewGuid():N}.db");
        try
        {
            CatalogSeedBootstrap.SeedBalticPatrol(dbPath, overwrite: true);

            // S36-04 + S37-03: seed full kill-chain (platform→link + weapon→mount→sensor) via gate for full chains
            using (var gate = new CatalogWriteGate(dbPath, new FixedCatalogClock(36004)))
            {
                var link = new CatalogLinkEntry("NATO_TADIL_J", "NATO Link 16", CatalogLinkTypes.Tactical, 50);
                var linkBatch = gate.ProposeLinkCatalogBatch([link], "agent", "s36-cli-test");
                Assert.That(gate.ApproveBatch(linkBatch, "human", "qa").Committed, Is.True);

                var comms = new CatalogCommsBinding("u1", "NATO_TADIL_J", ReviewState: CatalogReviewStates.Approved);
                var commsBatch = gate.ProposeCommsBatch([comms], "agent", "s36-cli-test");
                Assert.That(gate.ApproveBatch(commsBatch, "human", "qa").Committed, Is.True);

                // minimal mount/weapon for full kill-chain surfacing (S37-03)
                var mount = new CatalogMount("u1", "vls-fwd", MountType: "vls", ReviewState: CatalogReviewStates.Approved);
                var mountBatch = gate.ProposeMountBatch([mount], "agent", "s37-full-chain");
                Assert.That(gate.ApproveBatch(mountBatch, "human", "qa").Committed, Is.True);

                var magazine = new CatalogMagazineEntry("u1", "default", "vls-fwd", CatalogWeaponIds.MvpDefault, 4);
                var magBatch = gate.ProposeMagazineBatch([magazine], "agent", "s37-full-chain");
                Assert.That(gate.ApproveBatch(magBatch, "human", "qa").Committed, Is.True);
            }

            using var writer = new StringWriter();
            Assert.That(CatalogDependencyGraphCommand.Run(dbPath, writer), Is.EqualTo(0));

            using var doc = JsonDocument.Parse(writer.ToString());
            var root = doc.RootElement;
            Assert.That(root.GetProperty("ok").GetBoolean(), Is.True);
            Assert.That(root.GetProperty("verb").GetString(), Is.EqualTo("catalog_dependency_graph"));
            Assert.That(root.GetProperty("edgeCount").GetInt32() >= 1, Is.True);

            // S37-03 golden assertions: full kill-chain surfaced + all chain types
            Assert.That(root.GetProperty("fullKillChainSurfaced").GetBoolean(), Is.True);
            var chainTypes = root.GetProperty("chainTypes").EnumerateArray().Select(e => e.GetString()).ToArray();
            Assert.That(chainTypes, Does.Contain("mount"));
            Assert.That(chainTypes, Does.Contain("weapon"));
            Assert.That(chainTypes, Does.Contain("sensor"));
            Assert.That(chainTypes, Does.Contain("link"));

            var lines = root.GetProperty("canonicalLines").EnumerateArray()
                .Select(e => e.GetString()!)
                .ToArray();
            Assert.That(lines, Is.EqualTo(lines.OrderBy(l => l, StringComparer.Ordinal).ToArray()));
            Assert.That(lines.Any(l => l.StartsWith("link:", StringComparison.Ordinal)), Is.True);  // S36-04 AC
            // S37-03: full chains include weapon/mount/sensor lines (unchanged format)
            Assert.That(lines.Any(l => l.StartsWith("weapon:", StringComparison.Ordinal) || l.StartsWith("mount:", StringComparison.Ordinal) || l.StartsWith("sensor:", StringComparison.Ordinal)), Is.True);
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Test]
    public void DependencyGraph_reported_database_path_reflects_default_source_when_no_db_passed()
    {
        // CLI-12: an explicit missing --db is NOT_FOUND (CatalogMissingDbCliTests); no --db keeps default resolution.
        using var writer = new StringWriter();
        Assert.That(CatalogDependencyGraphCommand.Run(null, writer), Is.EqualTo(0));

        using var doc = JsonDocument.Parse(writer.ToString());
        var reportedPath = doc.RootElement.GetProperty("databasePath").GetString();

        Assert.That(reportedPath, Is.EqualTo(CatalogReaderFactory.ResolveBalticPatrolDatabasePath()));
    }

    [Test]
    public void DependencyGraph_help_text_is_registered()
    {
        using var writer = new StringWriter();
        CatalogDependencyGraphCommand.PrintHelp(writer);
        var help = writer.ToString();
        Assert.That(help, Does.Contain("catalog_dependency_graph"));
        Assert.That(help, Does.Contain("Read-only"));
        Assert.That(help, Does.Contain("full kill-chain"));  // S37-03
        Assert.That(help, Does.Contain("platform→link + weapon→mount→sensor"));  // S37-03 full chains
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