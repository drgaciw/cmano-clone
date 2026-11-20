namespace ProjectAegis.Data.Tests.Validation;

using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Scenario;
using ProjectAegis.Data.Scenario.Authoring;
using ProjectAegis.Data.Validation;
using NUnit.Framework;

public sealed class TlBranchValidationTests
{
    private static readonly ScenarioValidationEngine Engine = new();
    private static readonly ValidationConfig Config = new();
    [TestCase("TL-0")]
    [TestCase("TL-3")]
    [TestCase("TL-5")]
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
        Assert.That(report.Findings.Any(f => f.Code.StartsWith("TL_BRANCH_", StringComparison.Ordinal)), Is.False);
    }

    [Test]
    public void Missing_tlBranch_emits_TL_BRANCH_MISSING()
    {
        var scenario = new ScenarioDocumentDto
        {
            Metadata = new ScenarioMetadataDto { DbRef = "baltic_patrol" },
        };

        var report = Engine.Validate(scenario, InMemoryCatalogReader.BalticPatrolFixture(), Config);
        var finding = ProjectAegis.Data.Tests.AssertEx.Single(report.Findings, f => f.Code == "TL_BRANCH_MISSING");
        Assert.That(finding.Data, Is.Not.Null);
        Assert.That(finding.Data!["field"], Is.EqualTo("tlBranch"));
        Assert.That(report.CanExport(Config), Is.False);
    }

    [Test]
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
        var finding = ProjectAegis.Data.Tests.AssertEx.Single(report.Findings, f => f.Code == "TL_BRANCH_INVALID");
        Assert.That(finding.Message, Does.Contain("TL-9"));
        Assert.That(report.CanExport(Config), Is.False);
    }

    [Test]
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
        var finding = ProjectAegis.Data.Tests.AssertEx.Single(report.Findings, f => f.Code == "TL_BRANCH_SNAPSHOT_MISMATCH");
        Assert.That(finding.Data!["tlBranch"], Is.EqualTo("TL-2"));
        Assert.That(finding.Data!["snapshotBranch"], Is.EqualTo(CatalogTlTier.Tl0));
        Assert.That(report.CanExport(Config), Is.False);
    }

    [Test]
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
        Assert.That(package.TlBranch, Is.EqualTo(CatalogTlTier.Tl3));
        Assert.That(package.TlBranch, Is.EqualTo(ScenarioPackage.ResolveTlBranch(doc.Metadata)));
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