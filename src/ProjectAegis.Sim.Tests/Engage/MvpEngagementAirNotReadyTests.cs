using ProjectAegis.Sim.Core;
using ProjectAegis.Sim.Engage;
using ProjectAegis.Sim.Glossary;
using Xunit;

namespace ProjectAegis.Sim.Tests.Engage;

public sealed class MvpEngagementAirNotReadyTests
{
    [Fact]
    public void Resolve_aborts_with_AIR_NOT_READY_when_air_ops_not_ready()
    {
        var world = new DictionaryEngageWorldQuery();
        var magazines = new MagazineLedger();
        var request = new EngageRequest(1, 2, MountId: 0, SimTick: 1);
        world.Set(
            request,
            new EngageContext(
                50_000,
                new WeaponEnvelope(1_000, 100_000),
                RoundsRemaining: 2,
                HasFireControlTrack: true,
                AirOperationsReady: false));

        var resolver = new MvpEngagementResolver(world, magazines);
        var result = resolver.Resolve(request);

        Assert.False(result.Launched);
        Assert.Equal(EngagementAbortReason.AirNotReady, result.AbortReason);
        Assert.Equal(AbortReasonCatalog.Engage.AIR_NOT_READY, EngagementAbortReasonCodes.ToLogCode(result.AbortReason));
    }

    /// <summary>Wave 2 adversarial: AIR_NOT_READY must not burn magazines (doc 16 LOG-04).</summary>
    [Fact]
    public void Resolve_AIR_NOT_READY_does_not_mutate_magazine_ledger()
    {
        var world = new DictionaryEngageWorldQuery();
        var magazines = new MagazineLedger();
        magazines.SetRounds(shooterUnitId: 1, mountId: 0, rounds: 4);
        var request = new EngageRequest(1, 2, MountId: 0, SimTick: 1);
        world.Set(
            request,
            new EngageContext(
                50_000,
                new WeaponEnvelope(1_000, 100_000),
                RoundsRemaining: 4,
                HasFireControlTrack: true,
                AirOperationsReady: false));

        var resolver = new MvpEngagementResolver(world, magazines);
        var result = resolver.Resolve(request);

        Assert.False(result.Launched);
        Assert.Equal(EngagementAbortReason.AirNotReady, result.AbortReason);
        Assert.Equal(4, magazines.GetRounds(1, 0));
    }
}