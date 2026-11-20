using ProjectAegis.Sim.Core;
using ProjectAegis.Sim.Scenario;
using ProjectAegis.Sim.Sensors;
using NUnit.Framework;

namespace ProjectAegis.Sim.Tests.Sensors;

public sealed class DeterministicDetectionLoopTests
{
    [Test]
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
        Assert.That(b.Count, Is.EqualTo(a.Count));
        for (var i = 0; i < a.Count; i++)
        {
            Assert.That(b[i].Trial.TargetId, Is.EqualTo(a[i].Trial.TargetId));
            Assert.That(b[i].Pd, Is.EqualTo(a[i].Pd));
            Assert.That(b[i].Draw, Is.EqualTo(a[i].Draw));
            Assert.That(b[i].Detected, Is.EqualTo(a[i].Detected));
        }
    }

    [Test]
    public void Trials_are_sorted_by_observer_sensor_target()
    {
        var trials = new[]
        {
            new ScenarioDetectionTrial("u2", "s1", "t9", "c9", 1.0),
            new ScenarioDetectionTrial("u1", "s2", "t1", "c2", 1.0),
            new ScenarioDetectionTrial("u1", "s1", "t2", "c1", 1.0),
        };
        var results = DeterministicDetectionLoop.RollTick(SimSeed.FromScenario(1), 1, trials, null);
        Assert.That(results[0].Trial.ObserverId, Is.EqualTo("u1"));
        Assert.That(results[0].Trial.SensorId, Is.EqualTo("s1"));
        Assert.That(results[0].Trial.TargetId, Is.EqualTo("t2"));
        Assert.That(results[1].Trial.SensorId, Is.EqualTo("s2"));
        Assert.That(results[2].Trial.ObserverId, Is.EqualTo("u2"));
    }

    [Test]
    public void Pre_sorted_trials_skip_per_tick_sort_allocation()
    {
        var unsorted = new[]
        {
            new ScenarioDetectionTrial("u2", "s1", "t9", "c9", 1.0),
            new ScenarioDetectionTrial("u1", "s2", "t1", "c2", 1.0),
            new ScenarioDetectionTrial("u1", "s1", "t2", "c1", 1.0),
        };
        var sorted = DeterministicDetectionLoop.SortTrialsCopy(unsorted);
        var seed = SimSeed.FromScenario(1);

        var sortedPath = DeterministicDetectionLoop.RollTick(seed, 1, sorted, null, trialsPreSorted: true);
        var legacyPath = DeterministicDetectionLoop.RollTick(seed, 1, unsorted, null);

        Assert.That(legacyPath.Count, Is.EqualTo(sortedPath.Count));
        for (var i = 0; i < sortedPath.Count; i++)
        {
            Assert.That(legacyPath[i].Trial.TargetId, Is.EqualTo(sortedPath[i].Trial.TargetId));
            Assert.That(legacyPath[i].Pd, Is.EqualTo(sortedPath[i].Pd));
            Assert.That(legacyPath[i].Draw, Is.EqualTo(sortedPath[i].Draw));
            Assert.That(legacyPath[i].Detected, Is.EqualTo(sortedPath[i].Detected));
        }
    }

    [Test]
    public void BasePd_one_always_detects_basePd_zero_never_detects()
    {
        var always = DeterministicDetectionLoop.RollTick(
            SimSeed.FromScenario(9),
            1,
            [new ScenarioDetectionTrial("u1", "r1", "t1", "c1", 1.0)],
            null);
        Assert.That(always[0].Detected, Is.True);

        var never = DeterministicDetectionLoop.RollTick(
            SimSeed.FromScenario(9),
            1,
            [new ScenarioDetectionTrial("u1", "r1", "t1", "c1", 0.0)],
            null);
        Assert.That(never[0].Detected, Is.False);
    }

    [Test]
    public void DetectionProbability_clamps_jam_and_env()
    {
        Assert.That(DetectionProbability.ComputePd(1.0, envMask: 0.5), Is.EqualTo(0.5));
        Assert.That(DetectionProbability.ComputePd(1.0, jamStrength: 0.75), Is.EqualTo(0.25));
        Assert.That(DetectionProbability.ComputePd(1.0, eccmFactor: 0.75, jamStrength: 0.5), Is.EqualTo(0.375));
    }

    [Test]
    public void Scenario_jammer_prevents_detection_when_pd_zeroed()
    {
        var trials = new[] { new ScenarioDetectionTrial("u1", "radar-1", "hostile-1", "c1", 1.0) };
        var jammers = new[] { new ScenarioJammer("hostile-1", 1.0, 1) };
        var rolls = DeterministicDetectionLoop.RollTick(SimSeed.FromScenario(42), 1, trials, null, jammers: jammers);
        AssertEx.Single(rolls);
        Assert.That(rolls[0].Detected, Is.False);
        Assert.That(rolls[0].Pd, Is.EqualTo(0));
    }
}