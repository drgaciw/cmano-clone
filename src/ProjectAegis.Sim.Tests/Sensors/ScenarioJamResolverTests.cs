using ProjectAegis.Sim.Scenario;
using ProjectAegis.Sim.Sensors;
using Xunit;

namespace ProjectAegis.Sim.Tests.Sensors;

public sealed class ScenarioJamResolverTests
{
    [Fact]
    public void Full_jam_on_target_zeroes_effective_pd()
    {
        var jammers = new[] { new ScenarioJammer("hostile-1", 1.0, ActiveFromTick: 1) };
        var jam = ScenarioJamResolver.ResolveJam("u1", "hostile-1", 1, jammers);
        var pd = DetectionProbability.ComputePd(1.0, jamStrength: jam);
        Assert.Equal(0, pd);
    }

    [Fact]
    public void Jam_after_active_tick_only_applies_when_eligible()
    {
        var jammers = new[] { new ScenarioJammer("hostile-1", 1.0, ActiveFromTick: 2) };
        Assert.Equal(0, ScenarioJamResolver.ResolveJam("u1", "hostile-1", 1, jammers));
        Assert.Equal(1.0, ScenarioJamResolver.ResolveJam("u1", "hostile-1", 2, jammers));
    }
}