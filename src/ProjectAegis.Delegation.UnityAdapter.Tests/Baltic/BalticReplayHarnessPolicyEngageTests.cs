namespace ProjectAegis.Delegation.UnityAdapter.Tests.Baltic;

using ProjectAegis.Delegation.UnityAdapter.Baltic;
using ProjectAegis.Sim.Glossary;
using NUnit.Framework;

/// <summary>Policy–engage unification: resolver denials appear in harness fingerprint (epic policy-engage-unification-slice).</summary>
/// <remarks>
/// S86-02 triage (per sprint-86-cli-mcp-polish.md + kickoff-2026-07-04 + roadmap-execute-plan-07042026.md §4):
/// The two tests below were failing on main (inherited; confirmed at 17d426c).
/// Root cause (see docs/reports/baltic-headless-slice-gate-2026-07-04.md): restricted-engagement resolves ROE=WeaponsTight;
/// denials occur at Policy layer (PolicyDenial rows with "WeaponsTight") *before* reaching engagement resolver.
/// No "Engagement|" rows or canonical AbortReasonCatalog abort codes are emitted for this scenario.
/// Determinism holds (fingerprint equal run-to-run). This is contract/representation difference vs prior expectations, not determinism regression.
/// Decision (user-approved S86): align test expectations to observed PolicyDenial surface (fix by updating asserts);
/// preserve original intent comment; do not touch BalticReplayHarness / DelegationBridge (ZERO per invariants + CRITICAL 52 impact).
/// Cites: production/scenario-editor-scope-boundary-2026-07-04.md, production/qa/qa-plan-scenario-editor-2026-07-01.md (UA pair),
/// Game-Requirements/requirements/11-Agentic-Mission-Editor.md, AGENTS.md (GitNexus pre + detect), baltic-v* boundaries.
/// UA filter now green; full gates + 6/6 + 18/18 + hash preserved; no new failures introduced.
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
        // S86 triage: denials surface via PolicyDenial (not Engagement|) for WeaponsTight restricted case on current spine.
        // See baltic-headless-slice-gate-2026-07-04.md for contract analysis. Determinism contract holds.
        Assert.That(a.Fingerprint, Does.Contain("PolicyDenial"));
    }

    [Test]
    public void Friendly_weapons_tight_surfaces_policy_abort_in_engagement_log()
    {
        var result = BalticReplayHarness.Run(7, "restricted-engagement", ticks: 4);
        // S86 triage fix: expect observed representation (PolicyDenial + WeaponsTight string from ROE).
        // Catalog codes (ROE_WEAPONS_TIGHT etc) are not emitted here; policy denial precedes resolver abort logging.
        // Or conditions retained for forward compatibility if unification changes surface later.
        Assert.That(
            result.Fingerprint,
            Does.Contain("PolicyDenial").And.Contain("WeaponsTight")
                .Or.Contain(AbortReasonCatalog.Doctrine.ROE_WEAPONS_TIGHT)
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