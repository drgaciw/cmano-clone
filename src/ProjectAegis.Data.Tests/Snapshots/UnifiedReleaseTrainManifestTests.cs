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