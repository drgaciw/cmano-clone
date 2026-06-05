using Xunit;

namespace ProjectAegis.Sim.Tests.Scenario;

using ProjectAegis.Sim.Engage;
using ProjectAegis.Sim.Glossary;
using ProjectAegis.Sim.Scenario;

public sealed class ScenarioSpeculativeGateTests
{
    [Fact]
    public void Black_project_policy_json_enables_tl5_weapon_engage()
    {
        var path = ResolvePolicyPath("baltic-patrol-black-project.policy.json");
        var profile = ScenarioPolicyJsonLoader.LoadFromFile(path);
        Assert.True(profile.Speculative.BlackProjectMode);
        Assert.Equal(5, profile.Speculative.MaxTechnologyLevel);

        var ctx = profile.ResolveEngageContext();
        Assert.Null(SpeculativeEngageGate.Evaluate(profile.Speculative, in ctx));
    }

    [Fact]
    public void Campaign_default_policy_denies_tl5_black_project_weapon()
    {
        var path = ResolvePolicyPath("baltic-patrol-speculative-gated.policy.json");
        var profile = ScenarioPolicyJsonLoader.LoadFromFile(path);
        var ctx = profile.ResolveEngageContext();
        Assert.Equal(EngagementAbortReason.TechnologyLevelExceeded, SpeculativeEngageGate.Evaluate(profile.Speculative, in ctx));
    }

    [Fact]
    public void Mvp_resolver_denies_speculative_weapon_without_black_project_mode()
    {
        var world = new DictionaryEngageWorldQuery();
        var request = new EngageRequest(1, 2, 0, 0);
        world.Set(request, new EngageContext(
            50_000,
            new WeaponEnvelope(1_000, 100_000),
            2,
            true,
            WeaponTechnologyLevel: 5,
            WeaponRequiresBlackProject: true));

        var magazines = new MagazineLedger();
        magazines.SetRounds(1, 0, 2);
        var resolver = new MvpEngagementResolver(
            world,
            magazines,
            speculative: ScenarioSpeculativeSettings.CampaignDefault);

        var result = resolver.Resolve(request);
        Assert.Equal(EngagementAbortReason.TechnologyLevelExceeded, result.AbortReason);
        Assert.Equal(
            AbortReasonCatalog.Engage.TECHNOLOGY_LEVEL_EXCEEDED,
            EngagementAbortReasonCodes.ToLogCode(result.AbortReason));
    }

    [Fact]
    public void Mvp_resolver_allows_speculative_weapon_when_black_project_mode_on()
    {
        var world = new DictionaryEngageWorldQuery();
        var request = new EngageRequest(1, 2, 0, 0);
        world.Set(request, new EngageContext(
            50_000,
            new WeaponEnvelope(1_000, 100_000),
            2,
            true,
            WeaponTechnologyLevel: 5,
            WeaponRequiresBlackProject: true));

        var magazines = new MagazineLedger();
        magazines.SetRounds(1, 0, 2);
        var resolver = new MvpEngagementResolver(
            world,
            magazines,
            speculative: new ScenarioSpeculativeSettings(blackProjectMode: true, maxTechnologyLevel: 5));

        var result = resolver.Resolve(request);
        Assert.True(result.Launched);
    }

    [Fact]
    public void Speculative_platform_catalog_loads_npx_entry()
    {
        var path = ResolveCatalogPath();
        var catalog = SpeculativePlatformCatalog.LoadFromFile(path);
        Assert.True(catalog.TryGet("npx-laser-orbital", out var entry));
        Assert.Equal(5, entry.GameTechnologyLevel);
        Assert.True(entry.RequiresBlackProject);
    }

    private static string ResolvePolicyPath(string fileName)
    {
        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 8; i++)
        {
            var candidate = Path.Combine(dir, "data", "scenarios", fileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = Path.GetDirectoryName(dir) ?? dir;
        }

        dir = Directory.GetCurrentDirectory();
        for (var i = 0; i < 8; i++)
        {
            var candidate = Path.Combine(dir, "data", "scenarios", fileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = Path.GetDirectoryName(dir) ?? dir;
        }

        throw new FileNotFoundException(fileName);
    }

    private static string ResolveCatalogPath()
    {
        var dir = Directory.GetCurrentDirectory();
        for (var i = 0; i < 8; i++)
        {
            var candidate = Path.Combine(dir, "data", "catalog", "speculative_platforms.json");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = Path.GetDirectoryName(dir) ?? dir;
        }

        throw new FileNotFoundException("speculative_platforms.json");
    }
}