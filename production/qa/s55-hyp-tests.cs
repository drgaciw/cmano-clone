using ProjectAegis.Sim.Engage;
using Xunit;

namespace ProjectAegis.Sim.Tests.Engage;

public sealed class HypersonicEngageGateTests
{
    [Fact]
    public void Evaluate_blocks_without_defense_layer()
    {
        var ctx = new EngageContext(
            50_000,
            new WeaponEnvelope(1_000, 100_000),
            2,
            true,
            IsHypersonicTarget: true,
            HasHypersonicDefenseLayer: false);

        Assert.Equal(EngagementAbortReason.DomainNoSolution, HypersonicEngageGate.Evaluate(in ctx));
    }

    [Fact]
    public void Evaluate_allows_with_defense_layer()
    {
        var ctx = new EngageContext(
            50_000,
            new WeaponEnvelope(1_000, 100_000),
            2,
            true,
            IsHypersonicTarget: true,
            HasHypersonicDefenseLayer: true);

        Assert.Null(HypersonicEngageGate.Evaluate(in ctx));
    }
}