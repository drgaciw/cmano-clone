using ProjectAegis.Sim.Core;
using ProjectAegis.Sim.Scenario;
using ProjectAegis.Sim.Sensors;
using NUnit.Framework;

namespace ProjectAegis.Sim.Tests.Sensors;

public sealed class PdDetectionContactSimulatorTests
{
    [Test]
    public void Constructor_preserves_roll_order_with_pre_sorted_trials()
    {
        var trials = new[]
        {
            new ScenarioDetectionTrial("u2", "s1", "t9", "c9", 1.0),
            new ScenarioDetectionTrial("u1", "s2", "t1", "c2", 1.0),
            new ScenarioDetectionTrial("u1", "s1", "t2", "c1", 1.0),
        };
        var seed = SimSeed.FromScenario(4242);
        var sim = new PdDetectionContactSimulator(seed, trials);

        var a = DeterministicDetectionLoop.RollTick(seed, 3, trials, null);
        var b = DeterministicDetectionLoop.RollTick(seed, 3, trials, null);
        sim.Tick(3, 3.0);
        sim.Tick(3, 3.0);

        Assert.That(b.Count, Is.EqualTo(a.Count));
        for (var i = 0; i < a.Count; i++)
        {
            Assert.That(b[i].Trial.TargetId, Is.EqualTo(a[i].Trial.TargetId));
            Assert.That(b[i].Draw, Is.EqualTo(a[i].Draw));
        }
    }

    [Test]
    public void Two_runs_emit_identical_contact_transition_ordering()
    {
        var trials = new[]
        {
            new ScenarioDetectionTrial("u1", "radar-1", "hostile-2", "c2", 1.0),
            new ScenarioDetectionTrial("u1", "radar-1", "hostile-1", "c1", 1.0),
        };
        var seed = SimSeed.FromScenario(99);

        var first = RunTransitions(seed, trials, ticks: 4);
        var second = RunTransitions(seed, trials, ticks: 4);

        Assert.That(second.Count, Is.EqualTo(first.Count));
        for (var i = 0; i < first.Count; i++)
        {
            Assert.That(second[i].ContactId, Is.EqualTo(first[i].ContactId));
            Assert.That(second[i].PreviousState, Is.EqualTo(first[i].PreviousState));
            Assert.That(second[i].NewState, Is.EqualTo(first[i].NewState));
            Assert.That(second[i].SimTick, Is.EqualTo(first[i].SimTick));
        }
    }

    [Test]
    public void Primary_target_tracks_blue_force_for_red_side_engage()
    {
        var trials = new[]
        {
            new ScenarioDetectionTrial("ucav-red", "internal-ir", "u1", "c-friendly", 1.0, RequiresActiveRadar: false),
            new ScenarioDetectionTrial("u1", "radar-1", "hostile-1", "c1", 1.0),
        };
        var sim = new PdDetectionContactSimulator(SimSeed.FromScenario(42), trials);
        sim.Tick(1, 1.0);
        Assert.That(sim.PrimaryBlueForceTargetId, Is.EqualTo("u1"));
        Assert.That(sim.PrimaryTargetId, Is.EqualTo("hostile-1"));
    }

    [Test]
    public void Primary_target_ignores_non_hostile_detections()
    {
        var trials = new[]
        {
            new ScenarioDetectionTrial("ucav-red", "internal-ir", "u1", "c-friendly", 1.0, RequiresActiveRadar: false),
            new ScenarioDetectionTrial("u1", "radar-1", "hostile-1", "c1", 1.0),
        };
        var sim = new PdDetectionContactSimulator(SimSeed.FromScenario(42), trials);
        sim.Tick(1, 1.0);
        Assert.That(sim.PrimaryTargetId, Is.EqualTo("hostile-1"));
        sim.ApplyTargetKill(2, 2.0, "hostile-1");

        Assert.That(sim.PrimaryTargetId, Is.Null);
    }

    private static List<ContactTransition> RunTransitions(
        SimSeed seed,
        IReadOnlyList<ScenarioDetectionTrial> trials,
        int ticks)
    {
        var sim = new PdDetectionContactSimulator(seed, trials);
        var transitions = new List<ContactTransition>();
        for (ulong tick = 1; tick <= (ulong)ticks; tick++)
        {
            transitions.AddRange(sim.Tick(tick, tick));
        }

        return transitions;
    }
}