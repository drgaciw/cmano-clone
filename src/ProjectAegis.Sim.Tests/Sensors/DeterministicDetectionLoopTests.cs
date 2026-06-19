using ProjectAegis.Sim.Core;
using ProjectAegis.Sim.Scenario;
using ProjectAegis.Sim.Sensors;
using Xunit;

namespace ProjectAegis.Sim.Tests.Sensors;

public sealed class DeterministicDetectionLoopTests
{
    [Fact]
    public void Same_seed_and_tick_yields_identical_roll_sequence()
    {
        var trials = new[]
        {
            new ScenarioDetectionTrial("u1", "radar-1", "hostile-2", "c2", 0.5),
            new ScenarioDetectionTrial("u1", "radar-1", "hostile-1", "c1", 0.5),
        };
        var seed = SimSeed.FromScenario(4242);
        var a = DeterministicDetectionLoop.RollTick(seed, 3, trials, null);
        var b = DeterministicDetectionLoop.RollTick(seed, 3, trials, null);
        Assert.Equal(a.Count, b.Count);
        for (var i = 0; i < a.Count; i++)
        {
            Assert.Equal(a[i].Trial.TargetId, b[i].Trial.TargetId);
            Assert.Equal(a[i].Pd, b[i].Pd);
            Assert.Equal(a[i].Draw, b[i].Draw);
            Assert.Equal(a[i].Detected, b[i].Detected);
        }
    }

    [Fact]
    public void Trials_are_sorted_by_observer_sensor_target()
    {
        var trials = new[]
        {
            new ScenarioDetectionTrial("u2", "s1", "t9", "c9", 1.0),
            new ScenarioDetectionTrial("u1", "s2", "t1", "c2", 1.0),
            new ScenarioDetectionTrial("u1", "s1", "t2", "c1", 1.0),
        };
        var results = DeterministicDetectionLoop.RollTick(SimSeed.FromScenario(1), 1, trials, null);
        Assert.Equal("u1", results[0].Trial.ObserverId);
        Assert.Equal("s1", results[0].Trial.SensorId);
        Assert.Equal("t2", results[0].Trial.TargetId);
        Assert.Equal("s2", results[1].Trial.SensorId);
        Assert.Equal("u2", results[2].Trial.ObserverId);
    }

    [Fact]
    public void BasePd_one_always_detects_basePd_zero_never_detects()
    {
        var always = DeterministicDetectionLoop.RollTick(
            SimSeed.FromScenario(9),
            1,
            [new ScenarioDetectionTrial("u1", "r1", "t1", "c1", 1.0)],
            null);
        Assert.True(always[0].Detected);

        var never = DeterministicDetectionLoop.RollTick(
            SimSeed.FromScenario(9),
            1,
            [new ScenarioDetectionTrial("u1", "r1", "t1", "c1", 0.0)],
            null);
        Assert.False(never[0].Detected);
    }

    [Fact]
    public void DetectionProbability_clamps_jam_and_env()
    {
        Assert.Equal(0.5, DetectionProbability.ComputePd(1.0, envMask: 0.5));
        Assert.Equal(0.25, DetectionProbability.ComputePd(1.0, jamStrength: 0.75));
        Assert.Equal(0.375, DetectionProbability.ComputePd(1.0, eccmFactor: 0.75, jamStrength: 0.5));
    }

    [Fact]
    public void Scenario_jammer_prevents_detection_when_pd_zeroed()
    {
        var trials = new[] { new ScenarioDetectionTrial("u1", "radar-1", "hostile-1", "c1", 1.0) };
        var jammers = new[] { new ScenarioJammer("hostile-1", 1.0, 1) };
        var rolls = DeterministicDetectionLoop.RollTick(SimSeed.FromScenario(42), 1, trials, null, jammers: jammers);
        Assert.Single(rolls);
        Assert.False(rolls[0].Detected);
        Assert.Equal(0, rolls[0].Pd);
    }
}