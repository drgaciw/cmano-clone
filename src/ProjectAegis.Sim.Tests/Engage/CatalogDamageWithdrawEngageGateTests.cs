using ProjectAegis.Sim.Engage;
using ProjectAegis.Sim.Scenario;
using Xunit;

namespace ProjectAegis.Sim.Tests.Engage;

public sealed class CatalogDamageWithdrawEngageGateTests
{
    [Fact]
    public void Blocks_engage_when_catalog_resolved_trial_recommends_withdraw()
    {
        var trials = new[]
        {
            new ScenarioWithdrawReadinessTrial("u1", 0.15, WithdrawRecommended: true, CatalogResolved: true),
        };

        Assert.True(CatalogDamageWithdrawEngageGate.BlocksEngage("u1", trials));
    }

    [Fact]
    public void Does_not_block_when_trial_unresolved_or_withdraw_not_recommended()
    {
        var unresolved = new[]
        {
            new ScenarioWithdrawReadinessTrial("u1", 1.0, WithdrawRecommended: false, CatalogResolved: false),
        };
        var belowThreshold = new[]
        {
            new ScenarioWithdrawReadinessTrial("u1", 0.8, WithdrawRecommended: false, CatalogResolved: true),
        };

        Assert.False(CatalogDamageWithdrawEngageGate.BlocksEngage("u1", unresolved));
        Assert.False(CatalogDamageWithdrawEngageGate.BlocksEngage("u1", belowThreshold));
        Assert.False(CatalogDamageWithdrawEngageGate.BlocksEngage("u2", belowThreshold));
    }

    [Fact]
    public void Evaluate_returns_abort_reason_only_when_context_flag_set()
    {
        var blocked = new EngageContext(
            50_000,
            new WeaponEnvelope(1_000, 100_000),
            RoundsRemaining: 2,
            HasFireControlTrack: true,
            CatalogDamageWithdrawBlocked: true);
        var clear = blocked with { CatalogDamageWithdrawBlocked = false };

        Assert.Equal(EngagementAbortReason.DamageWithdrawRecommended, CatalogDamageWithdrawEngageGate.Evaluate(in blocked));
        Assert.Null(CatalogDamageWithdrawEngageGate.Evaluate(in clear));
    }
}