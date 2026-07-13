using ProjectAegis.Sim.Core;
using ProjectAegis.Sim.Engage;
using ProjectAegis.Sim.Glossary;
using Xunit;

namespace ProjectAegis.Sim.Tests.Engage;

public sealed class MvpEngagementDamageWithdrawTests
{
    [Fact]
    public void Resolve_aborts_with_DAMAGE_WITHDRAW_RECOMMENDED_when_catalog_trial_blocks()
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
                CatalogDamageWithdrawBlocked: true));

        var resolver = new MvpEngagementResolver(world, magazines);
        var result = resolver.Resolve(request);

        Assert.False(result.Launched);
        Assert.Equal(EngagementAbortReason.DamageWithdrawRecommended, result.AbortReason);
        Assert.Equal(
            AbortReasonCatalog.Engage.DAMAGE_WITHDRAW_RECOMMENDED,
            EngagementAbortReasonCodes.ToLogCode(result.AbortReason));
    }

    [Fact]
    public void Resolve_does_not_abort_for_damage_withdraw_when_context_flag_clear()
    {
        var world = new DictionaryEngageWorldQuery();
        var magazines = new MagazineLedger();
        magazines.SetRounds(1, 0, 2);
        var request = new EngageRequest(1, 2, 0, 0);
        world.Set(
            request,
            new EngageContext(
                50_000,
                new WeaponEnvelope(1_000, 100_000),
                RoundsRemaining: 2,
                HasFireControlTrack: true,
                CatalogDamageWithdrawBlocked: false));

        var resolver = new MvpEngagementResolver(world, magazines);
        var result = resolver.Resolve(request);

        Assert.NotEqual(EngagementAbortReason.DamageWithdrawRecommended, result.AbortReason);
    }
}