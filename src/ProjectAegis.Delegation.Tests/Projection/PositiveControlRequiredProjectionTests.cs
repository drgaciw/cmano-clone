using ProjectAegis.Delegation.Projection;
using ProjectAegis.Sim.Policy;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

/// <summary>Req 20 P0 Phase 0: PositiveControlRequired replaces WeaponsTight host proxies.</summary>
[TestFixture]
public sealed class PositiveControlRequiredProjectionTests
{
    [Test]
    public void WeaponsTight_requires_positive_control()
    {
        Assert.That(PositiveControlRequiredProjection.IsRequired(RoeLevel.WeaponsTight), Is.True);
        Assert.That(PositiveControlRequiredProjection.IsRequired(new EffectivePolicy(RoeLevel.WeaponsTight)), Is.True);
    }

    [Test]
    public void WeaponsFree_and_HoldFire_do_not_require_gate()
    {
        Assert.That(PositiveControlRequiredProjection.IsRequired(RoeLevel.WeaponsFree), Is.False);
        Assert.That(PositiveControlRequiredProjection.IsRequired(RoeLevel.HoldFire), Is.False);
    }

    [Test]
    public void Gate_honors_projection_flag()
    {
        var required = PositiveControlRequiredProjection.IsRequired(RoeLevel.WeaponsTight);
        Assert.That(
            WeaponsReleaseConfirmationGate.ShouldEmit(required, WeaponsReleaseConfirmationGate.GateAction.Confirm),
            Is.True);
        Assert.That(
            WeaponsReleaseConfirmationGate.ShouldEmit(required, WeaponsReleaseConfirmationGate.GateAction.Cancel),
            Is.False);
    }

    [Test]
    public void Host_proxy_must_not_use_raw_WeaponsTight_equality_for_gate()
    {
        // T3 residual: hosts call IsRequired rather than Roe == WeaponsTight.
        // Keep behavioral parity with the historical WeaponsTight heuristic.
        foreach (RoeLevel roe in Enum.GetValues(typeof(RoeLevel)))
        {
            var viaProjection = PositiveControlRequiredProjection.IsRequired(roe);
            var legacyHeuristic = roe == RoeLevel.WeaponsTight;
            Assert.That(viaProjection, Is.EqualTo(legacyHeuristic), $"ROE {roe}");
        }
    }
}
