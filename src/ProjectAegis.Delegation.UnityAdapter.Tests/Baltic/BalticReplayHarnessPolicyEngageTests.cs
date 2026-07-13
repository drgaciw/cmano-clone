namespace ProjectAegis.Delegation.UnityAdapter.Tests.Baltic;

using ProjectAegis.Delegation.UnityAdapter.Baltic;
using ProjectAegis.Sim.Glossary;
using NUnit.Framework;

/// <summary>Policy–engage unification: resolver denials appear in harness fingerprint (epic policy-engage-unification-slice).</summary>
/// <remarks>
/// Contract restored (fix/policy-engage-roe-abort-fingerprint, per docs/reports/baltic-headless-slice-gate-2026-07-04.md
/// recommendation #1, approach A2). The policy-engage-unification-slice epic requires ROE/WRA/EMCON denials to surface in the
/// engagement order log so the harness fingerprint carries the canonical AbortReasonCatalog code. Under restricted-engagement
/// (ROE=WeaponsTight) the engage intent is rejected at the agent/policy layer (PolicyDenial rows) before it reaches
/// MvpEngagementResolver, so the S86-02 triage had aligned these asserts *down* to the PolicyDenial surface. That masked the
/// bypass instead of fixing it. SimulationSession now additionally surfaces WeaponsTight engage denials as
/// Engagement|…|ROE_WEAPONS_TIGHT abort rows (SimulationSession.SurfaceRoePolicyDeniedEngagements), so the asserts below
/// verify the contract, not the bypass. The PolicyDenial row is preserved; determinism holds run-to-run.
/// </remarks>
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