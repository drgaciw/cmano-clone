using ProjectAegis.Data.Catalog;
using ProjectAegis.Sim.Catalog;
using ProjectAegis.Sim.Core;
using ProjectAegis.Sim.Policy;
using ProjectAegis.Sim.Scenario;
using ProjectAegis.Sim.Sensors;
using Xunit;

namespace ProjectAegis.Sim.Tests.Catalog;

public sealed class PhaseBCatalogConsumerTests
{
    private static ScenarioPolicyProfile CatalogProfile() =>
        new(
            EffectivePolicy.DefaultFree,
            catalogDetectionTargets:
            [
                new ScenarioCatalogDetectionTarget("u1", "radar-1", "hostile-1", "c1"),
            ]);

    [Fact]
    public void PhaseB_Legacy_catalog_without_signature_preserves_trial_basePd_and_envMask()
    {
        var profile = CatalogProfile();
        var catalog = InMemoryCatalogReader.BalticPatrolFixture();

        var trials = DetectionTrialResolver.Resolve(profile, catalog);

        Assert.Single(trials);
        Assert.Equal(1.0, trials[0].BasePd);
        Assert.Equal(1.0, trials[0].EnvMask);
    }

    [Fact]
    public void PhaseB_Committed_observer_signature_changes_effective_envMask_and_detection_pd()
    {
        var profile = CatalogProfile();
        var legacy = InMemoryCatalogReader.BalticPatrolFixture();
        var withSignature = new InMemoryCatalogReader(
            [
                new CatalogSensorBinding("u1", "radar-1", 1.0, "fixture-radar1"),
            ],
            "phase-b-signature-fixture",
            signatures: [new CatalogSignature("u1", RcsBandDbsm: -20)]);

        var baseline = DetectionTrialResolver.Resolve(profile, legacy);
        var modified = DetectionTrialResolver.Resolve(profile, withSignature);

        Assert.Equal(1.0, baseline[0].EnvMask);
        Assert.Equal(0.9, modified[0].EnvMask, precision: 6);
        Assert.Equal(1.0, modified[0].BasePd);

        var seed = SimSeed.FromScenario(42);
        var baselineRoll = DeterministicDetectionLoop.RollTick(seed, 0, baseline, unitRadarEmcon: null);
        var modifiedRoll = DeterministicDetectionLoop.RollTick(seed, 0, modified, unitRadarEmcon: null);

        Assert.NotEqual(baselineRoll[0].Pd, modifiedRoll[0].Pd);
    }

    [Theory]
    [InlineData(-40, 0.7)]
    [InlineData(-10, 1.0)]
    [InlineData(20, 1.0)]
    public void PhaseB_Rcs_boundary_values_apply_predictable_envMask(double rcsDbsm, double expectedEnvMask)
    {
        var signature = new CatalogSignature("u1", RcsBandDbsm: rcsDbsm);
        var envMask = PhaseBCatalogDetectionModifier.ApplyEnvMask(1.0, signature);
        Assert.Equal(expectedEnvMask, envMask, precision: 6);
    }

    [Fact]
    public void PhaseB_Unreferenced_signature_platform_leaves_trial_unchanged()
    {
        var profile = CatalogProfile();
        var catalog = new InMemoryCatalogReader(
            [
                new CatalogSensorBinding("u1", "radar-1", 1.0, "fixture-radar1"),
            ],
            signatures: [new CatalogSignature("other-platform", RcsBandDbsm: 30)]);

        var trials = DetectionTrialResolver.Resolve(profile, catalog);

        Assert.Single(trials);
        Assert.Equal(1.0, trials[0].EnvMask);
        Assert.Equal(1.0, trials[0].BasePd);
    }
}