namespace ProjectAegis.Sim.Tests.Engage;

using ProjectAegis.Sim.Engage;
using Xunit;

public sealed class LogisticsWinchesterEngageGateTests
{
    [Fact]
    public void Evaluate_blocks_when_rounds_zero()
    {
        Assert.Equal(
            EngagementAbortReason.WinchesterOrdnance,
            LogisticsWinchesterEngageGate.Evaluate(roundsRemaining: 0));
    }

    [Fact]
    public void Evaluate_allows_when_rounds_positive()
    {
        Assert.Null(LogisticsWinchesterEngageGate.Evaluate(roundsRemaining: 1));
        Assert.Null(LogisticsWinchesterEngageGate.Evaluate(roundsRemaining: 4));
    }

    [Fact]
    public void Resolver_aborts_with_WinchesterOrdnance_when_empty()
    {
        var world = new DictionaryEngageWorldQuery();
        var magazines = new MagazineLedger();
        magazines.SetRounds(1, 0, 0);
        var request = new EngageRequest(1, 2, 0, 10);
        world.Set(
            request,
            new EngageContext(
                50_000,
                new WeaponEnvelope(1_000, 100_000),
                RoundsRemaining: 0,
                HasFireControlTrack: true));
        var resolver = new MvpEngagementResolver(world, magazines);
        var result = resolver.Resolve(in request);
        Assert.False(result.Launched);
        Assert.Equal(EngagementAbortReason.WinchesterOrdnance, result.AbortReason);
        Assert.Equal("WINCHESTER_ORDNANCE", EngagementAbortReasonCodes.ToLogCode(result.AbortReason));
        Assert.Equal(0, magazines.GetRounds(1, 0)); // not consumed
    }
}
