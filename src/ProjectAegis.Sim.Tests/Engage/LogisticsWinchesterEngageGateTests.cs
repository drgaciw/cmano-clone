namespace ProjectAegis.Sim.Tests.Engage;

using ProjectAegis.Sim.Engage;
using NUnit.Framework;

public sealed class LogisticsWinchesterEngageGateTests
{
    [Test]
    public void Evaluate_blocks_when_rounds_zero()
    {
        Assert.That(LogisticsWinchesterEngageGate.Evaluate(roundsRemaining: 0), Is.EqualTo(EngagementAbortReason.WinchesterOrdnance));
    }

    [Test]
    public void Evaluate_allows_when_rounds_positive()
    {
        Assert.That(LogisticsWinchesterEngageGate.Evaluate(roundsRemaining: 1), Is.Null);
        Assert.That(LogisticsWinchesterEngageGate.Evaluate(roundsRemaining: 4), Is.Null);
    }

    [Test]
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
        Assert.That(result.Launched, Is.False);
        Assert.That(result.AbortReason, Is.EqualTo(EngagementAbortReason.WinchesterOrdnance));
        Assert.That(EngagementAbortReasonCodes.ToLogCode(result.AbortReason), Is.EqualTo("WINCHESTER_ORDNANCE"));
        Assert.That(magazines.GetRounds(1, 0), Is.EqualTo(0)); // not consumed
    }
}
