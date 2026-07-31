namespace ProjectAegis.Sim.Tests.Engage;

using ProjectAegis.Sim.Engage;
using Xunit;

public sealed class LogisticsShotgunEngageGateTests
{
    [Fact]
    public void Evaluate_denies_multi_salvo_in_shotgun_band()
    {
        // remaining 1, threshold 1 → SHOTGUN; salvo 2 denied
        Assert.Equal(
            EngagementAbortReason.ShotgunOrdnance,
            LogisticsShotgunEngageGate.Evaluate(roundsRemaining: 1, salvoSize: 2, shotgunRoundsThreshold: 1));
    }

    [Fact]
    public void Evaluate_allows_single_salvo_in_shotgun_band()
    {
        Assert.Null(LogisticsShotgunEngageGate.Evaluate(1, salvoSize: 1, shotgunRoundsThreshold: 1));
    }

    [Fact]
    public void Evaluate_allows_multi_salvo_when_nominal()
    {
        Assert.Null(LogisticsShotgunEngageGate.Evaluate(roundsRemaining: 4, salvoSize: 2, shotgunRoundsThreshold: 1));
    }

    [Fact]
    public void Evaluate_disabled_when_threshold_zero()
    {
        Assert.Null(LogisticsShotgunEngageGate.Evaluate(1, salvoSize: 3, shotgunRoundsThreshold: 0));
    }

    [Fact]
    public void Resolver_aborts_with_ShotgunOrdnance_when_multi_salvo_in_band()
    {
        var world = new DictionaryEngageWorldQuery();
        var magazines = new MagazineLedger();
        magazines.SetRounds(1, 0, 1); // SHOTGUN at threshold 1
        var request = new EngageRequest(1, 2, 0, 10);
        world.Set(
            request,
            new EngageContext(
                50_000,
                new WeaponEnvelope(1_000, 100_000),
                RoundsRemaining: 1,
                HasFireControlTrack: true,
                SalvoSize: 2,
                ShotgunRoundsThreshold: 1));
        var resolver = new MvpEngagementResolver(world, magazines);
        var result = resolver.Resolve(in request);
        Assert.False(result.Launched);
        Assert.Equal(EngagementAbortReason.ShotgunOrdnance, result.AbortReason);
        Assert.Equal("SHOTGUN_ORDNANCE", EngagementAbortReasonCodes.ToLogCode(result.AbortReason));
        Assert.Equal(1, magazines.GetRounds(1, 0)); // not consumed
    }
}
