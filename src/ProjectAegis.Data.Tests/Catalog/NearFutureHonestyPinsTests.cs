using System.Reflection;
using ProjectAegis.Data.Catalog;
using Xunit;

namespace ProjectAegis.Data.Tests.Catalog;

/// <summary>
/// Wave 3 / req-09 honesty characterization pins for the near-future spine.
/// Tests-only; pins documented behavior without production edits.
/// </summary>
public sealed class NearFutureHonestyPinsTests
{
    private static readonly string CatalogPath = ResolveCatalogPath();

    [Fact]
    public void near_future_archetype_catalog_has_exactly_four_rows_with_canonical_ids()
    {
        var rows = NearFutureArchetypeCatalog.LoadFromFile(CatalogPath);

        Assert.Equal(4, rows.Length);
        Assert.Contains(rows, a => a.ArchetypeId == "replicator-attritable");
        Assert.Contains(rows, a => a.ArchetypeId == "cca-wingman");
        Assert.Contains(rows, a => a.ArchetypeId == "swarm-saturation");
        Assert.Contains(rows, a => a.ArchetypeId == "hypersonic-boost-glide");
    }

    [Fact]
    public void swarm_tier_limits_medium_max_entities_is_500()
    {
        Assert.Equal(500, SwarmTierLimits.MediumMaxEntities);
        Assert.Equal(500, SwarmTierLimits.MaxEntitiesFor(SwarmTier.Medium));
        // Mass max is a design residual (not MVP default); pin residual ceiling only.
        Assert.Equal(5000, SwarmTierLimits.MassMaxEntities);
        Assert.Equal(5000, SwarmTierLimits.MaxEntitiesFor(SwarmTier.Mass));
    }

    [Fact]
    public void plan_spawns_returns_plan_records_only_not_world_entities()
    {
        // PlanSpawns is plan-only (no DOTS / world entity spawn) — return type is
        // IReadOnlyList<SpawnPlan> of plan records. At TL2 + Medium, mass-tier
        // swarm-saturation is rejected; only the accepted plan list is returned.
        var requests = new[]
        {
            new ScenarioNearFutureUnitRequest("cca-wingman", "cca-1"),
            new ScenarioNearFutureUnitRequest("swarm-saturation", "swarm-1"),
        };

        var plans = NearFutureArchetypeRuntime.PlanSpawns(
            requests,
            scenarioMaxTechnologyLevel: 2,
            maxSwarmTier: SwarmTier.Medium,
            CatalogPath);

        Assert.Single(plans);
        Assert.Equal("cca-wingman", plans[0].ArchetypeId);
        Assert.Equal("cca-1", plans[0].UnitId);
        Assert.IsAssignableFrom<IReadOnlyList<NearFutureArchetypeRuntime.SpawnPlan>>(plans);
        Assert.DoesNotContain(plans, p => p.ArchetypeId == "swarm-saturation");
    }

    [Fact]
    public void data_assembly_has_no_type_named_PlatformTechnologyLevel()
    {
        // Honesty: TL is CatalogPlatformBinding.GameTechnologyLevel / archetype TL —
        // there is no distinct PlatformTechnologyLevel type in ProjectAegis.Data.
        var assembly = typeof(NearFutureArchetypeCatalog).Assembly;
        var match = assembly.GetTypes().FirstOrDefault(t => t.Name == "PlatformTechnologyLevel");
        Assert.Null(match);
    }

    [Fact]
    public void catalog_platform_binding_exposes_GameTechnologyLevel_property()
    {
        var prop = typeof(CatalogPlatformBinding).GetProperty(
            "GameTechnologyLevel",
            BindingFlags.Public | BindingFlags.Instance);

        Assert.NotNull(prop);
        Assert.Equal(typeof(int), prop!.PropertyType);

        var sample = new CatalogPlatformBinding("u-honesty", "Honesty Pin", GameTechnologyLevel: 2);
        Assert.Equal(2, sample.GameTechnologyLevel);
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
