using ProjectAegis.Data.Catalog;
using ProjectAegis.Sim.Policy;
using ProjectAegis.Sim.Scenario;
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
}