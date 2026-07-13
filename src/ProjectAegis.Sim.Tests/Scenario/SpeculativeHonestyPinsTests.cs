using System.Reflection;
using Xunit;

namespace ProjectAegis.Sim.Tests.Scenario;

using ProjectAegis.Sim.Engage;
using ProjectAegis.Sim.Glossary;
using ProjectAegis.Sim.Scenario;

/// <summary>
/// Adversarial Wave 3 honesty pins for S54 demotion + SpeculativeEngageGate spine (tracker 10b).
/// Characterization-only: no production types invented; demotion of full DEW runtime remains hard.
/// </summary>
public sealed class SpeculativeHonestyPinsTests
{
    private static readonly string[] ForbiddenTypeNames =
    [
        "OrbitalDewPlatform",
        "KesslerRiskMeter",
        "EscalationTier",
    ];

    /// <summary>
    /// Pins Wave 3 hard demotion of tracker 10b: no full DEW/Kessler/escalation runtime types
    /// in ProjectAegis.Sim (or any other loaded ProjectAegis.* assembly from this test host).
    /// </summary>
    [Fact]
    public void src_assemblies_have_no_OrbitalDewPlatform_KesslerRiskMeter_or_EscalationTier_types()
    {
        // Always include Sim via SpeculativeEngageGate; Data is not referenced from Sim.Tests.
        _ = typeof(SpeculativeEngageGate);

        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a =>
            {
                var name = a.GetName().Name;
                return name is not null &&
                       name.StartsWith("ProjectAegis.", StringComparison.Ordinal);
            })
            .ToArray();

        Assert.Contains(assemblies, a => a == typeof(SpeculativeEngageGate).Assembly);

        var hits = new List<string>();
        foreach (var assembly in assemblies)
        {
            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = ex.Types.Where(t => t is not null).Cast<Type>().ToArray();
            }

            foreach (var type in types)
            {
                if (type.Name is null)
                {
                    continue;
                }

                foreach (var forbidden in ForbiddenTypeNames)
                {
                    if (string.Equals(type.Name, forbidden, StringComparison.Ordinal))
                    {
                        hits.Add($"{assembly.GetName().Name}:{type.FullName}");
                    }
                }
            }
        }

        Assert.True(
            hits.Count == 0,
            "Forbidden demoted types present: " + string.Join(", ", hits));
    }

    /// <summary>
    /// Metadata rows for gate tests only — not full DEW runtime types (see demotion pin above).
    /// </summary>
    [Fact]
    public void speculative_platform_catalog_includes_orbital_dew_demo_as_metadata_only()
    {
        var path = ResolveCatalogPath();
        var catalog = SpeculativePlatformCatalog.LoadFromFile(path);

        Assert.True(catalog.TryGet("orbital-dew-demo", out var dew));
        Assert.Equal(4, dew.GameTechnologyLevel);
        Assert.False(dew.RequiresBlackProject);

        Assert.True(catalog.TryGet("npx-laser-orbital", out var npx));
        Assert.Equal(5, npx.GameTechnologyLevel);
        Assert.True(npx.RequiresBlackProject);
    }

    /// <summary>
    /// Existing ScenarioSpeculativeGateTests cover TechnologyLevelExceeded (TL first).
    /// This fills the BlackProjectRequired gap: TL allows, black mode off.
    /// </summary>
    [Fact]
    public void speculative_gate_returns_BlackProjectRequired_when_tl_ok_but_black_mode_off()
    {
        var settings = new ScenarioSpeculativeSettings(
            blackProjectMode: false,
            maxTechnologyLevel: 5);

        var ctx = new EngageContext(
            50_000,
            new WeaponEnvelope(1_000, 100_000),
            2,
            true,
            WeaponTechnologyLevel: 5,
            WeaponRequiresBlackProject: true);

        Assert.Equal(
            EngagementAbortReason.BlackProjectRequired,
            SpeculativeEngageGate.Evaluate(settings, in ctx));
    }

    [Fact]
    public void mvp_resolver_aborts_black_project_required_with_log_code()
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
            speculative: new ScenarioSpeculativeSettings(
                blackProjectMode: false,
                maxTechnologyLevel: 5));

        var result = resolver.Resolve(request);
        Assert.Equal(EngagementAbortReason.BlackProjectRequired, result.AbortReason);
        Assert.Equal(
            AbortReasonCatalog.Engage.BLACK_PROJECT_REQUIRED,
            EngagementAbortReasonCodes.ToLogCode(result.AbortReason));
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

        dir = AppContext.BaseDirectory;
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
