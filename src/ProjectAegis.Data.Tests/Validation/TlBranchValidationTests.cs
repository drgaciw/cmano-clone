namespace ProjectAegis.Data.Tests.Validation;

using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Scenario;
using ProjectAegis.Data.Scenario.Authoring;
using ProjectAegis.Data.Validation;
using Xunit;

public sealed class TlBranchValidationTests
{
    private static readonly ScenarioValidationEngine Engine = new();
    private static readonly ValidationConfig Config = new();

    [Theory]
    [InlineData("TL-0")]
    [InlineData("TL-3")]
    [InlineData("TL-5")]
    public void Valid_tlBranch_passes_when_snapshot_branch_matches(string tlBranch)
    {
        var scenario = new ScenarioDocumentDto
        {
            Metadata = new ScenarioMetadataDto
            {
                DbRef = "baltic_patrol",
                TlBranch = tlBranch,
            },
        };

        var catalog = new BranchCatalogFixture(tlBranch);
        var report = Engine.Validate(scenario, catalog, Config);
        Assert.DoesNotContain(report.Findings, f => f.Code.StartsWith("TL_BRANCH_", StringComparison.Ordinal));
    }

    [Fact]
    public void Missing_tlBranch_emits_TL_BRANCH_MISSING()
    {
        var scenario = new ScenarioDocumentDto
        {
            Metadata = new ScenarioMetadataDto { DbRef = "baltic_patrol" },
        };

        var report = Engine.Validate(scenario, InMemoryCatalogReader.BalticPatrolFixture(), Config);
        var finding = Assert.Single(report.Findings, f => f.Code == "TL_BRANCH_MISSING");
        Assert.NotNull(finding.Data);
        Assert.Equal("tlBranch", finding.Data!["field"]);
        Assert.False(report.CanExport(Config));
    }

    [Fact]
    public void Invalid_tlBranch_emits_TL_BRANCH_INVALID()
    {
        var scenario = new ScenarioDocumentDto
        {
            Metadata = new ScenarioMetadataDto
            {
                DbRef = "baltic_patrol",
                TlBranch = "TL-9",
            },
        };

        var report = Engine.Validate(scenario, InMemoryCatalogReader.BalticPatrolFixture(), Config);
        var finding = Assert.Single(report.Findings, f => f.Code == "TL_BRANCH_INVALID");
        Assert.Contains("TL-9", finding.Message, StringComparison.Ordinal);
        Assert.False(report.CanExport(Config));
    }

    [Fact]
    public void Snapshot_branch_mismatch_emits_TL_BRANCH_SNAPSHOT_MISMATCH()
    {
        var scenario = new ScenarioDocumentDto
        {
            Metadata = new ScenarioMetadataDto
            {
                DbRef = "baltic_patrol",
                TlBranch = "TL-2",
            },
        };

        var catalog = new BranchCatalogFixture(CatalogTlTier.Tl0);
        var report = Engine.Validate(scenario, catalog, Config);
        var finding = Assert.Single(report.Findings, f => f.Code == "TL_BRANCH_SNAPSHOT_MISMATCH");
        Assert.Equal("TL-2", finding.Data!["tlBranch"]);
        Assert.Equal(CatalogTlTier.Tl0, finding.Data!["snapshotBranch"]);
        Assert.False(report.CanExport(Config));
    }

    [Fact]
    public void FromDocument_binds_normalized_tlBranch_at_load()
    {
        var doc = new ScenarioDocumentDto
        {
            Metadata = new ScenarioMetadataDto
            {
                DbRef = "baltic_patrol",
                TlBranch = " TL-3 ",
            },
        };

        var package = ScenarioPackage.FromDocument("tl-bind", doc);
        Assert.Equal(CatalogTlTier.Tl3, package.TlBranch);
        Assert.Equal(ScenarioPackage.ResolveTlBranch(doc.Metadata), package.TlBranch);
    }

    private sealed class BranchCatalogFixture : ICatalogReader
    {
        private readonly string _branch;

        public BranchCatalogFixture(string branch) => _branch = branch;

        public string LayerVersion => "tl-branch-test";

        public IReadOnlyList<CatalogSensorBinding> GetSortedSensorBindings() => Array.Empty<CatalogSensorBinding>();

        public bool TryGetBasePd(string platformId, string sensorId, out double basePd)
        {
            basePd = 0;
            return false;
        }

        public bool TryResolveDbRef(string dbRef, out string resolvedSnapshotId)
        {
            resolvedSnapshotId = CatalogValidationDefaults.BalticSnapshotId;
            return true;
        }

        public bool TryGetSnapshotBranch(string snapshotId, out string branch)
        {
            branch = _branch;
            return string.Equals(snapshotId, CatalogValidationDefaults.BalticSnapshotId, StringComparison.Ordinal);
        }

        public bool TryResolveSnapshotForTlBranch(string tlBranch, out string snapshotId, out string dbRef)
        {
            snapshotId = "";
            dbRef = "";

            var normalized = CatalogTlTier.Normalize(tlBranch);
            if (!string.Equals(normalized, _branch, StringComparison.Ordinal))
            {
                return false;
            }

            snapshotId = CatalogValidationDefaults.BalticSnapshotId;
            dbRef = CatalogValidationDefaults.BalticSnapshotId;
            return true;
        }
    }
}