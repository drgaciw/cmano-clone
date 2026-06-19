using ProjectAegis.Sim.Core;
using ProjectAegis.Sim.Scenario;
using ProjectAegis.Sim.Sensors;
using Xunit;

namespace ProjectAegis.Sim.Tests.Sensors;

public sealed class PdDetectionContactSimulatorTests
{
    [Fact]
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

        Assert.Equal(a.Count, b.Count);
        for (var i = 0; i < a.Count; i++)
        {
            Assert.Equal(a[i].Trial.TargetId, b[i].Trial.TargetId);
            Assert.Equal(a[i].Draw, b[i].Draw);
        }
    }

    [Fact]
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

        Assert.Equal(first.Count, second.Count);
        for (var i = 0; i < first.Count; i++)
        {
            Assert.Equal(first[i].ContactId, second[i].ContactId);
            Assert.Equal(first[i].PreviousState, second[i].PreviousState);
            Assert.Equal(first[i].NewState, second[i].NewState);
            Assert.Equal(first[i].SimTick, second[i].SimTick);
        }
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