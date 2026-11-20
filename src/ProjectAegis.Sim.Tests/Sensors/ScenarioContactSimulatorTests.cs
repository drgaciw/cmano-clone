using ProjectAegis.Sim.Scenario;
using ProjectAegis.Sim.Sensors;
using NUnit.Framework;

namespace ProjectAegis.Sim.Tests.Sensors;

public sealed class ScenarioContactSimulatorTests
{
    [Test]
    public void Same_seed_schedule_yields_identical_transition_list()
    {
        var seeds = new[]
        {
            new ScenarioContactSeed("u1", "hostile-2", "c2", AppearAtTick: 2),
            new ScenarioContactSeed("u1", "hostile-1", "c1", AppearAtTick: 1),
        };

        var a = RunTransitions(seeds);
        var b = RunTransitions(seeds);
        Assert.That(b, Is.EqualTo(a));
        Assert.That(a.Count, Is.EqualTo(2));
        Assert.That(a[0].TargetId, Is.EqualTo("hostile-1"));
        Assert.That(a[1].TargetId, Is.EqualTo("hostile-2"));
    }

    [Test]
    public void Primary_target_is_lexicographically_smallest_active()
    {
        var sim = new ScenarioContactSimulator(
        [
            new ScenarioContactSeed("u1", "hostile-2", "c2", 1),
            new ScenarioContactSeed("u1", "hostile-1", "c1", 1),
        ]);
        sim.Tick(1, 1.0);
        Assert.That(sim.PrimaryTargetId, Is.EqualTo("hostile-1"));
        Assert.That(sim.ActiveCount, Is.EqualTo(2));
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