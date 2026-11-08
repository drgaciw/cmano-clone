namespace ProjectAegis.Data.Tests.Validation;

using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Scenario;
using ProjectAegis.Data.Snapshots;
using ProjectAegis.Data.Scenario.Authoring;
using ProjectAegis.Data.Validation;
using Xunit;

public sealed class TlReleaseTrainValidationTests
{
    private static readonly ScenarioValidationEngine Engine = new();
    private static readonly ValidationConfig Config = new();

    [Fact]
    public void Valid_tlBranch_without_explicit_dbRef_resolves_snapshot_at_load()
    {
        var scenario = new ScenarioDocumentDto
        {
            Metadata = new ScenarioMetadataDto
            {
                TlBranch = CatalogTlTier.Tl0,
            },
        };

        var catalog = new ReleaseTrainCatalogFixture(
            releaseTrainBranch: CatalogTlTier.Tl0,
            releaseTrainSnapshotId: CatalogValidationDefaults.BalticSnapshotId);
        var report = Engine.Validate(scenario, catalog, Config);

        Assert.DoesNotContain(report.Findings, f => f.Code.StartsWith("TL_RELEASE_TRAIN_", StringComparison.Ordinal));
        Assert.DoesNotContain(report.Findings, f => f.Code.StartsWith("TL_BRANCH_", StringComparison.Ordinal));

        var package = ScenarioPackage.FromDocument("tl-release-bind", scenario, catalog);
        Assert.Equal(CatalogValidationDefaults.BalticSnapshotId, package.DbSnapshotId);
        Assert.Equal(CatalogValidationDefaults.BalticSnapshotId, package.DbRef);
    }

    [Fact]
    public void Missing_release_train_entry_emits_TL_RELEASE_TRAIN_NOT_FOUND()
    {
        var scenario = new ScenarioDocumentDto
        {
            Metadata = new ScenarioMetadataDto
            {
                TlBranch = CatalogTlTier.Tl2,
            },
        };

        var catalog = new ReleaseTrainCatalogFixture(releaseTrainBranch: null);
        var report = Engine.Validate(scenario, catalog, Config);
        var finding = Assert.Single(report.Findings, f => f.Code == "TL_RELEASE_TRAIN_NOT_FOUND");
        Assert.Equal(CatalogTlTier.Tl2, finding.Data!["tlBranch"]);
        Assert.False(report.CanExport(Config));
    }

    [Fact]
    public void Explicit_dbRef_conflicting_with_release_train_emits_TL_RELEASE_TRAIN_MISMATCH()
    {
        const string alternateSnapshot = "tl-train-alt-snapshot";
        var scenario = new ScenarioDocumentDto
        {
            Metadata = new ScenarioMetadataDto
            {
                DbRef = alternateSnapshot,
                TlBranch = CatalogTlTier.Tl0,
            },
        };

        var catalog = new ReleaseTrainCatalogFixture(
            releaseTrainBranch: CatalogTlTier.Tl0,
            releaseTrainSnapshotId: CatalogValidationDefaults.BalticSnapshotId,
            explicitDbRefSnapshotId: alternateSnapshot,
            explicitDbRefBranch: CatalogTlTier.Tl0);
        var report = Engine.Validate(scenario, catalog, Config);
        var finding = Assert.Single(report.Findings, f => f.Code == "TL_RELEASE_TRAIN_MISMATCH");
        Assert.Equal(CatalogTlTier.Tl0, finding.Data!["tlBranch"]);
        Assert.Equal(alternateSnapshot, finding.Data!["explicitSnapshot"]);
        Assert.Equal(CatalogValidationDefaults.BalticSnapshotId, finding.Data!["releaseTrainSnapshot"]);
        Assert.False(report.CanExport(Config));
    }

    [Fact]
    public void FromDocument_with_catalog_binds_resolved_snapshot_without_explicit_dbRef()
    {
        var doc = new ScenarioDocumentDto
        {
            Metadata = new ScenarioMetadataDto
            {
                TlBranch = " TL-0 ",
            },
        };

        var catalog = new ReleaseTrainCatalogFixture(
            releaseTrainBranch: CatalogTlTier.Tl0,
            releaseTrainSnapshotId: CatalogValidationDefaults.BalticSnapshotId);
        var package = ScenarioPackage.FromDocument("tl-bind", doc, catalog);

        Assert.Equal(CatalogValidationDefaults.BalticSnapshotId, package.DbSnapshotId);
        Assert.Equal(CatalogValidationDefaults.BalticSnapshotId, package.DbRef);
        Assert.Equal(CatalogTlTier.Tl0, package.TlBranch);
    }

    private sealed class ReleaseTrainCatalogFixture : ICatalogReader
    {
        private readonly string? _releaseTrainBranch;
        private readonly string? _releaseTrainSnapshotId;
        private readonly string? _explicitDbRefSnapshotId;
        private readonly string? _explicitDbRefBranch;

        public ReleaseTrainCatalogFixture(
            string? releaseTrainBranch,
            string? releaseTrainSnapshotId = null,
            string? explicitDbRefSnapshotId = null,
            string? explicitDbRefBranch = null)
        {
            _releaseTrainBranch = releaseTrainBranch;
            _releaseTrainSnapshotId = releaseTrainSnapshotId ?? CatalogValidationDefaults.BalticSnapshotId;
            _explicitDbRefSnapshotId = explicitDbRefSnapshotId;
            _explicitDbRefBranch = explicitDbRefBranch;
        }

        public string LayerVersion => "tl-release-train-test";

        public IReadOnlyList<CatalogSensorBinding> GetSortedSensorBindings() => Array.Empty<CatalogSensorBinding>();

        public bool TryGetBasePd(string platformId, string sensorId, out double basePd)
        {
            basePd = 0;
            return false;
        }

        public bool TryResolveDbRef(string dbRef, out string resolvedSnapshotId)
        {
            if (_explicitDbRefSnapshotId != null &&
                string.Equals(dbRef, _explicitDbRefSnapshotId, StringComparison.Ordinal))
            {
                resolvedSnapshotId = _explicitDbRefSnapshotId;
                return true;
            }

            if (string.Equals(dbRef, CatalogValidationDefaults.BalticSnapshotId, StringComparison.Ordinal))
            {
                resolvedSnapshotId = CatalogValidationDefaults.BalticSnapshotId;
                return true;
            }

            resolvedSnapshotId = "";
            return false;
        }

        public bool TryGetSnapshotBranch(string snapshotId, out string branch)
        {
            if (_explicitDbRefSnapshotId != null &&
                string.Equals(snapshotId, _explicitDbRefSnapshotId, StringComparison.Ordinal))
            {
                branch = _explicitDbRefBranch ?? CatalogTlTier.Default;
                return true;
            }

            if (_releaseTrainSnapshotId != null &&
                string.Equals(snapshotId, _releaseTrainSnapshotId, StringComparison.Ordinal))
            {
                branch = _releaseTrainBranch ?? CatalogTlTier.Default;
                return _releaseTrainBranch != null;
            }

            branch = CatalogTlTier.Default;
            return false;
        }

        public bool TryResolveSnapshotForTlBranch(string tlBranch, out string snapshotId, out string dbRef)
        {
            snapshotId = "";
            dbRef = "";

            if (_releaseTrainBranch == null)
            {
                return false;
            }

            var normalized = CatalogTlTier.Normalize(tlBranch);
            if (!string.Equals(normalized, _releaseTrainBranch, StringComparison.Ordinal))
            {
                return false;
            }

            snapshotId = _releaseTrainSnapshotId!;
            dbRef = CatalogReleaseTrainResolver.ToDbRef(snapshotId);
            return true;
        }
    }
}