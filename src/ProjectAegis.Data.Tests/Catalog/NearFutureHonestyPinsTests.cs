using System.Reflection;
using ProjectAegis.Data.Catalog;
using NUnit.Framework;

namespace ProjectAegis.Data.Tests.Catalog;

/// <summary>
/// Wave 3 / req-09 honesty characterization pins for the near-future spine.
/// Tests-only; pins documented behavior without production edits.
/// </summary>
public sealed class NearFutureHonestyPinsTests
{
    private static readonly string CatalogPath = ResolveCatalogPath();

    [Test]
    public void near_future_archetype_catalog_has_exactly_four_rows_with_canonical_ids()
    {
        var rows = NearFutureArchetypeCatalog.LoadFromFile(CatalogPath);

        Assert.That(rows.Length, Is.EqualTo(4));
        Assert.That(rows.Any(a => a.ArchetypeId == "replicator-attritable"), Is.True);
        Assert.That(rows.Any(a => a.ArchetypeId == "cca-wingman"), Is.True);
        Assert.That(rows.Any(a => a.ArchetypeId == "swarm-saturation"), Is.True);
        Assert.That(rows.Any(a => a.ArchetypeId == "hypersonic-boost-glide"), Is.True);
    }

    [Test]
    public void swarm_tier_limits_medium_max_entities_is_500()
    {
        Assert.That(SwarmTierLimits.MediumMaxEntities, Is.EqualTo(500));
        Assert.That(SwarmTierLimits.MaxEntitiesFor(SwarmTier.Medium), Is.EqualTo(500));
        // Mass max is a design residual (not MVP default); pin residual ceiling only.
        Assert.That(SwarmTierLimits.MassMaxEntities, Is.EqualTo(5000));
        Assert.That(SwarmTierLimits.MaxEntitiesFor(SwarmTier.Mass), Is.EqualTo(5000));
    }

    [Test]
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

        Assert.That(plans, Has.Exactly(1).Items);
        Assert.That(plans[0].ArchetypeId, Is.EqualTo("cca-wingman"));
        Assert.That(plans[0].UnitId, Is.EqualTo("cca-1"));
        Assert.That(plans, Is.InstanceOf<IReadOnlyList<NearFutureArchetypeRuntime.SpawnPlan>>());
        Assert.That(plans.Any(p => p.ArchetypeId == "swarm-saturation"), Is.False);
    }

    [Test]
    public void data_assembly_has_no_type_named_PlatformTechnologyLevel()
    {
        // Honesty: TL is CatalogPlatformBinding.GameTechnologyLevel / archetype TL —
        // there is no distinct PlatformTechnologyLevel type in ProjectAegis.Data.
        var assembly = typeof(NearFutureArchetypeCatalog).Assembly;
        var match = assembly.GetTypes().FirstOrDefault(t => t.Name == "PlatformTechnologyLevel");
        Assert.That(match, Is.Null);
    }

    [Test]
    public void catalog_platform_binding_exposes_GameTechnologyLevel_property()
    {
        var prop = typeof(CatalogPlatformBinding).GetProperty(
            "GameTechnologyLevel",
            BindingFlags.Public | BindingFlags.Instance);

        Assert.That(prop, Is.Not.Null);
        Assert.That(prop!.PropertyType, Is.EqualTo(typeof(int)));

        var sample = new CatalogPlatformBinding("u-honesty", "Honesty Pin", GameTechnologyLevel: 2);
        Assert.That(sample.GameTechnologyLevel, Is.EqualTo(2));
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
