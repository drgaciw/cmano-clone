using ProjectAegis.Data.Catalog;
using ProjectAegis.Sim.Core;
using ProjectAegis.Sim.Scenario;
using ProjectAegis.Sim.Sensors;
using NUnit.Framework;

namespace ProjectAegis.Sim.Tests.Sensors;

/// <summary>EW-01 / DRG-386: jammer vs radar generation matrix and scan-type ECCM.</summary>
public sealed class RadarEccmTests
{
    public static IEnumerable<TestCaseData> Matrix()
    {
        // scan, agile, radarGen, jammerGen, expected effectiveness
        yield return new TestCaseData(RadarScanType.Unspecified, false, 0, 0, 1.0);
        yield return new TestCaseData(RadarScanType.Unspecified, false, 0, 4, 1.0);
        yield return new TestCaseData(RadarScanType.Mechanical, false, 0, 0, 1.0);
        yield return new TestCaseData(RadarScanType.Mechanical, true, 0, 0, 0.8);
        yield return new TestCaseData(RadarScanType.Pesa, false, 0, 0, 0.6); // 0.75 × agile 0.8
        yield return new TestCaseData(RadarScanType.Aesa, false, 0, 0, 0.4); // 0.5 × agile 0.8
        yield return new TestCaseData(RadarScanType.Mechanical, false, 3, 3, 1.0);
        yield return new TestCaseData(RadarScanType.Mechanical, false, 4, 2, 0.7); // radar two generations ahead
        yield return new TestCaseData(RadarScanType.Mechanical, false, 2, 4, 1.0); // older radar: capped at full effect
        yield return new TestCaseData(RadarScanType.Aesa, false, 5, 2, 0.22); // 0.4 × 0.55
        yield return new TestCaseData(RadarScanType.Aesa, false, 6, 1, 0.1); // floor
    }

    [TestCaseSource(nameof(Matrix))]
    public void Effectiveness_matrix(RadarScanType scan, bool agile, int radarGen, int jammerGen, double expected)
    {
        var p = new RadarEccmProfile(scan, agile, radarGen);
        Assert.That(
            Math.Round((double)(EccmJamMatrix.Effectiveness(in p, jammerGen)), 6, MidpointRounding.AwayFromZero),
            Is.EqualTo(Math.Round((double)(expected), 6, MidpointRounding.AwayFromZero)));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Omitted_scan_type_is_unspecified(string? value)
    {
        Assert.That(RadarEccmProfile.ParseScanType(value), Is.EqualTo(RadarScanType.Unspecified));
    }

    [TestCase("aesa", RadarScanType.Aesa)]
    [TestCase("Pesa", RadarScanType.Pesa)]
    [TestCase("mechanical", RadarScanType.Mechanical)]
    public void Named_scan_type_parses(string value, RadarScanType expected)
    {
        Assert.That(RadarEccmProfile.ParseScanType(value), Is.EqualTo(expected));
    }

    [TestCase("2")]
    [TestCase("0")]
    [TestCase("Phased")]
    [TestCase("AESA ")]
    [TestCase("Mechanical,Aesa")]
    public void Numeric_unknown_and_combined_scan_types_are_rejected(string value)
    {
        var ex = Assert.Throws<InvalidDataException>(() => RadarEccmProfile.ParseScanType(value));
        Assert.That(ex!.Message, Does.Contain(value));
    }

    [Test]
    public void Pesa_and_aesa_are_always_frequency_agile()
    {
        Assert.That(new RadarEccmProfile(RadarScanType.Pesa).IsEffectivelyFrequencyAgile, Is.True);
        Assert.That(new RadarEccmProfile(RadarScanType.Aesa).IsEffectivelyFrequencyAgile, Is.True);
        Assert.That(new RadarEccmProfile(RadarScanType.Mechanical).IsEffectivelyFrequencyAgile, Is.False);
    }

    [Test]
    public void Jam_resolver_scales_each_jammer_then_takes_strongest()
    {
        var jammers = new[]
        {
            new ScenarioJammer("h1", 1.0, TechGeneration: 1),
            new ScenarioJammer("h1", 0.9, TechGeneration: 5),
        };
        var aesa = new RadarEccmProfile(RadarScanType.Aesa, TechGeneration: 5);
        // gen-1 jammer: 1.0 × max(0.1, 0.4 × 0.4)=0.16; gen-5 jammer: 0.9 × 0.4 = 0.36
        Assert.That(
            Math.Round((double)(ScenarioJamResolver.ResolveJam("u1", "h1", 0, jammers, in aesa)), 6, MidpointRounding.AwayFromZero),
            Is.EqualTo(Math.Round((double)(0.36), 6, MidpointRounding.AwayFromZero)));
    }

    [Test]
    public void Jam_resolver_legacy_overload_is_unchanged()
    {
        var jammers = new[] { new ScenarioJammer("h1", 0.7, TechGeneration: 3) };
        Assert.That(ScenarioJamResolver.ResolveJam("u1", "h1", 0, jammers), Is.EqualTo(0.7));
    }

    [Test]
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

        Assert.That(legacy[0].Pd, Is.EqualTo(0));
        Assert.That(
            Math.Round((double)(eccm[0].Pd), 6, MidpointRounding.AwayFromZero),
            Is.EqualTo(Math.Round((double)(0.6), 6, MidpointRounding.AwayFromZero))); // 1 − (1.0 × 0.4)
    }

    [Test]
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

        Assert.That(env.ResolveEccm("r1"), Is.EqualTo(new RadarEccmProfile(RadarScanType.Pesa, false, 3)));
        Assert.That(env.ResolveEccm("r2"), Is.EqualTo(new RadarEccmProfile(RadarScanType.Aesa, false, 5)));
        Assert.That(env.ResolveEccm("ir1"), Is.EqualTo(RadarEccmProfile.Unspecified));
    }

    [Test]
    public void Factory_returns_none_when_nothing_authored()
    {
        Assert.That(DetectionEnvironmentFactory.Build(null, null), Is.SameAs(DetectionEnvironment.None));
    }
}
