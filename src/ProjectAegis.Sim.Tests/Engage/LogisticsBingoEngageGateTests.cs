namespace ProjectAegis.Sim.Tests.Engage;

using ProjectAegis.Sim.Engage;
using NUnit.Framework;

public sealed class LogisticsBingoEngageGateTests
{
    [Test]
    public void Evaluate_blocks_when_LogisticsBingoBlocked()
    {
        var ctx = new EngageContext(
            50_000,
            new WeaponEnvelope(1_000, 100_000),
            RoundsRemaining: 2,
            HasFireControlTrack: true,
            LogisticsBingoBlocked: true);
        Assert.That(LogisticsBingoEngageGate.Evaluate(in ctx), Is.EqualTo(EngagementAbortReason.BingoFuel));
    }

    [Test]
    public void Evaluate_allows_when_not_bingo()
    {
        var ctx = new EngageContext(
            50_000,
            new WeaponEnvelope(1_000, 100_000),
            RoundsRemaining: 2,
            HasFireControlTrack: true);
        Assert.That(LogisticsBingoEngageGate.Evaluate(in ctx), Is.Null);
    }

    [Test]
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
        Assert.That(result.Launched, Is.False);
        Assert.That(result.AbortReason, Is.EqualTo(EngagementAbortReason.BingoFuel));
        Assert.That(EngagementAbortReasonCodes.ToLogCode(result.AbortReason), Is.EqualTo("BINGO_FUEL"));
    }
}
