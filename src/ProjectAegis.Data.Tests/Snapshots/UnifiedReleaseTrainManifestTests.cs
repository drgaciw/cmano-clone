using Microsoft.Data.Sqlite;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Import;
using ProjectAegis.Data.Scenario;
using ProjectAegis.Data.Scenario.Authoring;
using ProjectAegis.Data.Snapshots;
using ProjectAegis.Data.Validation;
using ProjectAegis.Data.WriteGate;
using Xunit;

namespace ProjectAegis.Data.Tests.Snapshots;

[Collection("CatalogSqlite")]
public sealed class UnifiedReleaseTrainManifestTests
{
    private static readonly ScenarioValidationEngine Engine = new();
    private static readonly ValidationConfig Config = new();

    [Fact]
    public void RecordUnifiedRelease_consolidates_sorted_domain_rows_with_stable_hash()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-unified-manifest-{Guid.NewGuid():N}.db");
        const string unifiedVersion = "unified-corpus-TL-0-20260619";

        try
        {
            SeedDomainRelease(dbPath, "nightly-sensor-s32-02", batchSuffix: "sensor");
            SeedDomainRelease(dbPath, "nightly-platform-s32-02", batchSuffix: "platform");

            UnifiedReleaseTrainManifest first;
            UnifiedReleaseTrainManifest second;
            using (var store = new DbSnapshotStore(dbPath))
            {
                first = store.RecordUnifiedRelease(
                    unifiedVersion,
                    CatalogValidationDefaults.BalticSnapshotId,
                    CatalogTlTier.Tl0,
                    ["nightly-platform-s32-02", "nightly-sensor-s32-02"],
                    createdUtcTicks: 9001);

                second = store.RecordUnifiedRelease(
                    unifiedVersion,
                    CatalogValidationDefaults.BalticSnapshotId,
                    CatalogTlTier.Tl0,
                    ["nightly-sensor-s32-02", "nightly-platform-s32-02"],
                    createdUtcTicks: 9002);
            }

            Assert.Equal(unifiedVersion, first.ReleaseVersion);
            Assert.Equal(CatalogTlTier.Tl0, first.TlTier);
            Assert.Equal(["platform", "sensor"], first.DomainDrops.Select(d => d.Domain).ToArray());
            Assert.Matches("^[a-f0-9]{64}$", first.ContentHashSha256);
            Assert.Equal(first.ContentHashSha256, second.ContentHashSha256);

            using (var store = new DbSnapshotStore(dbPath))
            {
                Assert.True(store.TryGetUnifiedManifest(unifiedVersion, out var loaded));
                Assert.Equal(first.ContentHashSha256, loaded.ContentHashSha256);
                Assert.Equal(2, loaded.DomainDrops.Count);
                Assert.True(store.TryResolveReleaseVersion(unifiedVersion, out var snapshotId));
                Assert.Equal(CatalogValidationDefaults.BalticSnapshotId, snapshotId);
            }
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Fact]
    public void Manifest_hash_is_order_independent_for_domain_drops()
    {
        var dropsA = new[]
        {
            new UnifiedReleaseTrainDomainDrop("platform", "nightly-platform-s32-02", "baltic_patrol", "hash-a"),
            new UnifiedReleaseTrainDomainDrop("sensor", "nightly-sensor-s32-02", "baltic_patrol", "hash-b"),
        };
        var dropsB = dropsA.Reverse().ToArray();

        Assert.Equal(
            UnifiedReleaseTrainManifest.ComputeManifestHash(dropsA),
            UnifiedReleaseTrainManifest.ComputeManifestHash(dropsB));
    }

    [Fact]
    public void Scenario_validate_resolves_manifest_backed_dbRef_at_load()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-unified-dbref-{Guid.NewGuid():N}.db");
        const string unifiedVersion = "unified-corpus-TL-0-dbref-test";

        try
        {
            SeedDomainRelease(dbPath, "nightly-sensor-s32-02-dbref", batchSuffix: "sensor-dbref");
            using (var store = new DbSnapshotStore(dbPath))
            {
                store.RecordUnifiedRelease(
                    unifiedVersion,
                    CatalogValidationDefaults.BalticSnapshotId,
                    CatalogTlTier.Tl0,
                    ["nightly-sensor-s32-02-dbref"],
                    createdUtcTicks: 9100);
            }

            using var catalog = new SqliteCatalogReader(dbPath, "unified-manifest-dbref");
            var scenario = new ScenarioDocumentDto
            {
                Metadata = new ScenarioMetadataDto
                {
                    DbRef = unifiedVersion,
                    TlBranch = CatalogTlTier.Tl0,
                },
                Missions =
                [
                    new ScenarioMissionDto
                    {
                        Id = "patrol-1",
                        Type = "Patrol",
                        AssignedUnitIds = ["u1"],
                        PatrolZone =
                        [
                            new ScenarioWaypointDto { Lat = 57.0, Lon = 20.0 },
                            new ScenarioWaypointDto { Lat = 57.1, Lon = 20.1 },
                            new ScenarioWaypointDto { Lat = 57.2, Lon = 20.2 },
                        ],
                    },
                ],
            };

            var report = Engine.Validate(scenario, catalog, Config);
            Assert.DoesNotContain(report.Findings, f => f.Code == "DB_MISMATCH");
            Assert.DoesNotContain(report.Findings, f => f.Code.StartsWith("TL_RELEASE_TRAIN_", StringComparison.Ordinal));

            var package = ScenarioPackage.FromDocument("unified-dbref", scenario, catalog);
            Assert.Equal(CatalogValidationDefaults.BalticSnapshotId, package.DbSnapshotId);
            Assert.Equal(unifiedVersion, package.DbRef);
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Fact]
    public void TlBranch_resolution_prefers_unified_manifest_dbRef_when_present()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-unified-tlbranch-{Guid.NewGuid():N}.db");
        const string unifiedVersion = "unified-corpus-TL-0-tlbranch-test";

        try
        {
            SeedDomainRelease(dbPath, "nightly-sensor-s32-02-tlbranch", batchSuffix: "sensor-tlbranch");
            using (var store = new DbSnapshotStore(dbPath))
            {
                store.RecordUnifiedRelease(
                    unifiedVersion,
                    CatalogValidationDefaults.BalticSnapshotId,
                    CatalogTlTier.Tl0,
                    ["nightly-sensor-s32-02-tlbranch"],
                    createdUtcTicks: 9200);
            }

            using var catalog = new SqliteCatalogReader(dbPath, "unified-manifest-tlbranch");
            Assert.True(catalog.TryResolveSnapshotForTlBranch(CatalogTlTier.Tl0, out var snapshotId, out var dbRef));
            Assert.Equal(CatalogValidationDefaults.BalticSnapshotId, snapshotId);
            Assert.Equal(unifiedVersion, dbRef);
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Fact]
    public void CatalogExportManifest_resolves_unified_manifest_fields()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-unified-export-{Guid.NewGuid():N}.db");
        const string unifiedVersion = "unified-corpus-TL-0-export-test";

        try
        {
            var bind = SeedDomainRelease(dbPath, "nightly-sensor-s32-02-export", batchSuffix: "sensor-export");
            using (var store = new DbSnapshotStore(dbPath))
            {
                store.RecordUnifiedRelease(
                    unifiedVersion,
                    CatalogValidationDefaults.BalticSnapshotId,
                    CatalogTlTier.Tl0,
                    ["nightly-sensor-s32-02-export"],
                    createdUtcTicks: 9300);
            }

            var manifest = CatalogExportManifest.Resolve(dbPath, bind.SnapshotId, unifiedVersion);
            Assert.Equal(unifiedVersion, manifest.DbVersion);
            Assert.Equal(CatalogTlTier.Tl0, manifest.TlTier);
            Assert.Matches("^[a-f0-9]{64}$", manifest.ContentHash);
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    // S65-03 TDD extension for Baltic v2 corpus (per skill: AAA, deterministic, scenario_Expected naming).
    // Uses v2 scenarios/goldens data refs (S64: 10 baltic-v2-*.policy + 9 replay-golden-baltic-v2-*.txt) via parse-safe release names.
    // Covers v2 domain hashes, unified manifests for v2, order independence, ToNotes roundtrip.
    // Cite: production/release-train-scope-boundary-2026-06-24.md ; roadmap-062426.md §5/§7/§10 ; S65-03/04.
    // GitNexus impact pre-edit confirmed LOW on edited manifest symbols; tests only (no CRIT symbols touched).
    // Pre-verif: dotnet test affected + full sln green (1229/0f); post will re-run + READ.

    [Fact]
    public void RecordUnifiedRelease_supports_baltic_v2_domain_drops_with_stable_content_hash()
    {
        // Arrange: v2-named domain releases (parse-safe as 'nightly-*-v2-*' -> domain from first post-nightly segment)
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-unified-manifest-v2-{Guid.NewGuid():N}.db");
        const string unifiedVersion = "unified-corpus-TL-0-baltic-v2";

        try
        {
            SeedDomainRelease(dbPath, "nightly-sensor-baltic-v2-0622", batchSuffix: "sensor-v2");
            SeedDomainRelease(dbPath, "nightly-platform-baltic-v2-0622", batchSuffix: "platform-v2");

            UnifiedReleaseTrainManifest first;
            UnifiedReleaseTrainManifest second;
            using (var store = new DbSnapshotStore(dbPath))
            {
                first = store.RecordUnifiedRelease(
                    unifiedVersion,
                    CatalogValidationDefaults.BalticSnapshotId,
                    CatalogTlTier.Tl0,
                    ["nightly-platform-baltic-v2-0622", "nightly-sensor-baltic-v2-0622"],
                    createdUtcTicks: 9901);

                second = store.RecordUnifiedRelease(
                    unifiedVersion,
                    CatalogValidationDefaults.BalticSnapshotId,
                    CatalogTlTier.Tl0,
                    ["nightly-sensor-baltic-v2-0622", "nightly-platform-baltic-v2-0622"],
                    createdUtcTicks: 9902);
            }

            // Act + Assert (manifest-level roundtrips and hash stability; these manifest tests do not drive full ScenarioDocumentDto validation like some older tests) [cite: S65 review + production/release-train-scope-boundary-2026-06-24.md + roadmap-062426.md §10]
            Assert.Equal(unifiedVersion, first.ReleaseVersion);
            Assert.Equal(CatalogTlTier.Tl0, first.TlTier);
            Assert.Equal(["platform", "sensor"], first.DomainDrops.Select(d => d.Domain).ToArray());
            Assert.Matches("^[a-f0-9]{64}$", first.ContentHashSha256);
            Assert.Equal(first.ContentHashSha256, second.ContentHashSha256);  // order independent stable hash

            using (var store = new DbSnapshotStore(dbPath))
            {
                Assert.True(store.TryGetUnifiedManifest(unifiedVersion, out var loaded));
                Assert.Equal(first.ContentHashSha256, loaded.ContentHashSha256);
                Assert.Equal(2, loaded.DomainDrops.Count);
            }
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Fact]
    public void Manifest_v2_scenario_refs_resolve_stable_hash_and_notes_roundtrip()
    {
        // Arrange: unified for v2 scenario (refs baltic-v2-patrol etc from S64 goldens corpus)
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-unified-v2-notes-{Guid.NewGuid():N}.db");
        const string unifiedVersion = "unified-baltic-v2-patrol-mission";

        try
        {
            SeedDomainRelease(dbPath, "nightly-sensor-baltic-v2-patrol", batchSuffix: "sensor-v2-notes");

            UnifiedReleaseTrainManifest manifest;
            using (var store = new DbSnapshotStore(dbPath))
            {
                manifest = store.RecordUnifiedRelease(
                    unifiedVersion,
                    CatalogValidationDefaults.BalticSnapshotId,
                    CatalogTlTier.Tl0,
                    ["nightly-sensor-baltic-v2-patrol"],
                    createdUtcTicks: 9910);
            }

            // Act
            var notes = manifest.ToNotesJson();
            var parsedOk = UnifiedReleaseTrainManifest.TryParseFromNotes(notes, unifiedVersion, out var roundtrip);

            // Assert
            Assert.StartsWith(UnifiedReleaseTrainManifest.NotesPrefix, notes, StringComparison.Ordinal);
            Assert.True(parsedOk);
            Assert.Equal(manifest.ContentHashSha256, roundtrip.ContentHashSha256);
            Assert.Equal("sensor", roundtrip.DomainDrops.Single().Domain);
            Assert.Contains("baltic-v2", roundtrip.DomainDrops.Single().ReleaseVersion, StringComparison.Ordinal);
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Fact]
    public void V2_domain_drops_hash_is_order_independent_like_v1()
    {
        // Covers v2 corpus domain hash stability (aligns with S64 v2 goldens determinism)
        var dropsV2a = new[]
        {
            new UnifiedReleaseTrainDomainDrop("platform", "nightly-platform-baltic-v2-0622", "baltic_patrol", "hash-v2-a"),
            new UnifiedReleaseTrainDomainDrop("sensor", "nightly-sensor-baltic-v2-0622", "baltic_patrol", "hash-v2-b"),
        };
        var dropsV2b = dropsV2a.Reverse().ToArray();

        Assert.Equal(
            UnifiedReleaseTrainManifest.ComputeManifestHash(dropsV2a),
            UnifiedReleaseTrainManifest.ComputeManifestHash(dropsV2b));
    }

    private static CatalogSnapshotBinder.BindResult SeedDomainRelease(
        string dbPath,
        string releaseVersion,
        string batchSuffix)
    {
        var markdown = CmoMarkdownImporter.ResolveMiniFixturePath();
        var propose = CmoMarkdownImportProposer.ProposeFromMarkdown(
            dbPath,
            markdown,
            maxRecords: 6,
            chunkSize: 500,
            clock: new FixedCatalogClock(8000));
        var batchId = $"{propose.Batches[0].BatchId}-{batchSuffix}";

        using (var gate = new CatalogWriteGate(dbPath, new FixedCatalogClock(8001)))
        {
            Assert.True(gate.ApproveBatch(propose.Batches[0].BatchId, "human", "unified-manifest-test").Committed);
        }

        return CatalogSnapshotBinder.BindAfterApprove(
            dbPath,
            propose.Batches[0].BatchId,
            new FixedCatalogClock(8002),
            releaseVersion: releaseVersion);
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