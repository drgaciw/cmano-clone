using ProjectAegis.Sim.Scenario;
using ProjectAegis.Sim.Sensors;
using Xunit;

namespace ProjectAegis.Sim.Tests.Sensors;

public sealed class ScenarioContactSimulatorTests
{
    [Fact]
    public void Same_seed_schedule_yields_identical_transition_list()
    {
        var seeds = new[]
        {
            new ScenarioContactSeed("u1", "hostile-2", "c2", AppearAtTick: 2),
            new ScenarioContactSeed("u1", "hostile-1", "c1", AppearAtTick: 1),
        };

        var a = RunTransitions(seeds);
        var b = RunTransitions(seeds);
        Assert.Equal(a, b);
        Assert.Equal(2, a.Count);
        Assert.Equal("hostile-1", a[0].TargetId);
        Assert.Equal("hostile-2", a[1].TargetId);
    }

    [Fact]
    public void Primary_target_is_lexicographically_smallest_active()
    {
        var sim = new ScenarioContactSimulator(
        [
            new ScenarioContactSeed("u1", "hostile-2", "c2", 1),
            new ScenarioContactSeed("u1", "hostile-1", "c1", 1),
        ]);
        sim.Tick(1, 1.0);
        Assert.Equal("hostile-1", sim.PrimaryTargetId);
        Assert.Equal(2, sim.ActiveCount);
    }

    private static List<ContactTransition> RunTransitions(IReadOnlyList<ScenarioContactSeed> seeds)
    {
        var sim = new ScenarioContactSimulator(seeds);
        var all = new List<ContactTransition>();
        for (ulong t = 0; t <= 3; t++)
        {
            all.AddRange(sim.Tick(t, t));
        }

        return all;
    }
}