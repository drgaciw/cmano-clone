namespace ProjectAegis.Sim.Tests.Engage;

using ProjectAegis.Sim.Engage;
using NUnit.Framework;

public sealed class LogisticsShotgunEngageGateTests
{
    [Test]
    public void Evaluate_denies_multi_salvo_in_shotgun_band()
    {
        // remaining 1, threshold 1 → SHOTGUN; salvo 2 denied
        Assert.That(
            LogisticsShotgunEngageGate.Evaluate(roundsRemaining: 1, salvoSize: 2, shotgunRoundsThreshold: 1),
            Is.EqualTo(EngagementAbortReason.ShotgunOrdnance));
    }

    [Test]
    public void Evaluate_allows_single_salvo_in_shotgun_band()
    {
        Assert.That(LogisticsShotgunEngageGate.Evaluate(1, salvoSize: 1, shotgunRoundsThreshold: 1), Is.Null);
    }

    [Test]
    public void Evaluate_allows_multi_salvo_when_nominal()
    {
        Assert.That(LogisticsShotgunEngageGate.Evaluate(roundsRemaining: 4, salvoSize: 2, shotgunRoundsThreshold: 1), Is.Null);
    }

    [Test]
    public void Evaluate_disabled_when_threshold_zero()
    {
        Assert.That(LogisticsShotgunEngageGate.Evaluate(1, salvoSize: 3, shotgunRoundsThreshold: 0), Is.Null);
    }

    [Test]
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
        Assert.That(result.Launched, Is.False);
        Assert.That(result.AbortReason, Is.EqualTo(EngagementAbortReason.ShotgunOrdnance));
        Assert.That(EngagementAbortReasonCodes.ToLogCode(result.AbortReason), Is.EqualTo("SHOTGUN_ORDNANCE"));
        Assert.That(magazines.GetRounds(1, 0), Is.EqualTo(1)); // not consumed
    }
}
