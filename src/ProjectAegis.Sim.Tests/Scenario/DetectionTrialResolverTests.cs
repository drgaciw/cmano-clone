using ProjectAegis.Data.Catalog;
using ProjectAegis.Sim.Policy;
using ProjectAegis.Sim.Scenario;
using ProjectAegis.Sim.Sensors;
using Xunit;

namespace ProjectAegis.Sim.Tests.Scenario;

public sealed class DetectionTrialResolverTests
{
    [Fact]
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

        Assert.Single(trials);
        Assert.Equal(1.0, trials[0].BasePd);
        Assert.Equal("hostile-1", trials[0].TargetId);
        Assert.Equal(SensorModality.Radar, trials[0].Modality);
        Assert.True(trials[0].RequiresActiveRadar);
    }

    [Fact]
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

        Assert.Single(trials);
        Assert.Equal(0.25, trials[0].BasePd);
    }

    [Fact]
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

        Assert.Single(trials);
        Assert.Equal(0.85, trials[0].BasePd);
        Assert.Equal(SensorModality.Infrared, trials[0].Modality);
        Assert.False(trials[0].RequiresActiveRadar);
        Assert.Equal("ucav-blue", trials[0].ObserverId);
        Assert.Equal("internal-ir", trials[0].SensorId);
    }

    [Fact]
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

        Assert.Single(trials);
        Assert.Equal(SensorModality.Radar, trials[0].Modality);
        Assert.True(trials[0].RequiresActiveRadar);
    }

    [Fact]
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

        Assert.Single(trials);
        Assert.Equal(0.5, trials[0].BasePd);
        Assert.Equal(SensorModality.Visual, trials[0].Modality);
        Assert.True(trials[0].RequiresActiveRadar);
    }

    [Theory]
    [InlineData(null, SensorModality.Radar)]
    [InlineData("", SensorModality.Radar)]
    [InlineData("   ", SensorModality.Radar)]
    [InlineData("Radar", SensorModality.Radar)]
    [InlineData("radar", SensorModality.Radar)]
    [InlineData("Infrared", SensorModality.Infrared)]
    [InlineData("infrared", SensorModality.Infrared)]
    [InlineData("INFRARED", SensorModality.Infrared)]
    [InlineData("Visual", SensorModality.Visual)]
    [InlineData("visual", SensorModality.Visual)]
    [InlineData("unknown-mod", SensorModality.Radar)]
    public void ParseSensorModality_maps_catalog_strings(string? input, SensorModality expected)
    {
        Assert.Equal(expected, DetectionTrialResolver.ParseSensorModality(input));
    }
}
