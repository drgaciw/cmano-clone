using ProjectAegis.Sim.Scenario;
using ProjectAegis.Sim.Sensors;
using NUnit.Framework;

namespace ProjectAegis.Sim.Tests.Sensors;

public sealed class ScenarioJamResolverTests
{
    [Test]
    public void Full_jam_on_target_zeroes_effective_pd()
    {
        var jammers = new[] { new ScenarioJammer("hostile-1", 1.0, ActiveFromTick: 1) };
        var jam = ScenarioJamResolver.ResolveJam("u1", "hostile-1", 1, jammers);
        var pd = DetectionProbability.ComputePd(1.0, jamStrength: jam);
        Assert.That(pd, Is.EqualTo(0));
    }

    [Test]
    public void Jam_after_active_tick_only_applies_when_eligible()
    {
        var jammers = new[] { new ScenarioJammer("hostile-1", 1.0, ActiveFromTick: 2) };
        Assert.That(ScenarioJamResolver.ResolveJam("u1", "hostile-1", 1, jammers), Is.EqualTo(0));
        Assert.That(ScenarioJamResolver.ResolveJam("u1", "hostile-1", 2, jammers), Is.EqualTo(1.0));
    }
}