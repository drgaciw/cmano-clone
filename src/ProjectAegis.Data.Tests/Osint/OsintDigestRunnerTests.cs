using Microsoft.Data.Sqlite;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Osint;
using ProjectAegis.Data.Osint.Connectors;
using ProjectAegis.Data.WriteGate;
using NUnit.Framework;

namespace ProjectAegis.Data.Tests.Osint;

[Runner.CatalogSqliteCollection]
public sealed class OsintDigestRunnerTests
{
    [Test]
    public void DigestRunner_WithFixedDiscoveries_ProducesProposalsAndLogOnly()
    {
        var discoveries = new[]
        {
            new OsintDiscoveryRecord("hypersonic-x", "https://ex/h", "hs boost glide", 0.72, "09", 8),
            new OsintDiscoveryRecord("low-conf-y", "https://ex/l", "spec", 0.50, "10", 3),
        };
        var runner = new OsintDigestRunner(0.65);
        var (proposals, logOnly) = runner.Run(discoveries);
        Assert.That(proposals, Is.Not.Empty);
        // TDD-GREEN (xUnit2012 fix for RC1): replaced Assert.True(coll.Any(pred)) with Assert.Contains(coll, pred)
        // per analyzer rule https://xunit.net/xunit.analyzers/rules/xUnit2012. Behavior preserved (exact match).
        // Cites: S41 ack ("i provide the ack") in production/gate-checks/scope-expansion-decision-2026-06-20-S41-close.md;
        // release-enablement-scope-boundary-2026-06-20.md (B3 Osint audit, extend-only CatalogWriteGate, GitNexus first);
        // prior CatalogSensorBinding extension fix (pattern for safe catalog/osint surface changes);
        // docs/adr/s41-structural-debt-decision-telemetry-osint.md (Osint 68% cohesion, GitNexus impact MEDIUM pre-edit).
        // ZERO DelegationBridge touched; deterministic replay hashes preserved.
        Assert.That(logOnly.Any(r => r.CanonicalId == "low-conf-y"), Is.True);
        foreach (var p in proposals)
{
Assert.That(p.RelevanceScore >= 0.65, Is.True);
}
        var (p2, l2) = runner.Run(discoveries);
        Assert.That(p2.Select(p => p.CanonicalId), Is.EqualTo(proposals.Select(p => p.CanonicalId)));
        Assert.That(l2.Select(r => r.CanonicalId), Is.EqualTo(logOnly.Select(r => r.CanonicalId)));
    }

    [Test]
    public void DigestRunner_EmptyInput_EmptyResults()
    {
        var runner = new OsintDigestRunner();
        var (proposals, logOnly) = runner.Run(Array.Empty<OsintDiscoveryRecord>());
        Assert.That(proposals, Is.Empty);
        Assert.That(logOnly, Is.Empty);
    }

    [Test]
    public void InMemoryConnector_FetchesAndFeedsRunner()
    {
        var connector = new InMemoryOsintConnector();
        var records = connector.Fetch();
        Assert.That(records, Is.Not.Empty);

        var runner = new OsintDigestRunner(0.65);
        var (proposals, logOnly) = runner.Run(records);
        Assert.That(proposals, Is.Not.Empty);
        Assert.That(logOnly.Any(r => r.CanonicalId.Contains("low", StringComparison.OrdinalIgnoreCase) || r.RelevanceScore < 0.65), Is.True);
    }

    [Test]
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

            var bindings = OsintCatalogMapper.ToSensorBindings(proposals, "osint-s19")
                .Select(b => b with { ReviewState = CatalogReviewStates.Approved })
                .ToList();

            using (var gate = new CatalogWriteGate(dbPath, new FixedCatalogClock(4242)))
            {
                var batchId = gate.ProposeSensorBatch(bindings, "osint-digest", "s19-test");
                var decision = gate.ApproveBatch(batchId, "human", "s19-reviewer");
                Assert.That(decision.Committed, Is.True);
            }

            using var reader = new SqliteCatalogReader(dbPath, "osint-e2e");
            var anyVisible = bindings.Any(b => reader.TryGetBasePd(b.PlatformId, b.SensorId, out _));
            Assert.That(anyVisible, Is.True, "At least one OSINT-derived sensor should be committed and readable");
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

    [Test]
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

            Assert.That(result.ParsedTotal, Is.EqualTo(5));
            Assert.That(result.ProposalCount, Is.EqualTo(3));
            Assert.That(result.LogOnlyCount, Is.EqualTo(1));
            Assert.That(result.BatchId, Is.Not.Null);

            using var gate = new CatalogWriteGate(dbPath, new FixedCatalogClock(43));
            var pending = gate.ListPendingBatches();
            Assert.That(pending, Has.Exactly(1).Items);
            Assert.That(pending[0].BatchId, Is.EqualTo(result.BatchId));
            Assert.That(pending[0].RecordCount, Is.EqualTo(3));

            using var reader = new SqliteCatalogReader(dbPath, "osint-staging");
            Assert.That(reader.TryGetBasePd("u-hypersonic", "radar-glide", out _), Is.False);
            Assert.That(reader.TryGetBasePd("speculative-rail-c", "speculative-rail-c", out _), Is.False);
            Assert.That(reader.TryGetBasePd("dedupe-target", "dedupe-target", out _), Is.False);
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

    [Test]
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

            Assert.That(result.ParsedTotal, Is.EqualTo(0));
            Assert.That(result.ProposalCount, Is.EqualTo(0));
            Assert.That(result.LogOnlyCount, Is.EqualTo(0));
            Assert.That(result.BatchId, Is.Null);
            Assert.That(File.Exists(dbPath), Is.False);
        }
        finally
        {
            if (File.Exists(digestPath))
            {
                File.Delete(digestPath);
            }
        }
    }

    [Test]
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

            Assert.That(result.ParsedTotal, Is.EqualTo(2));
            Assert.That(result.ProposalCount, Is.EqualTo(0));
            Assert.That(result.LogOnlyCount, Is.EqualTo(2));
            Assert.That(result.BatchId, Is.Null);
            Assert.That(File.Exists(dbPath), Is.False);
        }
        finally
        {
            if (File.Exists(digestPath))
            {
                File.Delete(digestPath);
            }
        }
    }

    [Test]
    public void DedupeDiscoveries_keeps_higher_relevance_and_tie_breaks_source_url()
    {
        var hits = new[]
        {
            new OsintDiscoveryRecord("dup", "https://example.org/z", "z", 0.7, "09", 5),
            new OsintDiscoveryRecord("dup", "https://example.org/a", "a", 0.7, "09", 5),
            new OsintDiscoveryRecord("dup", "https://example.org/m", "m", 0.8, "09", 6),
        };

        var deduped = OsintDigestRunner.DedupeDiscoveries(hits);

        Assert.That(deduped, Has.Exactly(1).Items);
        Assert.That(deduped[0].SourceUrl, Is.EqualTo("https://example.org/m"));
        Assert.That(deduped[0].RelevanceScore, Is.EqualTo(0.8));
    }

    [Test]
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

        Assert.That(bindings.Length, Is.EqualTo(2));
        Assert.That(bindings[0].PlatformId, Is.EqualTo("flat-id"));
        Assert.That(bindings[0].SensorId, Is.EqualTo("flat-id"));
        Assert.That(bindings[1].PlatformId, Is.EqualTo("u-hypersonic"));
        Assert.That(bindings[1].SensorId, Is.EqualTo("radar-glide"));
        Assert.That(bindings[1].Confidence, Is.EqualTo(0.72));
        Assert.That(bindings[1].TrlLevel, Is.EqualTo(6));
        Assert.That(bindings[1].CitationRef, Is.EqualTo("https://example.org/a"));
        Assert.That(bindings[1].ReviewState, Is.EqualTo(CatalogReviewStates.Provisional));
        Assert.That(bindings[0].SourceFile, Is.EqualTo("osint-digest-fixture.json"));
        Assert.That(bindings[1].SourceFile, Is.EqualTo("osint-digest-fixture.json"));
    }

    [Test]
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
            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.GetString(0), Is.EqualTo("a-first"));
            Assert.That(reader.GetString(1), Is.EqualTo("a-first"));
            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.GetString(0), Is.EqualTo("z-last"));
            Assert.That(reader.GetString(1), Is.EqualTo("z-last"));
            Assert.That(reader.Read(), Is.False);
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

    [Test]
    public void EnableRealtimeSocialStream_remains_false_for_mvp()
    {
        Assert.That(OsintDigestRunner.EnableRealtimeSocialStream, Is.False);
    }

    private static string WriteDigest(string json)
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-osint-digest-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, json);
        return path;
    }
}