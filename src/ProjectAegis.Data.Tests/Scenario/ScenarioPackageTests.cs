namespace ProjectAegis.Data.Tests.Scenario;

using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Scenario;
using ProjectAegis.Data.Scenario.Authoring;
using Xunit;

public sealed class ScenarioPackageTests
{
    [Fact]
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
        Assert.Equal("golden_clean", package.ScenarioId);
        Assert.Equal("baltic-patrol-catalog", package.PolicyId);
        Assert.Equal(CatalogValidationDefaults.BalticSnapshotId, package.DbSnapshotId);
        Assert.Equal(99ul, package.Seed);
    }

    [Fact]
    public void Loader_golden_clean_fixture_binds_policy_and_snapshot()
    {
        var path = ResolveFixture("golden_clean.json");
        if (path == null)
        {
            return;
        }

        var package = ScenarioPackageLoader.LoadFromFile(path);
        Assert.Equal("golden_clean", package.ScenarioId);
        Assert.Equal("baltic-patrol-catalog", package.PolicyId);
        Assert.Equal(CatalogValidationDefaults.BalticSnapshotId, package.DbSnapshotId);
    }

    [Fact]
    public void Data_policy_repository_loads_json_policy_ids()
    {
        ScenarioPolicyJsonCatalog.EnsureDefaultJsonLoaded();
        var ids = ScenarioPolicyJsonCatalog.AllIds();
        Assert.Contains("restricted-engagement", ids);
        Assert.Contains("baltic-patrol", ids);
        var dto = ScenarioPolicyJsonCatalog.TryGetJson("restricted-engagement");
        Assert.NotNull(dto);
        Assert.Equal("restricted-engagement", dto!.Id);
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