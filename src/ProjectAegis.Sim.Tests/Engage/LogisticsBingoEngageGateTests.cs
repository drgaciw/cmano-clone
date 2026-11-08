namespace ProjectAegis.Sim.Tests.Engage;

using ProjectAegis.Sim.Engage;
using Xunit;

public sealed class LogisticsBingoEngageGateTests
{
    [Fact]
    public void Evaluate_blocks_when_LogisticsBingoBlocked()
    {
        var ctx = new EngageContext(
            50_000,
            new WeaponEnvelope(1_000, 100_000),
            RoundsRemaining: 2,
            HasFireControlTrack: true,
            LogisticsBingoBlocked: true);
        Assert.Equal(EngagementAbortReason.BingoFuel, LogisticsBingoEngageGate.Evaluate(in ctx));
    }

    [Fact]
    public void Evaluate_allows_when_not_bingo()
    {
        var ctx = new EngageContext(
            50_000,
            new WeaponEnvelope(1_000, 100_000),
            RoundsRemaining: 2,
            HasFireControlTrack: true);
        Assert.Null(LogisticsBingoEngageGate.Evaluate(in ctx));
    }

    [Fact]
    public void Resolver_aborts_with_BingoFuel_when_context_blocked()
    {
        var world = new DictionaryEngageWorldQuery();
        var magazines = new MagazineLedger();
        magazines.SetRounds(1, 0, 4);
        var request = new EngageRequest(1, 2, 0, 10);
        world.Set(
            request,
            new EngageContext(
                50_000,
                new WeaponEnvelope(1_000, 100_000),
                RoundsRemaining: 4,
                HasFireControlTrack: true,
                LogisticsBingoBlocked: true));
        var resolver = new MvpEngagementResolver(world, magazines);
        var result = resolver.Resolve(in request);
        Assert.False(result.Launched);
        Assert.Equal(EngagementAbortReason.BingoFuel, result.AbortReason);
        Assert.Equal("BINGO_FUEL", EngagementAbortReasonCodes.ToLogCode(result.AbortReason));
    }
}
