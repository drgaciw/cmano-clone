using ProjectAegis.Data.Catalog;
using ProjectAegis.Sim.Scenario;
using Xunit;

namespace ProjectAegis.Sim.Tests.Scenario;

public sealed class DatalinkShareLagResolverTests
{
    private static readonly ScenarioDatalinkDoctrine SharingDoctrine = new(
        OrganicOnly: false,
        UnitSides: new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["u1"] = "blue",
            ["u2"] = "blue",
        });

    [Fact]
    public void Explicit_shareLagTicks_in_json_wins_over_catalog()
    {
        var repoRoot = FindRepoRoot();
        Assert.NotNull(repoRoot);
        var path = Path.Combine(repoRoot!, "data", "scenarios", "baltic-patrol-datalink-lag.policy.json");
        var profile = ScenarioPolicyJsonLoader.LoadFromFile(path);
        var catalog = InMemoryCatalogReader.BalticPatrolFixture();

        var resolved = DatalinkShareLagResolver.Resolve(profile.DatalinkDoctrine, catalog);

        Assert.True(profile.DatalinkDoctrine.ShareLagTicksSpecified);
        Assert.Equal(2, resolved.ShareLagTicks);
        Assert.Equal(profile.DatalinkDoctrine, resolved);
    }

    [Fact]
    public void Catalog_latency_applied_when_sharing_enabled_and_shareLagTicks_omitted()
    {
        var repoRoot = FindRepoRoot();
        Assert.NotNull(repoRoot);
        var path = Path.Combine(repoRoot!, "data", "scenarios", "baltic-patrol-datalink.policy.json");
        var profile = ScenarioPolicyJsonLoader.LoadFromFile(path);
        var catalog = InMemoryCatalogReader.BalticPatrolFixture();

        Assert.False(profile.DatalinkDoctrine.ShareLagTicksSpecified);
        Assert.Equal(0, profile.DatalinkDoctrine.ShareLagTicks);
        Assert.True(profile.DatalinkDoctrine.IsSharingEnabled);

        var resolved = DatalinkShareLagResolver.Resolve(profile.DatalinkDoctrine, catalog);

        Assert.Equal(3, resolved.ShareLagTicks);
    }

    [Fact]
    public void Missing_link_keeps_default_zero_share_lag()
    {
        var catalog = new InMemoryCatalogReader(
            [new CatalogSensorBinding("u1", "radar-1", 1.0, "fixture")]);

        var resolved = DatalinkShareLagResolver.Resolve(SharingDoctrine, catalog);

        Assert.Equal(0, resolved.ShareLagTicks);
        Assert.Equal(SharingDoctrine, resolved);
    }

    [Fact]
    public void Production_baltic_patrol_without_datalink_section_unchanged()
    {
        var repoRoot = FindRepoRoot();
        Assert.NotNull(repoRoot);
        var path = Path.Combine(repoRoot!, "data", "scenarios", "baltic-patrol.policy.json");
        var profile = ScenarioPolicyJsonLoader.LoadFromFile(path);
        var catalog = InMemoryCatalogReader.BalticPatrolFixture();

        Assert.False(profile.DatalinkDoctrine.IsSharingEnabled);
        Assert.Equal(ScenarioDatalinkDoctrine.Default, profile.DatalinkDoctrine);

        var resolved = DatalinkShareLagResolver.Resolve(profile.DatalinkDoctrine, catalog);

        Assert.Equal(0, resolved.ShareLagTicks);
        Assert.False(resolved.ShareLagTicksSpecified);
        Assert.Equal(profile.DatalinkDoctrine, resolved);
    }

    [Fact]
    public void Primary_link_prefers_first_sorted_comms_binding_over_link_catalog()
    {
        var catalog = new InMemoryCatalogReader(
            [new CatalogSensorBinding("u1", "radar-1", 1.0, "fixture")],
            comms: [new CatalogCommsBinding("u1", "SATCOM_B")],
            links: CatalogValidationDefaults.BalticLinks());

        var resolved = DatalinkShareLagResolver.Resolve(SharingDoctrine, catalog);

        Assert.Equal(15, resolved.ShareLagTicks);
    }

    private static string? FindRepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 10; i++)
        {
            if (File.Exists(Path.Combine(dir, "data", "scenarios", "baltic-patrol.policy.json")))
            {
                return dir;
            }

            dir = Path.GetDirectoryName(dir) ?? dir;
        }

        return null;
    }
}