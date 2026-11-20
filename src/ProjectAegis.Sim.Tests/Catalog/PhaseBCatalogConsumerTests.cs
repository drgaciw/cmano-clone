using ProjectAegis.Data.Catalog;
using ProjectAegis.Sim.Catalog;
using ProjectAegis.Sim.Core;
using ProjectAegis.Sim.Policy;
using ProjectAegis.Sim.Scenario;
using ProjectAegis.Sim.Sensors;
using NUnit.Framework;
using ProjectAegis.Sim.Tests;

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

    [Test]
    public void PhaseB_Legacy_catalog_without_signature_preserves_trial_basePd_and_envMask()
    {
        var profile = CatalogProfile();
        var catalog = InMemoryCatalogReader.BalticPatrolFixture();

        var trials = DetectionTrialResolver.Resolve(profile, catalog);

        AssertEx.Single(trials);
        Assert.That(trials[0].BasePd, Is.EqualTo(1.0));
        Assert.That(trials[0].EnvMask, Is.EqualTo(1.0));
    }

    [Test]
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

        Assert.That(baseline[0].EnvMask, Is.EqualTo(1.0));
        Assert.That(
            Math.Round((double)(modified[0].EnvMask), 6, MidpointRounding.AwayFromZero),
            Is.EqualTo(Math.Round((double)(0.9), 6, MidpointRounding.AwayFromZero)));
        Assert.That(modified[0].BasePd, Is.EqualTo(1.0));

        var seed = SimSeed.FromScenario(42);
        var baselineRoll = DeterministicDetectionLoop.RollTick(seed, 0, baseline, unitRadarEmcon: null);
        var modifiedRoll = DeterministicDetectionLoop.RollTick(seed, 0, modified, unitRadarEmcon: null);

        Assert.That(modifiedRoll[0].Pd, Is.Not.EqualTo(baselineRoll[0].Pd));
    }

    [TestCase(-40, 0.7)]
    [TestCase(-10, 1.0)]
    [TestCase(20, 1.0)]
    public void PhaseB_Rcs_boundary_values_apply_predictable_envMask(double rcsDbsm, double expectedEnvMask)
    {
        var signature = new CatalogSignature("u1", RcsBandDbsm: rcsDbsm);
        var envMask = PhaseBCatalogDetectionModifier.ApplyEnvMask(1.0, signature);
        Assert.That(
            Math.Round((double)(envMask), 6, MidpointRounding.AwayFromZero),
            Is.EqualTo(Math.Round((double)(expectedEnvMask), 6, MidpointRounding.AwayFromZero)));
    }

    [Test]
    public void PhaseB_Unreferenced_signature_platform_leaves_trial_unchanged()
    {
        var profile = CatalogProfile();
        var catalog = new InMemoryCatalogReader(
            [
                new CatalogSensorBinding("u1", "radar-1", 1.0, "fixture-radar1"),
            ],
            signatures: [new CatalogSignature("other-platform", RcsBandDbsm: 30)]);

        var trials = DetectionTrialResolver.Resolve(profile, catalog);

        AssertEx.Single(trials);
        Assert.That(trials[0].EnvMask, Is.EqualTo(1.0));
        Assert.That(trials[0].BasePd, Is.EqualTo(1.0));
    }

    [Test]
    public void PhaseB_Legacy_catalog_without_mobility_preserves_launch_readiness()
    {
        var catalog = InMemoryCatalogReader.BalticPatrolFixture();

        var readiness = PhaseBCatalogMobilityReadinessStub.EvaluateLaunchReadiness("u1", catalog);

        Assert.That(readiness.CatalogResolved, Is.False);
        Assert.That(readiness.ReadyForLaunch, Is.True);
        Assert.That(readiness.MobilityScore, Is.EqualTo(PhaseBCatalogMobilityReadinessStub.NeutralMobilityScore));
    }

    [Test]
    public void PhaseB_Committed_zero_range_mobility_blocks_launch_readiness()
    {
        var catalog = new InMemoryCatalogReader(
            [new CatalogSensorBinding("u1", "radar-1", 1.0, "fixture-radar1")],
            mobility: [new CatalogMobility("u1", MaxSpeedKnots: 32, RangeNm: 0)]);

        var readiness = PhaseBCatalogMobilityReadinessStub.EvaluateLaunchReadiness("u1", catalog);

        Assert.That(readiness.CatalogResolved, Is.True);
        Assert.That(readiness.ReadyForLaunch, Is.False);
        Assert.That(readiness.MobilityScore, Is.EqualTo(0.0));
    }

    [Test]
    public void PhaseB_Baltic_fixture_mobility_changes_readiness_policy_outcome()
    {
        var profile = new ScenarioPolicyProfile(EffectivePolicy.DefaultFree);
        var legacy = InMemoryCatalogReader.BalticPatrolFixture();
        var withMobility = InMemoryCatalogReader.BalticPhaseBFixture();

        var baseline = ReadinessPolicyEvaluator.EvaluateUnit("u1", profile, legacy);
        var modified = ReadinessPolicyEvaluator.EvaluateUnit("u1", profile, withMobility);

        Assert.That(baseline.CatalogResolved, Is.False);
        Assert.That(modified.CatalogResolved, Is.True);
        Assert.That(modified.ReadyForLaunch, Is.True);
        Assert.That(modified.ReadinessScore > 0.5, Is.True);
    }

    [Test]
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

        AssertEx.Single(activeRolls);
        Assert.That(catalogRolls, Is.Empty);
    }

    [Test]
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

        AssertEx.Single(rolls);
    }

    [TestCase("off", EmconState.Off)]
    [TestCase("standby", EmconState.Passive)]
    [TestCase("active", EmconState.Active)]
    public void PhaseB_Catalog_posture_maps_to_emcon_state(string posture, EmconState expected)
    {
        Assert.That(CatalogRadarEmconResolver.MapPosture(posture), Is.EqualTo(expected));
    }

    // PlatformWorkbookValidator.AllowedEmconPostures validates "Posture" against off/standby/active
    // using StringComparer.OrdinalIgnoreCase (see ProjectAegis.Data/Platform/PlatformWorkbookValidator.cs),
    // so a workbook author who types "Off"/"STANDBY"/"Active" passes catalog validation cleanly. But
    // MapPosture's switch only matches the exact lowercase literals, so any differently-cased-but-valid
    // posture silently falls into the `_ => EmconState.Active` default instead of the doctrine-intended
    // state — e.g. a unit authored as radar "Off" (EMCON silent) is misreported as radiating (Active),
    // defeating EMCON discipline for detection rolls and the MvpEngagementResolver RadarEmconActive gate.

    [TestCase("Off", EmconState.Off)]
    [TestCase("OFF", EmconState.Off)]
    [TestCase("Standby", EmconState.Passive)]
    [TestCase("STANDBY", EmconState.Passive)]
    [TestCase("Active", EmconState.Active)]
    public void PhaseB_Catalog_posture_mapping_is_case_insensitive(string posture, EmconState expected)
    {
        Assert.That(CatalogRadarEmconResolver.MapPosture(posture), Is.EqualTo(expected));
    }
}