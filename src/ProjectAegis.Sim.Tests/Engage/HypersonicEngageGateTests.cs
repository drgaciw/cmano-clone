using ProjectAegis.Sim.Engage;
using NUnit.Framework;

namespace ProjectAegis.Sim.Tests.Engage;

public sealed class HypersonicEngageGateTests
{
    [Test]
    public void Evaluate_blocks_without_defense_layer()
    {
        var ctx = new EngageContext(
            50_000,
            new WeaponEnvelope(1_000, 100_000),
            2,
            true,
            IsHypersonicTarget: true,
            HasHypersonicDefenseLayer: false);

        Assert.That(HypersonicEngageGate.Evaluate(in ctx), Is.EqualTo(EngagementAbortReason.DomainNoSolution));
    }

    [Test]
    public void Evaluate_allows_with_defense_layer()
    {
        var ctx = new EngageContext(
            50_000,
            new WeaponEnvelope(1_000, 100_000),
            2,
            true,
            IsHypersonicTarget: true,
            HasHypersonicDefenseLayer: true);

        Assert.That(HypersonicEngageGate.Evaluate(in ctx), Is.Null);
    }
}