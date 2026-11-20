using ProjectAegis.Sim.Logistics;
using NUnit.Framework;

namespace ProjectAegis.Sim.Tests.Logistics;

public sealed class OrdnanceStateBandsTests
{

    [TestCase(5, 1, "NOMINAL")]
    [TestCase(1, 1, "SHOTGUN")]
    [TestCase(0, 1, "WINCHESTER")]
    [TestCase(2, 2, "SHOTGUN")]
    [TestCase(1, 0, "NOMINAL")] // shotgun disabled
    [TestCase(0, 0, "WINCHESTER")]
    public void Resolve_matches_doctrine_bands(int remaining, int shotgunThreshold, string expected)
    {
        Assert.That(OrdnanceStateBands.Resolve(remaining, shotgunThreshold), Is.EqualTo(expected));
    }
}
