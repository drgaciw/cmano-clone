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

    [Fact]
    public void PhaseB_Legacy_catalog_without_mobility_preserves_launch_readiness()
    {
        var catalog = InMemoryCatalogReader.BalticPatrolFixture();

        var readiness = PhaseBCatalogMobilityReadinessStub.EvaluateLaunchReadiness("u1", catalog);

        Assert.False(readiness.CatalogResolved);
        Assert.True(readiness.ReadyForLaunch);
        Assert.Equal(PhaseBCatalogMobilityReadinessStub.NeutralMobilityScore, readiness.MobilityScore);
    }

    [Fact]
    public void PhaseB_Committed_zero_range_mobility_blocks_launch_readiness()
    {
        var catalog = new InMemoryCatalogReader(
            [new CatalogSensorBinding("u1", "radar-1", 1.0, "fixture-radar1")],
            mobility: [new CatalogMobility("u1", MaxSpeedKnots: 32, RangeNm: 0)]);

        var readiness = PhaseBCatalogMobilityReadinessStub.EvaluateLaunchReadiness("u1", catalog);

        Assert.True(readiness.CatalogResolved);
        Assert.False(readiness.ReadyForLaunch);
        Assert.Equal(0.0, readiness.MobilityScore);
    }

    [Fact]
    public void PhaseB_Baltic_fixture_mobility_changes_readiness_policy_outcome()
    {
        var profile = new ScenarioPolicyProfile(EffectivePolicy.DefaultFree);
        var legacy = InMemoryCatalogReader.BalticPatrolFixture();
        var withMobility = InMemoryCatalogReader.BalticPhaseBFixture();

        var baseline = ReadinessPolicyEvaluator.EvaluateUnit("u1", profile, legacy);
        var modified = ReadinessPolicyEvaluator.EvaluateUnit("u1", profile, withMobility);

        Assert.False(baseline.CatalogResolved);
        Assert.True(modified.CatalogResolved);
        Assert.True(modified.ReadyForLaunch);
        Assert.True(modified.ReadinessScore > 0.5);
    }

    [Fact]
    public void PhaseB_Catalog_emcon_off_blocks_active_radar_detection_roll()
    {
        var seed = SimSeed.FromScenario(42);
        var trials = new[]
        {
            new ScenarioDetectionTrial("u1", "radar-1", "hostile-1", "c1", 1.0, RequiresActiveRadar: true),
        };
        var catalog = new InMemoryCatalogReader(
            [new CatalogSensorBinding("u1", "radar-1", 1.0, "fixture-radar1")],
            emcon: [new CatalogEmcon("u1", "free", "radar-1", "off")]);

        var activeRolls = DeterministicDetectionLoop.RollTick(seed, 0, trials, unitRadarEmcon: null);
        var catalogRolls = DeterministicDetectionLoop.RollTick(seed, 0, trials, unitRadarEmcon: null, catalog: catalog);

        Assert.Single(activeRolls);
        Assert.Empty(catalogRolls);
    }

    [Fact]
    public void PhaseB_Scenario_emcon_overrides_catalog_emcon_for_detection()
    {
        var seed = SimSeed.FromScenario(42);
        var trials = new[]
        {
            new ScenarioDetectionTrial("u1", "radar-1", "hostile-1", "c1", 1.0, RequiresActiveRadar: true),
        };
        var catalog = new InMemoryCatalogReader(
            [new CatalogSensorBinding("u1", "radar-1", 1.0, "fixture-radar1")],
            emcon: [new CatalogEmcon("u1", "free", "radar-1", "off")]);
        var scenarioEmcon = new Dictionary<string, EmconState> { ["u1"] = EmconState.Active };

        var rolls = DeterministicDetectionLoop.RollTick(seed, 0, trials, scenarioEmcon, catalog: catalog);

        Assert.Single(rolls);
    }

    [Theory]
    [InlineData("off", EmconState.Off)]
    [InlineData("standby", EmconState.Passive)]
    [InlineData("active", EmconState.Active)]
    public void PhaseB_Catalog_posture_maps_to_emcon_state(string posture, EmconState expected)
    {
        Assert.Equal(expected, CatalogRadarEmconResolver.MapPosture(posture));
    }

    // PlatformWorkbookValidator.AllowedEmconPostures validates "Posture" against off/standby/active
    // using StringComparer.OrdinalIgnoreCase (see ProjectAegis.Data/Platform/PlatformWorkbookValidator.cs),
    // so a workbook author who types "Off"/"STANDBY"/"Active" passes catalog validation cleanly. But
    // MapPosture's switch only matches the exact lowercase literals, so any differently-cased-but-valid
    // posture silently falls into the `_ => EmconState.Active` default instead of the doctrine-intended
    // state — e.g. a unit authored as radar "Off" (EMCON silent) is misreported as radiating (Active),
    // defeating EMCON discipline for detection rolls and the MvpEngagementResolver RadarEmconActive gate.
    [Theory]
    [InlineData("Off", EmconState.Off)]
    [InlineData("OFF", EmconState.Off)]
    [InlineData("Standby", EmconState.Passive)]
    [InlineData("STANDBY", EmconState.Passive)]
    [InlineData("Active", EmconState.Active)]
    public void PhaseB_Catalog_posture_mapping_is_case_insensitive(string posture, EmconState expected)
    {
        Assert.Equal(expected, CatalogRadarEmconResolver.MapPosture(posture));
    }
}