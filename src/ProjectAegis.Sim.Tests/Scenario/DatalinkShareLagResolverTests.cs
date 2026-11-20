using ProjectAegis.Data.Catalog;
using ProjectAegis.Sim.Scenario;
using NUnit.Framework;

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

    [Test]
    public void Explicit_shareLagTicks_in_json_wins_over_catalog()
    {
        var repoRoot = FindRepoRoot();
        Assert.That(repoRoot, Is.Not.Null);
        var path = Path.Combine(repoRoot!, "data", "scenarios", "baltic-patrol-datalink-lag.policy.json");
        var profile = ScenarioPolicyJsonLoader.LoadFromFile(path);
        var catalog = InMemoryCatalogReader.BalticPatrolFixture();

        var resolved = DatalinkShareLagResolver.Resolve(profile.DatalinkDoctrine, catalog);

        Assert.That(profile.DatalinkDoctrine.ShareLagTicksSpecified, Is.True);
        Assert.That(resolved.ShareLagTicks, Is.EqualTo(2));
        Assert.That(resolved, Is.EqualTo(profile.DatalinkDoctrine));
    }

    [Test]
    public void Catalog_latency_applied_when_sharing_enabled_and_shareLagTicks_omitted()
    {
        var repoRoot = FindRepoRoot();
        Assert.That(repoRoot, Is.Not.Null);
        var path = Path.Combine(repoRoot!, "data", "scenarios", "baltic-patrol-datalink.policy.json");
        var profile = ScenarioPolicyJsonLoader.LoadFromFile(path);
        var catalog = InMemoryCatalogReader.BalticPatrolFixture();

        Assert.That(profile.DatalinkDoctrine.ShareLagTicksSpecified, Is.False);
        Assert.That(profile.DatalinkDoctrine.ShareLagTicks, Is.EqualTo(0));
        Assert.That(profile.DatalinkDoctrine.IsSharingEnabled, Is.True);

        var resolved = DatalinkShareLagResolver.Resolve(profile.DatalinkDoctrine, catalog);

        Assert.That(resolved.ShareLagTicks, Is.EqualTo(3));
    }

    [Test]
    public void Missing_link_keeps_default_zero_share_lag()
    {
        var catalog = new InMemoryCatalogReader(
            [new CatalogSensorBinding("u1", "radar-1", 1.0, "fixture")]);

        var resolved = DatalinkShareLagResolver.Resolve(SharingDoctrine, catalog);

        Assert.That(resolved.ShareLagTicks, Is.EqualTo(0));
        Assert.That(resolved, Is.EqualTo(SharingDoctrine));
    }

    [Test]
    public void Production_baltic_patrol_without_datalink_section_unchanged()
    {
        var repoRoot = FindRepoRoot();
        Assert.That(repoRoot, Is.Not.Null);
        var path = Path.Combine(repoRoot!, "data", "scenarios", "baltic-patrol.policy.json");
        var profile = ScenarioPolicyJsonLoader.LoadFromFile(path);
        var catalog = InMemoryCatalogReader.BalticPatrolFixture();

        Assert.That(profile.DatalinkDoctrine.IsSharingEnabled, Is.False);
        Assert.That(profile.DatalinkDoctrine, Is.EqualTo(ScenarioDatalinkDoctrine.Default));

        var resolved = DatalinkShareLagResolver.Resolve(profile.DatalinkDoctrine, catalog);

        Assert.That(resolved.ShareLagTicks, Is.EqualTo(0));
        Assert.That(resolved.ShareLagTicksSpecified, Is.False);
        Assert.That(resolved, Is.EqualTo(profile.DatalinkDoctrine));
    }

    [Test]
    public void Primary_link_prefers_first_sorted_comms_binding_over_link_catalog()
    {
        var catalog = new InMemoryCatalogReader(
            [new CatalogSensorBinding("u1", "radar-1", 1.0, "fixture")],
            comms: [new CatalogCommsBinding("u1", "SATCOM_B")],
            links: CatalogValidationDefaults.BalticLinks());

        var resolved = DatalinkShareLagResolver.Resolve(SharingDoctrine, catalog);

        Assert.That(resolved.ShareLagTicks, Is.EqualTo(15));
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