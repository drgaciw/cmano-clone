using System.Text.Json;
using Microsoft.Data.Sqlite;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.WriteGate;
using ProjectAegis.MissionEditor.Cli;
using Xunit;

namespace ProjectAegis.MissionEditor.Cli.Tests;

public sealed class CatalogDependencyGraphCommandTests
{
    [Fact]
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
                Assert.True(gate.ApproveBatch(linkBatch, "human", "qa").Committed);

                var comms = new CatalogCommsBinding("u1", "NATO_TADIL_J", ReviewState: CatalogReviewStates.Approved);
                var commsBatch = gate.ProposeCommsBatch([comms], "agent", "s36-cli-test");
                Assert.True(gate.ApproveBatch(commsBatch, "human", "qa").Committed);

                // minimal mount/weapon for full kill-chain surfacing (S37-03)
                var mount = new CatalogMount("u1", "vls-fwd", MountType: "vls", ReviewState: CatalogReviewStates.Approved);
                var mountBatch = gate.ProposeMountBatch([mount], "agent", "s37-full-chain");
                Assert.True(gate.ApproveBatch(mountBatch, "human", "qa").Committed);

                var magazine = new CatalogMagazineEntry("u1", "default", "vls-fwd", CatalogWeaponIds.MvpDefault, 4);
                var magBatch = gate.ProposeMagazineBatch([magazine], "agent", "s37-full-chain");
                Assert.True(gate.ApproveBatch(magBatch, "human", "qa").Committed);
            }

            using var writer = new StringWriter();
            Assert.Equal(0, CatalogDependencyGraphCommand.Run(dbPath, writer));

            using var doc = JsonDocument.Parse(writer.ToString());
            var root = doc.RootElement;
            Assert.True(root.GetProperty("ok").GetBoolean());
            Assert.Equal("catalog_dependency_graph", root.GetProperty("verb").GetString());
            Assert.True(root.GetProperty("edgeCount").GetInt32() >= 1);

            // S37-03 golden assertions: full kill-chain surfaced + all chain types
            Assert.True(root.GetProperty("fullKillChainSurfaced").GetBoolean());
            var chainTypes = root.GetProperty("chainTypes").EnumerateArray().Select(e => e.GetString()).ToArray();
            Assert.Contains("mount", chainTypes);
            Assert.Contains("weapon", chainTypes);
            Assert.Contains("sensor", chainTypes);
            Assert.Contains("link", chainTypes);

            var lines = root.GetProperty("canonicalLines").EnumerateArray()
                .Select(e => e.GetString()!)
                .ToArray();
            Assert.Equal(lines.OrderBy(l => l, StringComparer.Ordinal).ToArray(), lines);
            Assert.Contains(lines, l => l.StartsWith("link:", StringComparison.Ordinal));  // S36-04 AC
            // S37-03: full chains include weapon/mount/sensor lines (unchanged format)
            Assert.Contains(lines, l => l.StartsWith("weapon:", StringComparison.Ordinal) || l.StartsWith("mount:", StringComparison.Ordinal) || l.StartsWith("sensor:", StringComparison.Ordinal));
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Fact]
    public void DependencyGraph_help_text_is_registered()
    {
        using var writer = new StringWriter();
        CatalogDependencyGraphCommand.PrintHelp(writer);
        var help = writer.ToString();
        Assert.Contains("catalog_dependency_graph", help);
        Assert.Contains("Read-only", help);
        Assert.Contains("full kill-chain", help);  // S37-03
        Assert.Contains("platform→link + weapon→mount→sensor", help);  // S37-03 full chains
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