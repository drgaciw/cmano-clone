using System.Security.Cryptography;
using Microsoft.Data.Sqlite;
using NUnit.Framework;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Osint;
using ProjectAegis.Data.WriteGate;

namespace ProjectAegis.Data.Tests.Osint;

[Runner.CatalogSqliteCollection]
public sealed class OsintStagingReviewServiceTests
{
    [Test]
    public void LoadStagingSnapshot_reads_real_confidence_citation_trl_and_fact_id_from_staging_sensor()
    {
        var dbPath = CreateTempDb();
        try
        {
            using (var gate = new CatalogWriteGate(dbPath, new FixedCatalogClock(1000)))
            {
                var binding = new CatalogSensorBinding(
                    PlatformId: "osint-platform",
                    SensorId: "osint-radar-09",
                    BasePd: 0.77,
                    SourceFactId: "osint:09",
                    Confidence: 0.77,
                    ImportBatchId: "branch:doc-09",
                    SourceFile: "digest.json",
                    ReviewState: CatalogReviewStates.Approved,
                    TrlLevel: 7,
                    ValueTier: CatalogProvenanceTier.InterpretedValue,
                    ReviewerId: "osint-digest",
                    CitationRef: "https://example.org/spec-radar");
                gate.ProposeSensorBatch(new[] { binding }, "osint", "osint-digest", "observed spec radar");
            }

            var snapshot = OsintStagingReviewService.LoadStagingSnapshot(dbPath);

            Assert.That(snapshot.SourceKind, Is.EqualTo("staging-db"));
            Assert.That(snapshot.Proposals, Has.Count.EqualTo(1));
            var row = snapshot.Proposals[0];
            Assert.That(row.Confidence, Is.EqualTo(0.77).Within(0.0001));
            Assert.That(row.CitationRef, Is.EqualTo("https://example.org/spec-radar"));
            Assert.That(row.SourceFactId, Is.EqualTo("osint:09"));
            Assert.That(row.TrlLevel, Is.EqualTo(7));
            Assert.That(row.SourceFile, Is.EqualTo("digest.json"));
            Assert.That(row.Snippet, Is.Not.Empty);
            Assert.That(row.IsLogOnly, Is.False);
            Assert.That(row.CitationRef, Does.Not.StartWith("batch://"));
        }
        finally
        {
            CleanupDb(dbPath);
        }
    }

    [Test]
    public void LoadStagingSnapshot_partitions_fixture_records_and_keeps_sub_threshold_in_log_only()
    {
        var emptyDbPath = Path.Combine(Path.GetTempPath(), $"aegis-osint-empty-{Guid.NewGuid():N}.db");
        var fixturePath = ResolveRepoFile("data", "osint_facts.json");

        var snapshot = OsintStagingReviewService.LoadStagingSnapshot(emptyDbPath, fixturePath);

        Assert.That(snapshot.SourceKind, Is.EqualTo("fixture"));
        Assert.That(snapshot.Proposals.Select(p => p.SensorId), Does.Contain("osint-hypersonic-glide-s20"));
        Assert.That(snapshot.Proposals.Select(p => p.SensorId), Does.Contain("osint-railgun-demo"));
        Assert.That(snapshot.Proposals.Select(p => p.SensorId), Does.Not.Contain("osint-low-conf-example"));
        Assert.That(snapshot.LogOnly.Select(p => p.SensorId), Does.Contain("osint-low-conf-example"));

        var logOnlyRow = snapshot.LogOnly.Single(p => p.SensorId == "osint-low-conf-example");
        Assert.That(logOnlyRow.IsLogOnly, Is.True);
        Assert.That(logOnlyRow.Confidence, Is.EqualTo(0.40).Within(0.0001));
        Assert.That(logOnlyRow.CitationRef, Is.EqualTo("https://ex.com/low"));
        Assert.That(logOnlyRow.Snippet, Is.EqualTo("below threshold"));
    }

    [Test]
    public void LoadStagingSnapshot_on_shipped_catalog_preserves_file_sha256()
    {
        var shipped = ResolveRepoFile("assets", "data", "catalog", "baltic_patrol.db");
        var tempCopy = Path.Combine(Path.GetTempPath(), $"aegis-shipped-copy-{Guid.NewGuid():N}.db");
        try
        {
            File.Copy(shipped, tempCopy, overwrite: true);
            var beforeHash = Sha256Hex(tempCopy);

            var snapshot = OsintStagingReviewService.LoadStagingSnapshot(tempCopy, fixturePath: null);

            var afterHash = Sha256Hex(tempCopy);
            Assert.That(afterHash, Is.EqualTo(beforeHash));
            Assert.That(snapshot, Is.Not.Null);
        }
        finally
        {
            CleanupDb(tempCopy);
        }
    }

    [Test]
    public void ProposeAndApprove_rejects_sub_threshold_and_missing_citation_without_touching_gate()
    {
        var dbPath = CreateTempDb();
        try
        {
            var lowConf = new OsintDiscoveryRecord("low-item", "https://ex.com/low", "below floor", 0.40, "10", 5);
            var lowResult = OsintStagingReviewService.ProposeAndApproveDiscovery(
                dbPath, lowConf, "human", "reviewer-1", clock: new FixedCatalogClock(100));

            Assert.That(lowResult.Succeeded, Is.False);
            Assert.That(lowResult.Committed, Is.False);
            Assert.That(lowResult.Errors, Does.Contain("BELOW_CONFIDENCE_THRESHOLD"));

            var noCitation = new OsintDiscoveryRecord("uncited-item", string.Empty, string.Empty, 0.85, "10", 7);
            var uncitedResult = OsintStagingReviewService.ProposeAndApproveDiscovery(
                dbPath, noCitation, "human", "reviewer-1", clock: new FixedCatalogClock(101));

            Assert.That(uncitedResult.Succeeded, Is.False);
            Assert.That(uncitedResult.Committed, Is.False);
            Assert.That(uncitedResult.Errors, Does.Contain("MISSING_CITATION"));

            using var gate = new CatalogWriteGate(dbPath, new FixedCatalogClock(102));
            Assert.That(gate.ListPendingBatches(), Is.Empty);
        }
        finally
        {
            CleanupDb(dbPath);
        }
    }

    [Test]
    public void ApproveBatch_and_RejectBatch_delegate_to_CatalogWriteGate()
    {
        var dbPath = CreateTempDb();
        try
        {
            var clock = new FixedCatalogClock(2000);
            var approvedRecord = new OsintDiscoveryRecord(
                "hypersonic-glide-s20", "https://ex.com/hg-s20", "observed boost-glide", 0.81, "10", 7);
            var stagedForApprove = OsintStagingReviewService.ProposeDiscovery(
                dbPath,
                approvedRecord,
                "osint",
                "digest",
                markApprovedForHumanReview: true,
                clock: clock);
            Assert.That(stagedForApprove.Succeeded, Is.True);

            var approveDecision = OsintStagingReviewService.ApproveBatch(
                dbPath, stagedForApprove.BatchId, "human", "reviewer-1", new FixedCatalogClock(2001));
            Assert.That(approveDecision.Succeeded, Is.True);
            Assert.That(approveDecision.Committed, Is.True);
            Assert.That(approveDecision.Rejected, Is.False);

            using (var reader = new SqliteCatalogReader(dbPath, "osint-review-check"))
            {
                Assert.That(reader.TryGetBasePd("osint-platform", "osint-hypersonic-glide-s20", out var pd), Is.True);
                Assert.That(pd, Is.EqualTo(0.81).Within(0.001));
            }

            var rejectRecord = new OsintDiscoveryRecord(
                "railgun-demo", "https://ex.com/rg-s20", "speculative railgun", 0.71, "10", 6);
            var stagedForReject = OsintStagingReviewService.ProposeDiscovery(
                dbPath,
                rejectRecord,
                "osint",
                "digest",
                markApprovedForHumanReview: true,
                clock: new FixedCatalogClock(2002));
            Assert.That(stagedForReject.Succeeded, Is.True);

            var rejectDecision = OsintStagingReviewService.RejectBatch(
                dbPath, stagedForReject.BatchId, "human", "reviewer-1", "out of scope", new FixedCatalogClock(2003));
            Assert.That(rejectDecision.Succeeded, Is.True);
            Assert.That(rejectDecision.Committed, Is.False);
            Assert.That(rejectDecision.Rejected, Is.True);
            Assert.That(rejectDecision.Errors, Is.Empty);

            var after = OsintStagingReviewService.LoadStagingSnapshot(dbPath, fixturePath: null);
            Assert.That(after.Proposals, Is.Empty);
        }
        finally
        {
            CleanupDb(dbPath);
        }
    }

    [Test]
    public void ApproveBatch_with_provisional_review_state_surfaces_quarantine_error()
    {
        var dbPath = CreateTempDb();
        try
        {
            var record = new OsintDiscoveryRecord(
                "provisional-sensor", "https://ex.com/prov", "provisional snippet", 0.82, "10", 6);
            var staged = OsintStagingReviewService.ProposeDiscovery(
                dbPath, record, "osint", "digest", markApprovedForHumanReview: false, clock: new FixedCatalogClock(3000));
            Assert.That(staged.Succeeded, Is.True);

            var decision = OsintStagingReviewService.ApproveBatch(
                dbPath, staged.BatchId, "human", "reviewer-1", new FixedCatalogClock(3001));

            Assert.That(decision.Committed, Is.False);
            Assert.That(decision.Succeeded, Is.False);
            Assert.That(decision.Errors, Has.Some.Contains("review_state_provisional"));
        }
        finally
        {
            CleanupDb(dbPath);
        }
    }

    [Test]
    public void LoadStagingSnapshot_and_ProposeDiscovery_reject_or_log_only_non_finite_and_out_of_range_confidence()
    {
        var dbPath = CreateTempDb();
        try
        {
            using (var gate = new CatalogWriteGate(dbPath, new FixedCatalogClock(4000)))
            {
                foreach (var (sensorId, conf) in new[]
                {
                    ("osint-pos-inf", double.PositiveInfinity),
                    ("osint-neg-inf", double.NegativeInfinity),
                    ("osint-negative", -0.25),
                    ("osint-over-one", 1.25),
                })
                {
                    var binding = new CatalogSensorBinding(
                        PlatformId: "osint-platform",
                        SensorId: sensorId,
                        BasePd: 0.70,
                        SourceFactId: "osint:nf",
                        Confidence: conf,
                        ImportBatchId: "branch:nf",
                        SourceFile: "digest.json",
                        ReviewState: CatalogReviewStates.Approved,
                        TrlLevel: 7,
                        ValueTier: CatalogProvenanceTier.InterpretedValue,
                        ReviewerId: "osint-digest",
                        CitationRef: "https://example.org/nf");
                    gate.ProposeSensorBatch(new[] { binding }, "osint", "osint-digest", "non-finite confidence test");
                }

                var validBinding = new CatalogSensorBinding(
                    PlatformId: "osint-platform",
                    SensorId: "osint-valid-085",
                    BasePd: 0.85,
                    SourceFactId: "osint:ok",
                    Confidence: 0.85,
                    ImportBatchId: "branch:ok",
                    SourceFile: "digest.json",
                    ReviewState: CatalogReviewStates.Approved,
                    TrlLevel: 7,
                    ValueTier: CatalogProvenanceTier.InterpretedValue,
                    ReviewerId: "osint-digest",
                    CitationRef: "https://example.org/ok");
                gate.ProposeSensorBatch(new[] { validBinding }, "osint", "osint-digest", "valid confidence test");
            }

            var snapshot = OsintStagingReviewService.LoadStagingSnapshot(dbPath);
            Assert.That(snapshot.Proposals.Select(p => p.SensorId), Is.EqualTo(new[] { "osint-valid-085" }));
            Assert.That(snapshot.LogOnly.Select(p => p.SensorId), Does.Contain("osint-pos-inf"));
            Assert.That(snapshot.LogOnly.Select(p => p.SensorId), Does.Contain("osint-neg-inf"));
            Assert.That(snapshot.LogOnly.Select(p => p.SensorId), Does.Contain("osint-negative"));
            Assert.That(snapshot.LogOnly.Select(p => p.SensorId), Does.Contain("osint-over-one"));
            Assert.That(snapshot.LogOnly.All(r => r.IsLogOnly), Is.True);

            var nanMinSnapshot = OsintStagingReviewService.LoadStagingSnapshot(
                dbPath, fixturePath: null, minimumConfidence: double.NaN);
            Assert.That(nanMinSnapshot.Proposals, Is.Empty);
            Assert.That(nanMinSnapshot.LogOnly, Has.Count.EqualTo(5));

            foreach (var badScore in new[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity, -0.1, 1.1 })
            {
                var badRecord = new OsintDiscoveryRecord("bad-conf", "https://ex.com/bad", "bad snippet", badScore, "10", 6);
                var proposeResult = OsintStagingReviewService.ProposeDiscovery(
                    dbPath, badRecord, "human", "reviewer-1", clock: new FixedCatalogClock(4001));
                Assert.That(proposeResult.Succeeded, Is.False);
                Assert.That(proposeResult.Errors, Does.Contain(OsintStagingReviewService.ReasonBelowConfidenceThreshold));
            }
        }
        finally
        {
            CleanupDb(dbPath);
        }
    }

    [Test]
    public void LoadStagingSnapshot_degrades_gracefully_on_corrupt_non_sqlite_file()
    {
        var corruptPath = Path.Combine(Path.GetTempPath(), $"aegis-corrupt-{Guid.NewGuid():N}.db");
        try
        {
            File.WriteAllText(corruptPath, "this is definitely not a sqlite database file");

            var snapshot = OsintStagingReviewService.LoadStagingSnapshot(corruptPath, fixturePath: null);
            Assert.That(snapshot.Proposals, Is.Empty);
            Assert.That(snapshot.LogOnly, Is.Empty);
            Assert.That(snapshot.SourceKind, Is.EqualTo("empty"));

            var fixturePath = ResolveRepoFile("data", "osint_facts.json");
            var fallbackSnapshot = OsintStagingReviewService.LoadStagingSnapshot(corruptPath, fixturePath);
            Assert.That(fallbackSnapshot.SourceKind, Is.EqualTo("fixture"));
            Assert.That(fallbackSnapshot.Proposals, Is.Not.Empty);
        }
        finally
        {
            CleanupDb(corruptPath);
        }
    }

    private static string CreateTempDb()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-osint-review-{Guid.NewGuid():N}.db");
        CatalogSeedBootstrap.SeedBalticPatrol(dbPath, overwrite: true);
        return dbPath;
    }

    private static void CleanupDb(string dbPath)
    {
        SqliteConnection.ClearAllPools();
        try
        {
            if (File.Exists(dbPath)) File.Delete(dbPath);
        }
        catch
        {
            // ignore cleanup lock
        }
    }

    private static string ResolveRepoFile(params string[] parts)
    {
        var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "ProjectAegis.sln")))
        {
            dir = dir.Parent;
        }

        Assert.That(dir, Is.Not.Null, "Repo root not found");
        var pathParts = new List<string> { dir!.FullName };
        pathParts.AddRange(parts);
        var path = Path.Combine(pathParts.ToArray());
        Assert.That(File.Exists(path), Is.True, path);
        return path;
    }

    private static string Sha256Hex(string path)
    {
        using var stream = File.OpenRead(path);
        var hash = SHA256.HashData(stream);
        return Convert.ToHexString(hash);
    }
}
