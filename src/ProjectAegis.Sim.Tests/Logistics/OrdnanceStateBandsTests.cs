using ProjectAegis.Sim.Logistics;
using Xunit;

namespace ProjectAegis.Sim.Tests.Logistics;

public sealed class OrdnanceStateBandsTests
{
    [Theory]
    [InlineData(5, 1, "NOMINAL")]
    [InlineData(1, 1, "SHOTGUN")]
    [InlineData(0, 1, "WINCHESTER")]
    [InlineData(2, 2, "SHOTGUN")]
    [InlineData(1, 0, "NOMINAL")] // shotgun disabled
    [InlineData(0, 0, "WINCHESTER")]
    public void Resolve_matches_doctrine_bands(int remaining, int shotgunThreshold, string expected)
    {
        Assert.Equal(expected, OrdnanceStateBands.Resolve(remaining, shotgunThreshold));
    }
}
