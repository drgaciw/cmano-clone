using ProjectAegis.Data.Catalog;
using ProjectAegis.Sim.Policy;
using ProjectAegis.Sim.Scenario;
using ProjectAegis.Sim.Sensors;
using NUnit.Framework;
using ProjectAegis.Sim.Tests;

namespace ProjectAegis.Sim.Tests.Scenario;

public sealed class DetectionTrialResolverTests
{
    [Test]
    public void Catalog_detection_builds_trials_with_catalog_basePd()
    {
        var profile = new ScenarioPolicyProfile(
            EffectivePolicy.DefaultFree,
            catalogDetectionTargets:
            [
                new ScenarioCatalogDetectionTarget("u1", "radar-1", "hostile-1", "c1"),
            ]);
        var catalog = InMemoryCatalogReader.BalticPatrolFixture();

        var trials = DetectionTrialResolver.Resolve(profile, catalog);

        AssertEx.Single(trials);
        Assert.That(trials[0].BasePd, Is.EqualTo(1.0));
        Assert.That(trials[0].TargetId, Is.EqualTo("hostile-1"));
        Assert.That(trials[0].Modality, Is.EqualTo(SensorModality.Radar));
        Assert.That(trials[0].RequiresActiveRadar, Is.True);
    }

    [Test]
    public void Explicit_detection_trials_take_precedence_over_catalog()
    {
        var profile = new ScenarioPolicyProfile(
            EffectivePolicy.DefaultFree,
            detectionTrials: [new ScenarioDetectionTrial("u1", "radar-1", "hostile-1", "c1", 0.25)],
            catalogDetectionTargets:
            [
                new ScenarioCatalogDetectionTarget("u1", "radar-1", "hostile-2", "c2"),
            ]);
        var catalog = InMemoryCatalogReader.BalticPatrolFixture();

        var trials = DetectionTrialResolver.Resolve(profile, catalog);

        AssertEx.Single(trials);
        Assert.That(trials[0].BasePd, Is.EqualTo(0.25));
    }

    [Test]
    public void Catalog_ir_fixture_maps_modality_and_clears_requires_active_radar()
    {
        var profile = new ScenarioPolicyProfile(
            EffectivePolicy.DefaultFree,
            catalogDetectionTargets:
            [
                new ScenarioCatalogDetectionTarget("ucav-blue", "internal-ir", "hostile-1", "c-ir"),
            ]);
        var catalog = InMemoryCatalogReader.BalticV3Fixture();

        var trials = DetectionTrialResolver.Resolve(profile, catalog);

        AssertEx.Single(trials);
        Assert.That(trials[0].BasePd, Is.EqualTo(0.85));
        Assert.That(trials[0].Modality, Is.EqualTo(SensorModality.Infrared));
        Assert.That(trials[0].RequiresActiveRadar, Is.False);
        Assert.That(trials[0].ObserverId, Is.EqualTo("ucav-blue"));
        Assert.That(trials[0].SensorId, Is.EqualTo("internal-ir"));
    }

    [Test]
    public void Catalog_radar_on_ucav_stays_radar_modality()
    {
        var profile = new ScenarioPolicyProfile(
            EffectivePolicy.DefaultFree,
            catalogDetectionTargets:
            [
                new ScenarioCatalogDetectionTarget("ucav-blue", "recon-radar", "hostile-1", "c-r"),
            ]);
        var catalog = InMemoryCatalogReader.BalticV3Fixture();

        var trials = DetectionTrialResolver.Resolve(profile, catalog);

        AssertEx.Single(trials);
        Assert.That(trials[0].Modality, Is.EqualTo(SensorModality.Radar));
        Assert.That(trials[0].RequiresActiveRadar, Is.True);
    }

    [Test]
    public void Explicit_trials_leave_modality_as_authored()
    {
        var profile = new ScenarioPolicyProfile(
            EffectivePolicy.DefaultFree,
            detectionTrials:
            [
                new ScenarioDetectionTrial(
                    "ucav-blue",
                    "internal-ir",
                    "hostile-1",
                    "c1",
                    0.5,
                    Modality: SensorModality.Visual,
                    RequiresActiveRadar: true),
            ],
            catalogDetectionTargets:
            [
                new ScenarioCatalogDetectionTarget("ucav-blue", "internal-ir", "hostile-2", "c2"),
            ]);
        var catalog = InMemoryCatalogReader.BalticV3Fixture();

        var trials = DetectionTrialResolver.Resolve(profile, catalog);

        AssertEx.Single(trials);
        Assert.That(trials[0].BasePd, Is.EqualTo(0.5));
        Assert.That(trials[0].Modality, Is.EqualTo(SensorModality.Visual));
        Assert.That(trials[0].RequiresActiveRadar, Is.True);
    }

    [TestCase(null, SensorModality.Radar)]
    [TestCase("", SensorModality.Radar)]
    [TestCase("   ", SensorModality.Radar)]
    [TestCase("Radar", SensorModality.Radar)]
    [TestCase("radar", SensorModality.Radar)]
    [TestCase("Infrared", SensorModality.Infrared)]
    [TestCase("infrared", SensorModality.Infrared)]
    [TestCase("INFRARED", SensorModality.Infrared)]
    [TestCase("Visual", SensorModality.Visual)]
    [TestCase("visual", SensorModality.Visual)]
    [TestCase("unknown-mod", SensorModality.Radar)]
    public void ParseSensorModality_maps_catalog_strings(string? input, SensorModality expected)
    {
        Assert.That(DetectionTrialResolver.ParseSensorModality(input), Is.EqualTo(expected));
    }
}
