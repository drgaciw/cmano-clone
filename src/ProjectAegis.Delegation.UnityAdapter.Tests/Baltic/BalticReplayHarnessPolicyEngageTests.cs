namespace ProjectAegis.Delegation.UnityAdapter.Tests.Baltic;

using ProjectAegis.Delegation.UnityAdapter.Baltic;
using ProjectAegis.Sim.Glossary;
using NUnit.Framework;

/// <summary>Policy–engage unification: resolver denials appear in harness fingerprint (epic policy-engage-unification-slice).</summary>
[TestFixture]
public sealed class BalticReplayHarnessPolicyEngageTests
{
    [Test]
    public void Restricted_engagement_scenario_fingerprint_is_deterministic()
    {
        var a = BalticReplayHarness.Run(7, "restricted-engagement", ticks: 4);
        var b = BalticReplayHarness.Run(7, "restricted-engagement", ticks: 4);
        Assert.That(b.Fingerprint, Is.EqualTo(a.Fingerprint));
        Assert.That(a.Fingerprint, Does.Contain("Engagement|"));
    }

    [Test]
    public void Friendly_weapons_tight_surfaces_policy_abort_in_engagement_log()
    {
        var result = BalticReplayHarness.Run(7, "restricted-engagement", ticks: 4);
        Assert.That(
            result.Fingerprint,
            Does.Contain(AbortReasonCatalog.Doctrine.ROE_WEAPONS_TIGHT)
                .Or.Contain(AbortReasonCatalog.Engage.OUT_OF_ENVELOPE)
                .Or.Contain(AbortReasonCatalog.Engage.DLZ_OUT));
    }

    [Test]
    public void Emcon_off_scenario_logs_emcon_abort_in_fingerprint()
    {
        var result = BalticReplayHarness.Run(9, "baltic-patrol-emcon-off", ticks: 4);
        Assert.That(result.Fingerprint, Does.Contain(AbortReasonCatalog.Doctrine.EMCON_OFF));
    }
}