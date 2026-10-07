using ProjectAegis.Data.Catalog;
using ProjectAegis.Sim.Core;
using ProjectAegis.Sim.Scenario;
using ProjectAegis.Sim.Sensors;
using Xunit;

namespace ProjectAegis.Sim.Tests.Sensors;

/// <summary>EW-01 / DRG-386: jammer vs radar generation matrix and scan-type ECCM.</summary>
public sealed class RadarEccmTests
{
    public static TheoryData<RadarScanType, bool, int, int, double> Matrix => new()
    {
        // scan, agile, radarGen, jammerGen, expected effectiveness
        { RadarScanType.Unspecified, false, 0, 0, 1.0 },
        { RadarScanType.Unspecified, false, 0, 4, 1.0 },
        { RadarScanType.Mechanical, false, 0, 0, 1.0 },
        { RadarScanType.Mechanical, true, 0, 0, 0.8 },
        { RadarScanType.Pesa, false, 0, 0, 0.6 },   // 0.75 × agile 0.8
        { RadarScanType.Aesa, false, 0, 0, 0.4 },   // 0.5 × agile 0.8
        { RadarScanType.Mechanical, false, 3, 3, 1.0 },
        { RadarScanType.Mechanical, false, 4, 2, 0.7 }, // radar two generations ahead
        { RadarScanType.Mechanical, false, 2, 4, 1.0 }, // older radar: capped at full effect
        { RadarScanType.Aesa, false, 5, 2, 0.22 },      // 0.4 × 0.55
        { RadarScanType.Aesa, false, 6, 1, 0.1 },       // floor
    };

    [Theory]
    [MemberData(nameof(Matrix))]
    public void Effectiveness_matrix(RadarScanType scan, bool agile, int radarGen, int jammerGen, double expected)
    {
        var p = new RadarEccmProfile(scan, agile, radarGen);
        Assert.Equal(expected, EccmJamMatrix.Effectiveness(in p, jammerGen), precision: 6);
    }

    [Fact]
    public void Pesa_and_aesa_are_always_frequency_agile()
    {
        Assert.True(new RadarEccmProfile(RadarScanType.Pesa).IsEffectivelyFrequencyAgile);
        Assert.True(new RadarEccmProfile(RadarScanType.Aesa).IsEffectivelyFrequencyAgile);
        Assert.False(new RadarEccmProfile(RadarScanType.Mechanical).IsEffectivelyFrequencyAgile);
    }

    [Fact]
    public void Jam_resolver_scales_each_jammer_then_takes_strongest()
    {
        var jammers = new[]
        {
            new ScenarioJammer("h1", 1.0, TechGeneration: 1),
            new ScenarioJammer("h1", 0.9, TechGeneration: 5),
        };
        var aesa = new RadarEccmProfile(RadarScanType.Aesa, TechGeneration: 5);
        // gen-1 jammer: 1.0 × max(0.1, 0.4 × 0.4)=0.16; gen-5 jammer: 0.9 × 0.4 = 0.36
        Assert.Equal(0.36, ScenarioJamResolver.ResolveJam("u1", "h1", 0, jammers, in aesa), precision: 6);
    }

    [Fact]
    public void Jam_resolver_legacy_overload_is_unchanged()
    {
        var jammers = new[] { new ScenarioJammer("h1", 0.7, TechGeneration: 3) };
        Assert.Equal(0.7, ScenarioJamResolver.ResolveJam("u1", "h1", 0, jammers));
    }

    [Fact]
    public void Detection_loop_applies_sensor_eccm_profile_to_radar_jam()
    {
        var trials = new[] { new ScenarioDetectionTrial("u1", "aesa-1", "h1", "c1", 1.0) };
        var jammers = new[] { new ScenarioJammer("h1", 1.0) };
        var env = new DetectionEnvironment(
            LosGeometryTable.Empty,
            new Dictionary<string, RadarEccmProfile> { ["aesa-1"] = new(RadarScanType.Aesa) });

        var legacy = DeterministicDetectionLoop.RollTick(SimSeed.FromScenario(1), 1, trials, null, jammers: jammers);
        var eccm = DeterministicDetectionLoop.RollTick(
            SimSeed.FromScenario(1), 1, trials, null, jammers: jammers, environment: env);

        Assert.Equal(0, legacy[0].Pd);
        Assert.Equal(0.6, eccm[0].Pd, precision: 6); // 1 − (1.0 × 0.4)
    }

    [Fact]
    public void Factory_reads_catalog_then_scenario_overrides()
    {
        var catalog = new InMemoryCatalogReader(new[]
        {
            new CatalogSensorBinding("p1", "r1", 1.0) { RadarScanType = "Pesa", RadarTechGeneration = 3 },
            new CatalogSensorBinding("p1", "r2", 1.0) { RadarScanType = "Mechanical" },
            new CatalogSensorBinding("p1", "ir1", 1.0, Modality: CatalogSensorModalities.Infrared),
        });
        var profile = new ScenarioPolicyProfile(ProjectAegis.Sim.Policy.EffectivePolicy.DefaultFree)
        {
            RadarEccm = new Dictionary<string, RadarEccmProfile> { ["r2"] = new(RadarScanType.Aesa, TechGeneration: 5) },
        };

        var env = DetectionEnvironmentFactory.Build(profile, catalog);

        Assert.Equal(new RadarEccmProfile(RadarScanType.Pesa, false, 3), env.ResolveEccm("r1"));
        Assert.Equal(new RadarEccmProfile(RadarScanType.Aesa, false, 5), env.ResolveEccm("r2"));
        Assert.Equal(RadarEccmProfile.Unspecified, env.ResolveEccm("ir1"));
    }

    [Fact]
    public void Factory_returns_none_when_nothing_authored()
    {
        Assert.Same(DetectionEnvironment.None, DetectionEnvironmentFactory.Build(null, null));
    }
}
