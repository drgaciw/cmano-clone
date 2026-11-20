using ProjectAegis.Sim.Core;
using ProjectAegis.Sim.Glossary;
using ProjectAegis.Sim.Scenario;
using ProjectAegis.Sim.Sensors;
using NUnit.Framework;
using ProjectAegis.Sim.Tests;

namespace ProjectAegis.Sim.Tests.Sensors;

/// <summary>ENV-02 / DRG-379: radar horizon + terrain masking.</summary>
public sealed class LineOfSightEvaluatorTests
{
    [Test]
    public void Horizon_range_matches_four_thirds_earth_formula()
    {
        // 20 m mast vs 5 m sea-skimmer ≈ 18.4 km + 9.2 km on a 4/3 earth.
        var d = LineOfSightEvaluator.HorizonRangeMeters(20, 5, LineOfSightEvaluator.RadarKFactor);
        Assert.That(d, Is.InRange(27_500, 27_700));
    }

    [Test]
    public void Optical_horizon_is_shorter_than_radar_horizon()
    {
        var radar = LineOfSightEvaluator.HorizonRangeMeters(30, 30, LineOfSightEvaluator.RadarKFactor);
        var optical = LineOfSightEvaluator.HorizonRangeMeters(30, 30, LineOfSightEvaluator.OpticalKFactor);
        Assert.That(optical < radar, Is.True);
    }

    [Test]
    public void Low_flyer_beyond_horizon_is_blocked_with_reason_code()
    {
        var g = new ScenarioLosGeometry("u1", "h1", 20, 15, 40_000);
        var r = LineOfSightEvaluator.Evaluate(g, SensorModality.Radar);
        Assert.That(r.Clear, Is.False);
        Assert.That(r.BlockCode, Is.EqualTo(AbortReasonCatalog.Sensor.LOS_RADAR_HORIZON));
    }

    [Test]
    public void High_altitude_target_at_same_range_is_visible()
    {
        var g = new ScenarioLosGeometry("u1", "h1", 20, 9_000, 40_000);
        Assert.That(LineOfSightEvaluator.Evaluate(g, SensorModality.Radar).Clear, Is.True);
    }

    [Test]
    public void Ridge_between_observer_and_target_masks_with_terrain_code()
    {
        var ridge = new[] { new TerrainProfileSample(5_000, 400) };
        var g = new ScenarioLosGeometry("u1", "h1", 50, 100, 10_000, ridge);
        var r = LineOfSightEvaluator.Evaluate(g, SensorModality.Radar);
        Assert.That(r.Clear, Is.False);
        Assert.That(r.BlockCode, Is.EqualTo(AbortReasonCatalog.Sensor.LOS_TERRAIN_MASK));
    }

    [Test]
    public void Low_terrain_below_ray_does_not_mask()
    {
        var hills = new[] { new TerrainProfileSample(2_000, 10), new TerrainProfileSample(8_000, 20) };
        var g = new ScenarioLosGeometry("u1", "h1", 300, 3_000, 10_000, hills);
        Assert.That(LineOfSightEvaluator.Evaluate(g, SensorModality.Radar).Clear, Is.True);
    }

    [Test]
    public void Samples_outside_path_are_ignored()
    {
        var samples = new[] { new TerrainProfileSample(0, 5_000), new TerrainProfileSample(10_000, 5_000) };
        var g = new ScenarioLosGeometry("u1", "h1", 300, 3_000, 10_000, samples);
        Assert.That(LineOfSightEvaluator.Evaluate(g, SensorModality.Radar).Clear, Is.True);
    }

    [Test]
    public void Terrain_samples_beyond_budget_cap_are_not_evaluated()
    {
        var samples = new TerrainProfileSample[LineOfSightEvaluator.MaxTerrainSamplesPerPair + 1];
        for (var i = 0; i < samples.Length - 1; i++)
        {
            samples[i] = new TerrainProfileSample(10 + i, 0);
        }

        samples[^1] = new TerrainProfileSample(5_000, 10_000); // would mask, but is past the cap
        var g = new ScenarioLosGeometry("u1", "h1", 300, 3_000, 10_000, samples);
        Assert.That(LineOfSightEvaluator.Evaluate(g, SensorModality.Radar).Clear, Is.True);
    }

    [Test]
    public void Detection_loop_reserves_draw_index_when_los_blocks()
    {
        var seed = SimSeed.FromScenario(42);
        var trials = new[]
        {
            new ScenarioDetectionTrial("u1", "radar-1", "h1", "c1", 0.5),
            new ScenarioDetectionTrial("u1", "radar-1", "h2", "c2", 0.5),
        };
        var env = new DetectionEnvironment(
            new LosGeometryTable(new[] { new ScenarioLosGeometry("u1", "h1", 20, 10, 60_000) }),
            new Dictionary<string, RadarEccmProfile>());
        var blocks = new List<DetectionLosBlock>();

        var gated = DeterministicDetectionLoop.RollTick(seed, 3, trials, null, environment: env, losBlocks: blocks);
        var unblocked = DeterministicDetectionLoop.RollTick(seed, 3, trials, null);

        var only = AssertEx.Single(gated);
        Assert.That(only.Trial.TargetId, Is.EqualTo("h2"));
        Assert.That(only, Is.EqualTo(unblocked.Single(r => r.Trial.TargetId == "h2")));
        var block = AssertEx.Single(blocks);
        Assert.That(block.SimTick, Is.EqualTo(3UL));
        Assert.That(block.ObserverId, Is.EqualTo("u1"));
        Assert.That(block.SensorId, Is.EqualTo("radar-1"));
        Assert.That(block.TargetId, Is.EqualTo("h1"));
        Assert.That(block.BlockCode, Is.EqualTo(AbortReasonCatalog.Sensor.LOS_RADAR_HORIZON));
    }

    [Test]
    public void Terrain_mask_leaves_unrelated_contact_results_unchanged()
    {
        var seed = SimSeed.FromScenario(42);
        var trials = new[]
        {
            new ScenarioDetectionTrial("u1", "radar-1", "h1", "c1", 0.5),
            new ScenarioDetectionTrial("u1", "radar-1", "h2", "c2", 0.5),
            new ScenarioDetectionTrial("u1", "radar-1", "h3", "c3", 0.4),
        };
        var clear = new DetectionEnvironment(
            new LosGeometryTable(new[] { new ScenarioLosGeometry("u1", "h1", 50, 100, 10_000) }),
            new Dictionary<string, RadarEccmProfile>());
        var masked = new DetectionEnvironment(
            new LosGeometryTable(new[]
            {
                new ScenarioLosGeometry(
                    "u1",
                    "h1",
                    50,
                    100,
                    10_000,
                    new[] { new TerrainProfileSample(5_000, 400) }),
            }),
            new Dictionary<string, RadarEccmProfile>());
        var blocks = new List<DetectionLosBlock>();

        var withoutMask = DeterministicDetectionLoop.RollTick(seed, 3, trials, null, environment: clear);
        var withMask = DeterministicDetectionLoop.RollTick(seed, 3, trials, null, environment: masked, losBlocks: blocks);

        Assert.That(withoutMask.Count, Is.EqualTo(3));
        Assert.That(withMask.Select(r => r.Trial.TargetId), Is.EqualTo(new[] { "h2", "h3" }));
        Assert.That(AssertEx.Single(blocks).BlockCode, Is.EqualTo(AbortReasonCatalog.Sensor.LOS_TERRAIN_MASK));
        Assert.That(withMask.Single(r => r.Trial.TargetId == "h2"), Is.EqualTo(withoutMask.Single(r => r.Trial.TargetId == "h2")));
        Assert.That(withMask.Single(r => r.Trial.TargetId == "h3"), Is.EqualTo(withoutMask.Single(r => r.Trial.TargetId == "h3")));
    }

    [Test]
    public void Detection_loop_without_environment_is_unchanged()
    {
        var seed = SimSeed.FromScenario(7);
        var trials = new[] { new ScenarioDetectionTrial("u1", "radar-1", "h1", "c1", 0.6) };
        var a = DeterministicDetectionLoop.RollTick(seed, 5, trials, null);
        var b = DeterministicDetectionLoop.RollTick(seed, 5, trials, null, environment: DetectionEnvironment.None);
        Assert.That(b, Is.EqualTo(a));
    }

    [Test]
    public void Pd_simulator_exposes_last_tick_los_blocks()
    {
        var trials = new[] { new ScenarioDetectionTrial("u1", "radar-1", "h1", "c1", 1.0) };
        var env = new DetectionEnvironment(
            new LosGeometryTable(new[] { new ScenarioLosGeometry("u1", "h1", 20, 10, 60_000) }),
            new Dictionary<string, RadarEccmProfile>());
        var sim = new PdDetectionContactSimulator(SimSeed.FromScenario(1), trials, environment: env);
        var transitions = sim.Tick(1, 1);
        Assert.That(transitions, Is.Empty);
        Assert.That(sim.ActiveCount, Is.EqualTo(0));
        AssertEx.Single(sim.LastLosBlocks);
    }
}
