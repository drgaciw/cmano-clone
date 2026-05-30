using ProjectAegis.Sim.Core;
using ProjectAegis.Sim.Time;
using Xunit;

namespace ProjectAegis.Sim.Tests.Core;

public sealed class SimTickRunnerTests
{
    [Fact]
    public void Same_seed_and_ticks_produce_identical_world_hash()
    {
        var a = RunTicks(9034412, 60);
        var b = RunTicks(9034412, 60);
        Assert.Equal(a, b);
    }

    [Fact]
    public void Different_seed_produces_different_world_hash()
    {
        var a = RunTicks(1, 10);
        var b = RunTicks(2, 10);
        Assert.NotEqual(a, b);
    }

    private static ulong RunTicks(ulong seed, int count)
    {
        var runner = new SimTickRunner(SimSeed.FromScenario(seed));
        for (var i = 0; i < count; i++)
        {
            runner.TickOnce(TimeCompressionMode.HeadlessBatch);
        }

        return runner.LastWorldHash;
    }
}
