using ProjectAegis.Sim.Engage;
using ProjectAegis.Sim.Policy;
using Xunit;

namespace ProjectAegis.Sim.Tests.Engage;

public sealed class MvpEngagementResolverTests
{
    [Fact]
    public void In_zone_with_ammo_launches_and_decrements_magazine()
    {
        var world = new DictionaryEngageWorldQuery();
        var magazines = new MagazineLedger();
        magazines.SetRounds(1, 0, 2);
        var resolver = new MvpEngagementResolver(world, magazines);
        var request = new EngageRequest(1, 2, 0, 0);
        world.Set(request, new EngageContext(50_000, new WeaponEnvelope(1_000, 100_000), 2, true));

        var result = resolver.Resolve(request);
        Assert.True(result.Launched);
        Assert.Equal(1, magazines.GetRounds(1, 0));
    }

    [Fact]
    public void Out_of_envelope_aborts()
    {
        var world = new DictionaryEngageWorldQuery();
        var resolver = new MvpEngagementResolver(world, new MagazineLedger());
        var request = new EngageRequest(1, 2, 0, 0);
        world.Set(request, new EngageContext(200_000, new WeaponEnvelope(1_000, 100_000), 5, true));

        var result = resolver.Resolve(request);
        Assert.False(result.Launched);
        Assert.Equal(EngagementAbortReason.OutOfEnvelope, result.AbortReason);
    }

    [Fact]
    public void Radar_emcon_off_aborts_before_track_check()
    {
        var world = new DictionaryEngageWorldQuery();
        var resolver = new MvpEngagementResolver(world, new MagazineLedger());
        var request = new EngageRequest(1, 2, 0, 0);
        world.Set(request, new EngageContext(50_000, new WeaponEnvelope(1_000, 100_000), 2, true, RadarEmconActive: false));

        var result = resolver.Resolve(request);
        Assert.Equal(EngagementAbortReason.EmconOff, result.AbortReason);
    }

    [Fact]
    public void Hold_fire_policy_denies_before_envelope_check()
    {
        var world = new DictionaryEngageWorldQuery();
        var evaluator = new PolicyEvaluator(_ => new EffectivePolicy(RoeLevel.HoldFire));
        var resolver = new MvpEngagementResolver(
            world,
            new MagazineLedger(),
            evaluator,
            _ => new EffectivePolicy(RoeLevel.HoldFire));
        var request = new EngageRequest(1, 2, 0, 0);
        world.Set(request, new EngageContext(50_000, new WeaponEnvelope(1_000, 100_000), 2, true));

        var result = resolver.Resolve(request);
        Assert.Equal(EngagementAbortReason.RoeHoldFire, result.AbortReason);
    }

    [Fact]
    public void Dlz_out_aborts_when_approaching_edge()
    {
        var world = new DictionaryEngageWorldQuery();
        var resolver = new MvpEngagementResolver(world, new MagazineLedger());
        var request = new EngageRequest(1, 2, 0, 0);
        world.Set(request, new EngageContext(90_000, new WeaponEnvelope(1_000, 100_000), 5, true));

        var result = resolver.Resolve(request);
        Assert.Equal(EngagementAbortReason.DlzOut, result.AbortReason);
    }

    [Fact]
    public void Killed_target_aborts_before_launch()
    {
        var world = new DictionaryEngageWorldQuery();
        var killed = new KilledTargetRegistry();
        killed.MarkKilled(2, "hostile-1");
        var resolver = new MvpEngagementResolver(world, new MagazineLedger(), killedTargets: killed);
        var request = new EngageRequest(1, 2, 0, 0);
        world.Set(request, new EngageContext(50_000, new WeaponEnvelope(1_000, 100_000), 2, true));

        var result = resolver.Resolve(request);
        Assert.Equal(EngagementAbortReason.TargetDestroyed, result.AbortReason);
    }
}
