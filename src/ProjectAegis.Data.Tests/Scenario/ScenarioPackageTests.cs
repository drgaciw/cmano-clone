namespace ProjectAegis.Data.Tests.Scenario;

using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Scenario;
using ProjectAegis.Data.Scenario.Authoring;
using NUnit.Framework;

using CatalogTlTier = ProjectAegis.Data.Catalog.CatalogTlTier;

public sealed class ScenarioPackageTests
{
    [Test]
    public void FromDocument_uses_explicit_db_snapshot_id_when_present()
    {
        var doc = new ScenarioDocumentDto
        {
            Metadata = new ScenarioMetadataDto
            {
                DbSnapshotId = "catalog-p2-test-snapshot",
                PolicyId = "baltic-patrol",
            },
        };

        var package = ScenarioPackage.FromDocument("scenario_a", doc);
        Assert.That(package.DbSnapshotId, Is.EqualTo("catalog-p2-test-snapshot"));
    }

    [Test]
    public void FromDocument_binds_tlBranch_from_metadata()
    {
        var doc = new ScenarioDocumentDto
        {
            Metadata = new ScenarioMetadataDto
            {
                DbRef = "baltic_patrol",
                TlBranch = CatalogTlTier.Tl2,
            },
        };

        var package = ScenarioPackage.FromDocument("tl_scenario", doc);
        Assert.That(package.TlBranch, Is.EqualTo(CatalogTlTier.Tl2));
    }

    [Test]
    public void FromDocument_resolves_db_snapshot_from_db_ref()
    {
        var doc = new ScenarioDocumentDto
        {
            Metadata = new ScenarioMetadataDto
            {
                DbRef = "baltic_patrol",
                PolicyId = "baltic-patrol-catalog",
                Seed = 99,
            },
        };

        var package = ScenarioPackage.FromDocument("golden_clean", doc);
        Assert.That(package.ScenarioId, Is.EqualTo("golden_clean"));
        Assert.That(package.PolicyId, Is.EqualTo("baltic-patrol-catalog"));
        Assert.That(package.DbSnapshotId, Is.EqualTo(CatalogValidationDefaults.BalticSnapshotId));
        Assert.That(package.Seed, Is.EqualTo(99ul));
    }

    [Test]
    public void Loader_golden_clean_fixture_binds_policy_and_snapshot()
    {
        var path = ResolveFixture("golden_clean.json");
        if (path == null)
        {
            return;
        }

        var package = ScenarioPackageLoader.LoadFromFile(path);
        Assert.That(package.ScenarioId, Is.EqualTo("golden_clean"));
        Assert.That(package.PolicyId, Is.EqualTo("baltic-patrol-catalog"));
        Assert.That(package.DbSnapshotId, Is.EqualTo(CatalogValidationDefaults.BalticSnapshotId));
    }

    [Test]
    public void Data_policy_repository_loads_json_policy_ids()
    {
        ScenarioPolicyJsonCatalog.EnsureDefaultJsonLoaded();
        var ids = ScenarioPolicyJsonCatalog.AllIds();
        Assert.That(ids, Does.Contain("restricted-engagement"));
        Assert.That(ids, Does.Contain("baltic-patrol"));
        var dto = ScenarioPolicyJsonCatalog.TryGetJson("restricted-engagement");
        Assert.That(dto, Is.Not.Null);
        Assert.That(dto!.Id, Is.EqualTo("restricted-engagement"));
    }

    private static string? ResolveFixture(string fileName)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        for (var i = 0; i < 12; i++)
        {
            if (dir == null)
            {
                break;
            }

            var candidate = Path.Combine(
                dir.FullName,
                "assets",
                "data",
                "scenarios",
                "validation",
                fileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        return null;
    }
}