using ProjectAegis.Data.Catalog;
using Xunit;

namespace ProjectAegis.Data.Tests.Catalog;

public sealed class CatalogArchetypeGateTests
{
    private static readonly string CatalogPath = ResolveCatalogPath();

    [Fact]
    public void ApplyTechnologyLevelGate_keeps_tl2_archetypes_for_campaign_default()
    {
        var rows = NearFutureArchetypeCatalog.LoadFromFile(CatalogPath);
        var gated = CatalogArchetypeGate.ApplyTechnologyLevelGate(rows, scenarioMaxTechnologyLevel: 2);
        Assert.Equal(3, gated.Length);
        Assert.Contains(gated, a => a.ArchetypeId == "cca-wingman");
        Assert.Contains(gated, a => a.ArchetypeId == "hypersonic-boost-glide");
        Assert.DoesNotContain(gated, a => a.ArchetypeId == "swarm-saturation");
    }

    [Fact]
    public void ApplySwarmTierCap_rejects_mass_tier_for_medium_cap()
    {
        var rows = NearFutureArchetypeCatalog.LoadFromFile(CatalogPath);
        var gated = CatalogArchetypeGate.ApplySwarmTierCap(rows, SwarmTier.Medium);
        Assert.Equal(3, gated.Length);
        Assert.DoesNotContain(gated, a => a.SwarmTier == SwarmTier.Mass);
    }

    [Fact]
    public void ApplyAllGates_intersects_tl_and_swarm_rules()
    {
        var rows = NearFutureArchetypeCatalog.LoadFromFile(CatalogPath);
        var gated = CatalogArchetypeGate.ApplyAllGates(rows, scenarioMaxTechnologyLevel: 2, maxSwarmTier: SwarmTier.Medium);
        Assert.Equal(3, gated.Length);
        Assert.Contains(gated, a => a.ArchetypeId == "cca-wingman");
        Assert.Contains(gated, a => a.ArchetypeId == "replicator-attritable");
        Assert.Contains(gated, a => a.ArchetypeId == "hypersonic-boost-glide");
    }

    private static string ResolveCatalogPath()
    {
        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 8; i++)
        {
            var candidate = Path.Combine(dir, "data", "catalog", "near_future_archetypes.json");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = Path.GetDirectoryName(dir) ?? dir;
        }

        dir = Directory.GetCurrentDirectory();
        for (var i = 0; i < 8; i++)
        {
            var candidate = Path.Combine(dir, "data", "catalog", "near_future_archetypes.json");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = Path.GetDirectoryName(dir) ?? dir;
        }

        throw new FileNotFoundException("near_future_archetypes.json");
    }
}