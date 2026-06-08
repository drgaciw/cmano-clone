using Microsoft.Data.Sqlite;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Osint;
using ProjectAegis.Data.Osint.Connectors;
using ProjectAegis.Data.WriteGate;
using Xunit;

namespace ProjectAegis.Data.Tests.Osint;

[Collection("CatalogSqlite")]
public sealed class OsintDigestRunnerTests
{
    [Fact]
    public void DigestRunner_WithFixedDiscoveries_ProducesProposalsAndLogOnly()
    {
        var discoveries = new[]
        {
            new OsintDiscoveryRecord("hypersonic-x", "https://ex/h", "hs boost glide", 0.72, "09", 8),
            new OsintDiscoveryRecord("low-conf-y", "https://ex/l", "spec", 0.50, "10", 3),
        };
        var runner = new OsintDigestRunner(0.65);
        var (proposals, logOnly) = runner.Run(discoveries);
        Assert.NotEmpty(proposals);
        Assert.True(logOnly.Any(r => r.CanonicalId == "low-conf-y"));
        Assert.All(proposals, p => Assert.True(p.RelevanceScore >= 0.65));
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
        Assert.NotEmpty(proposals);
        Assert.Contains(logOnly, r => r.CanonicalId.Contains("low", StringComparison.OrdinalIgnoreCase) || r.RelevanceScore < 0.65);
    }

    [Fact]
    public void OsintRunner_Proposals_MapAndProposeViaWriteGate_ApproveCommitsVisibleToReader()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-osint-e2e-{Guid.NewGuid():N}.db");
        try
        {
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
            var anyVisible = bindings.Any(b => reader.TryGetBasePd(b.PlatformId, b.SensorId, out _));
            Assert.True(anyVisible, "At least one OSINT-derived sensor should be committed and readable");
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            try
            {
                if (File.Exists(dbPath))
                {
                    File.Delete(dbPath);
                }
            }
            catch
            {
                // ignore lock
            }
        }
    }

    [Fact]
    public void RunFromDigestFile_stages_proposals_via_write_gate_without_commit()
    {
        var digestPath = OsintDigestRunner.ResolveFixtureDigestPath();
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-osint-{Guid.NewGuid():N}.db");

        try
        {
            var result = OsintDigestRunner.RunFromDigestFile(
                dbPath,
                digestPath,
                clock: new FixedCatalogClock(42));

            Assert.Equal(5, result.ParsedTotal);
            Assert.Equal(3, result.ProposalCount);
            Assert.Equal(1, result.LogOnlyCount);
            Assert.NotNull(result.BatchId);

            using var gate = new CatalogWriteGate(dbPath, new FixedCatalogClock(43));
            var pending = gate.ListPendingBatches();
            Assert.Single(pending);
            Assert.Equal(result.BatchId, pending[0].BatchId);
            Assert.Equal(3, pending[0].RecordCount);

            using var reader = new SqliteCatalogReader(dbPath, "osint-staging");
            Assert.False(reader.TryGetBasePd("u-hypersonic", "radar-glide", out _));
            Assert.False(reader.TryGetBasePd("speculative-rail-c", "speculative-rail-c", out _));
            Assert.False(reader.TryGetBasePd("dedupe-target", "dedupe-target", out _));
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(dbPath))
            {
                File.Delete(dbPath);
            }
        }
    }

    [Fact]
    public void RunFromDigestFile_empty_digest_skips_write_gate()
    {
        var digestPath = WriteDigest(
            """
            {
              "discoveries": []
            }
            """);
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-osint-empty-{Guid.NewGuid():N}.db");

        try
        {
            var result = OsintDigestRunner.RunFromDigestFile(
                dbPath,
                digestPath,
                clock: new FixedCatalogClock(1));

            Assert.Equal(0, result.ParsedTotal);
            Assert.Equal(0, result.ProposalCount);
            Assert.Equal(0, result.LogOnlyCount);
            Assert.Null(result.BatchId);
            Assert.False(File.Exists(dbPath));
        }
        finally
        {
            if (File.Exists(digestPath))
            {
                File.Delete(digestPath);
            }
        }
    }

    [Fact]
    public void RunFromDigestFile_all_below_threshold_is_log_only_without_write_gate()
    {
        var digestPath = WriteDigest(
            """
            {
              "discoveries": [
                {
                  "canonicalId": "below-a",
                  "sourceUrl": "https://example.org/a",
                  "snippet": "low confidence",
                  "relevanceScore": 0.5,
                  "targetDoc": "09",
                  "proposedTrl": 6
                },
                {
                  "canonicalId": "below-b",
                  "sourceUrl": "https://example.org/b",
                  "snippet": "just under threshold",
                  "relevanceScore": 0.64,
                  "targetDoc": "09",
                  "proposedTrl": 6
                }
              ]
            }
            """);
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-osint-logonly-{Guid.NewGuid():N}.db");

        try
        {
            var result = OsintDigestRunner.RunFromDigestFile(
                dbPath,
                digestPath,
                clock: new FixedCatalogClock(2));

            Assert.Equal(2, result.ParsedTotal);
            Assert.Equal(0, result.ProposalCount);
            Assert.Equal(2, result.LogOnlyCount);
            Assert.Null(result.BatchId);
            Assert.False(File.Exists(dbPath));
        }
        finally
        {
            if (File.Exists(digestPath))
            {
                File.Delete(digestPath);
            }
        }
    }

    [Fact]
    public void DedupeDiscoveries_keeps_higher_relevance_and_tie_breaks_source_url()
    {
        var hits = new[]
        {
            new OsintDiscoveryRecord("dup", "https://example.org/z", "z", 0.7, "09", 5),
            new OsintDiscoveryRecord("dup", "https://example.org/a", "a", 0.7, "09", 5),
            new OsintDiscoveryRecord("dup", "https://example.org/m", "m", 0.8, "09", 6),
        };

        var deduped = OsintDigestRunner.DedupeDiscoveries(hits);

        Assert.Single(deduped);
        Assert.Equal("https://example.org/m", deduped[0].SourceUrl);
        Assert.Equal(0.8, deduped[0].RelevanceScore);
    }

    [Fact]
    public void MapProposalsToBindings_splits_canonical_id_and_maps_provenance_fields()
    {
        var proposals = new[]
        {
            new OsintDiscoveryRecord(
                "u-hypersonic/radar-glide",
                "https://example.org/a",
                "snippet",
                0.72,
                "09",
                6),
            new OsintDiscoveryRecord(
                "flat-id",
                "https://example.org/b",
                "snippet",
                0.66,
                "10",
                4),
        };

        var bindings = OsintDigestRunner.MapProposalsToBindings(proposals, "osint-digest-fixture.json");

        Assert.Equal(2, bindings.Length);
        Assert.Equal("flat-id", bindings[0].PlatformId);
        Assert.Equal("flat-id", bindings[0].SensorId);
        Assert.Equal("u-hypersonic", bindings[1].PlatformId);
        Assert.Equal("radar-glide", bindings[1].SensorId);
        Assert.Equal(0.72, bindings[1].Confidence);
        Assert.Equal(6, bindings[1].TrlLevel);
        Assert.Equal("https://example.org/a", bindings[1].CitationRef);
        Assert.Equal(CatalogReviewStates.Provisional, bindings[1].ReviewState);
        Assert.Equal("osint-digest-fixture.json", bindings[0].SourceFile);
        Assert.Equal("osint-digest-fixture.json", bindings[1].SourceFile);
    }

    [Fact]
    public void RunFromDigestFile_preserves_deterministic_proposal_ordering()
    {
        var digestPath = WriteDigest(
            """
            {
              "discoveries": [
                {
                  "canonicalId": "z-last",
                  "sourceUrl": "https://z.example/2",
                  "snippet": "z",
                  "relevanceScore": 0.8,
                  "targetDoc": "09",
                  "proposedTrl": 7
                },
                {
                  "canonicalId": "a-first",
                  "sourceUrl": "https://a.example/1",
                  "snippet": "a",
                  "relevanceScore": 0.8,
                  "targetDoc": "09",
                  "proposedTrl": 7
                }
              ]
            }
            """);
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-osint-order-{Guid.NewGuid():N}.db");

        try
        {
            OsintDigestRunner.RunFromDigestFile(dbPath, digestPath, clock: new FixedCatalogClock(7));

            using var connection = new SqliteConnection($"Data Source={dbPath};Pooling=false");
            connection.Open();
            using var cmd = connection.CreateCommand();
            cmd.CommandText =
                """
                SELECT platform_id, sensor_id
                FROM catalog_staging_sensor
                ORDER BY platform_id ASC, sensor_id ASC
                """;
            using var reader = cmd.ExecuteReader();
            Assert.True(reader.Read());
            Assert.Equal("a-first", reader.GetString(0));
            Assert.Equal("a-first", reader.GetString(1));
            Assert.True(reader.Read());
            Assert.Equal("z-last", reader.GetString(0));
            Assert.Equal("z-last", reader.GetString(1));
            Assert.False(reader.Read());
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(dbPath))
            {
                File.Delete(dbPath);
            }

            if (File.Exists(digestPath))
            {
                File.Delete(digestPath);
            }
        }
    }

    [Fact]
    public void EnableRealtimeSocialStream_remains_false_for_mvp()
    {
        Assert.False(OsintDigestRunner.EnableRealtimeSocialStream);
    }

    private static string WriteDigest(string json)
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-osint-digest-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, json);
        return path;
    }
}