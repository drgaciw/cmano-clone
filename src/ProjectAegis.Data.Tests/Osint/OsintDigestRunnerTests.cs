using Microsoft.Data.Sqlite;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Osint;
using ProjectAegis.Data.Osint.Connectors;
using ProjectAegis.Data.WriteGate;
using Xunit;

namespace ProjectAegis.Data.Tests.Osint;

public sealed class OsintDigestRunnerTests
{
    [Fact]
    public void DigestRunner_WithFixedDiscoveries_ProducesProposalsAndLogOnly()
    {
        var discoveries = new[]
        {
            new OsintDiscoveryRecord("hypersonic-x", "https://ex/h", "hs boost glide", 0.72, "09", 8),
            new OsintDiscoveryRecord("low-conf-y", "https://ex/l", "spec", 0.50, "10", 3), // below threshold -> log only
        };
        var runner = new OsintDigestRunner(0.65);
        var (proposals, logOnly) = runner.Run(discoveries);
        Assert.NotEmpty(proposals);
        Assert.True(logOnly.Any(r => r.CanonicalId == "low-conf-y"));
        Assert.All(proposals, p => Assert.True(p.RelevanceScore >= 0.65));
        // deterministic: same input same output (no time variance)
        var (p2, l2) = runner.Run(discoveries);
        Assert.Equal(proposals.Select(p => p.CanonicalId), p2.Select(p => p.CanonicalId));
    }

    [Fact]
    public void DigestRunner_EmptyInput_EmptyResults()
    {
        var runner = new OsintDigestRunner();
        var (proposals, logOnly) = runner.Run(Array.Empty<OsintDiscoveryRecord>());
        Assert.Empty(proposals);
        Assert.Empty(logOnly);
    }

    [Fact]
    public void InMemoryConnector_FetchesAndFeedsRunner()
    {
        var connector = new InMemoryOsintConnector();
        var records = connector.Fetch();
        Assert.NotEmpty(records);

        var runner = new OsintDigestRunner(0.65);
        var (proposals, logOnly) = runner.Run(records);
        Assert.NotEmpty(proposals); // at least the high relevance ones
        // low relevance in logOnly
        Assert.Contains(logOnly, r => r.CanonicalId.Contains("low", StringComparison.OrdinalIgnoreCase) || r.RelevanceScore < 0.65);
    }

    [Fact]
    public void OsintRunner_Proposals_MapAndProposeViaWriteGate_ApproveCommitsVisibleToReader()
    {
        var dbPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"aegis-osint-e2e-{Guid.NewGuid():N}.db");
        try
        {
            // Seed minimal for write gate (like other tests)
            CatalogSeedBootstrap.SeedBalticPatrol(dbPath, overwrite: true);

            var connector = new InMemoryOsintConnector();
            var records = connector.Fetch();

            var runner = new OsintDigestRunner(0.65);
            var (proposals, _) = runner.Run(records);

            var bindings = OsintCatalogMapper.ToSensorBindings(proposals, "osint-s19");

            using (var gate = new CatalogWriteGate(dbPath, new FixedCatalogClock(4242)))
            {
                var batchId = gate.ProposeSensorBatch(bindings, "osint-digest", "s19-test");
                var decision = gate.ApproveBatch(batchId, "human", "s19-reviewer");
                Assert.True(decision.Committed);
            }

            using var reader = new SqliteCatalogReader(dbPath, "osint-e2e");
            // At least one of the proposed should be visible (high relevance ones)
            var anyVisible = bindings.Any(b => reader.TryGetBasePd(b.PlatformId, b.SensorId, out _));
            Assert.True(anyVisible, "At least one OSINT-derived sensor should be committed and readable");
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            try { if (System.IO.File.Exists(dbPath)) System.IO.File.Delete(dbPath); } catch { /* ignore lock */ }
        }
    }
}
