using ProjectAegis.Sim.Core;
using NUnit.Framework;

namespace ProjectAegis.Sim.Tests.Core;

public sealed class SimWorldHashTests
{
    [Test]
    public void Combine_is_stable_for_same_inputs()
    {
        var a = SimWorldHash.Combine(100, 200, 300);
        var b = SimWorldHash.Combine(100, 200, 300);
        Assert.That(b, Is.EqualTo(a));
    }

    [Test]
    public void Detection_layer_changes_composite()
    {
        var baseHash = SimWorldHash.Combine(1000, 0, 50);
        var withDetection = SimWorldHash.Combine(1000, 999, 50);
        Assert.That(withDetection, Is.Not.EqualTo(baseHash));
    }
}