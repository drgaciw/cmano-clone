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
}