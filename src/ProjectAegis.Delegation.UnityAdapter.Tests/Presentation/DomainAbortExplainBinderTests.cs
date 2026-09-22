using NUnit.Framework;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Presentation;
using ProjectAegis.Sim.Glossary;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

[TestFixture]
public sealed class DomainAbortExplainBinderTests
{
    private static readonly string[] DomainCodes =
    [
        AbortReasonCatalog.Engage.AIR_ASPECT_BLOCK,
        AbortReasonCatalog.Engage.SURFACE_ASPECT_BLOCK,
        AbortReasonCatalog.Engage.SUBSURFACE_ASPECT_BLOCK,
        AbortReasonCatalog.Engage.LAND_ASPECT_BLOCK,
        AbortReasonCatalog.Engage.MINE_ASPECT_BLOCK,
        AbortReasonCatalog.Engage.FACILITY_ASPECT_BLOCK,
        AbortReasonCatalog.Engage.DOMAIN_NO_SOLUTION,
    ];

    [TestCaseSource(nameof(DomainCodes))]
    public void Explain_reason_code_binds_blocked_domain_abort(string code)
    {
        var explain = new EngageExplain(
            $"ENGAGE: BLOCKED — {code}",
            code,
            $"Engagement blocked ({code}).",
            IsBlocked: true);

        var bound = DomainAbortExplainBinder.Bind(explain);

        Assert.That(bound.Code, Is.EqualTo(code));
        Assert.That(bound.IsBlocked, Is.True);
        Assert.That(bound.StateLine, Does.Contain(code));
        Assert.That(bound.StateLine, Does.Contain(DomainAbortDeclutterTokens.Blocked));
        Assert.That(bound.CueClass, Is.EqualTo(DomainAbortCueClasses.Blocked));
        Assert.That(bound.DeclutterToken, Is.EqualTo(DomainAbortDeclutterTokens.Blocked));
        Assert.That(bound.Fingerprint, Is.EqualTo($"dom:blocked:{code}"));
    }

    [Test]
    public void Engage_preview_abort_code_binds_when_explain_has_no_reason()
    {
        var preview = new EngagePreview(
            "DLZ: InZone (Normal)",
            CanFire: false,
            AbortReasonCatalog.Engage.SURFACE_ASPECT_BLOCK);

        var bound = DomainAbortExplainBinder.Bind(EngageExplain.Empty, preview);

        Assert.That(bound.Code, Is.EqualTo(AbortReasonCatalog.Engage.SURFACE_ASPECT_BLOCK));
        Assert.That(bound.CueClass, Is.EqualTo(DomainAbortCueClasses.Blocked));
        Assert.That(bound.IsBlocked, Is.True);
    }

    [Test]
    public void Combat_detail_outcome_binds_domain_code_when_preview_is_not_a_domain_abort()
    {
        var detail = DetailWithStatus("u1 → hostile-1: AuthorizationRefused / AIR_ASPECT_BLOCK");
        var preview = new EngagePreview("DLZ: InZone (Normal)", CanFire: false, AbortReasonCatalog.Engage.DLZ_OUT);

        var bound = DomainAbortExplainBinder.Bind(null, preview, detail);

        Assert.That(bound.Code, Is.EqualTo(AbortReasonCatalog.Engage.AIR_ASPECT_BLOCK));
        Assert.That(bound.StateLine, Does.StartWith("DOMAIN: BLOCKED"));
        Assert.That(bound.CueClass, Is.EqualTo(DomainAbortCueClasses.Blocked));
    }

    [Test]
    public void Explicit_reason_code_wins_over_a_different_code_in_prose()
    {
        var explain = new EngageExplain(
            "ENGAGE: BLOCKED — SURFACE_ASPECT_BLOCK",
            AbortReasonCatalog.Engage.MINE_ASPECT_BLOCK,
            "Also saw AIR_ASPECT_BLOCK in narrative.",
            IsBlocked: true);

        var bound = DomainAbortExplainBinder.Bind(explain);

        Assert.That(bound.Code, Is.EqualTo(AbortReasonCatalog.Engage.MINE_ASPECT_BLOCK));
    }

    [Test]
    public void Non_domain_abort_is_domain_clear()
    {
        var explain = new EngageExplain(
            "ENGAGE: BLOCKED — DLZ_OUT",
            AbortReasonCatalog.Engage.DLZ_OUT,
            "Target is outside the dynamic launch zone for this weapon envelope.",
            IsBlocked: true);

        var bound = DomainAbortExplainBinder.Bind(explain);

        Assert.That(bound.Code, Is.Null);
        Assert.That(bound.IsBlocked, Is.False);
        Assert.That(bound.CueClass, Is.EqualTo(DomainAbortCueClasses.Clear));
        Assert.That(bound.DeclutterToken, Is.EqualTo(DomainAbortDeclutterTokens.Clear));
        Assert.That(bound.StateLine, Does.Contain("DOMAIN: CLEAR"));
        Assert.That(bound.StateLine, Does.Contain(DomainAbortDeclutterTokens.Clear));
        Assert.That(bound.Fingerprint, Is.EqualTo("dom:clear"));
    }

    [Test]
    public void Permitted_launch_is_domain_clear()
    {
        var explain = EngageExplainProjection.Project(
            new EngagePreview("DLZ: InZone (Normal)", CanFire: true, AbortPreviewCode: null));

        var bound = DomainAbortExplainBinder.Bind(explain);

        Assert.That(bound.CueClass, Is.EqualTo(DomainAbortCueClasses.Clear));
        Assert.That(bound.Code, Is.Null);
        Assert.That(bound.IsBlocked, Is.False);
    }

    [Test]
    public void Missing_projection_fails_closed_to_unknown()
    {
        var bound = DomainAbortExplainBinder.Bind(null, null, null);

        Assert.That(bound, Is.EqualTo(DomainAbortExplainState.Empty));
        Assert.That(bound.CueClass, Is.EqualTo(DomainAbortCueClasses.Unknown));
        Assert.That(bound.DeclutterToken, Is.EqualTo(DomainAbortDeclutterTokens.Unknown));
        Assert.That(bound.StateLine, Does.Contain("DOMAIN: —"));
        Assert.That(bound.Fingerprint, Is.EqualTo("dom:unknown"));
        Assert.That(bound.Code, Is.Null);
    }

    [Test]
    public void Empty_explain_and_empty_detail_stay_unknown()
    {
        var bound = DomainAbortExplainBinder.Bind(EngageExplain.Empty, null, CombatDetailPresentation.Empty);

        Assert.That(bound.CueClass, Is.EqualTo(DomainAbortCueClasses.Unknown));
        Assert.That(bound.Fingerprint, Is.EqualTo("dom:unknown"));
    }

    [Test]
    public void Code_token_does_not_match_a_longer_identifier()
    {
        var explain = new EngageExplain(
            "ENGAGE: BLOCKED",
            "AIR_ASPECT_BLOCKED_EXTRA",
            "No domain abort.",
            IsBlocked: true);

        var bound = DomainAbortExplainBinder.Bind(explain);

        Assert.That(bound.Code, Is.Null);
        Assert.That(bound.CueClass, Is.EqualTo(DomainAbortCueClasses.Clear));
    }

    [Test]
    public void Earliest_code_in_one_line_wins()
    {
        var detail = DetailWithStatus("refused LAND_ASPECT_BLOCK then FACILITY_ASPECT_BLOCK");

        var bound = DomainAbortExplainBinder.Bind(null, null, detail);

        Assert.That(bound.Code, Is.EqualTo(AbortReasonCatalog.Engage.LAND_ASPECT_BLOCK));
    }

    [Test]
    public void Bind_rows_exposes_domain_abort_element()
    {
        var bound = DomainAbortExplainBinder.Bind(new EngageExplain(
            "ENGAGE: BLOCKED — FACILITY_ASPECT_BLOCK",
            AbortReasonCatalog.Engage.FACILITY_ASPECT_BLOCK,
            "Engagement blocked (FACILITY_ASPECT_BLOCK).",
            IsBlocked: true));
        var rows = DomainAbortExplainBinder.BindRows(bound);

        Assert.That(rows, Has.Count.EqualTo(1));
        Assert.That(rows[0].ElementName, Is.EqualTo("engage-explain-domain-abort"));
        Assert.That(rows[0].Text, Does.Contain(AbortReasonCatalog.Engage.FACILITY_ASPECT_BLOCK));
        Assert.That(rows[0].CueClass, Is.EqualTo(DomainAbortCueClasses.Blocked));
    }

    [Test]
    public void Fingerprint_changes_when_domain_code_changes()
    {
        var air = DomainAbortExplainBinder.Bind(Blocked(AbortReasonCatalog.Engage.AIR_ASPECT_BLOCK));
        var mine = DomainAbortExplainBinder.Bind(Blocked(AbortReasonCatalog.Engage.MINE_ASPECT_BLOCK));
        var clear = DomainAbortExplainBinder.Bind(Blocked(AbortReasonCatalog.Engage.ROE_HOLD_FIRE));

        Assert.That(air.Fingerprint, Is.Not.EqualTo(mine.Fingerprint));
        Assert.That(air.Fingerprint, Is.Not.EqualTo(clear.Fingerprint));
    }

    [Test]
    public void Cue_class_catalog_covers_blocked_and_clear()
    {
        Assert.That(DomainAbortCueClasses.All, Does.Contain(DomainAbortCueClasses.Unknown));
        Assert.That(DomainAbortCueClasses.All, Does.Contain(DomainAbortCueClasses.Clear));
        Assert.That(DomainAbortCueClasses.All, Does.Contain(DomainAbortCueClasses.Blocked));
    }

    private static EngageExplain Blocked(string code) =>
        new($"ENGAGE: BLOCKED — {code}", code, $"Engagement blocked ({code}).", IsBlocked: true);

    private static CombatDetailPresentation DetailWithStatus(string statusLine) =>
        new(
            statusLine,
            "Weapon family: SAM",
            "Hard constraints: UNKNOWN — policy refusal reported",
            "Policy: UNKNOWN",
            "Contact confidence: UNKNOWN",
            "Firing solution: UNKNOWN",
            "No corrective action reported.",
            "BDA: UNKNOWN",
            "Posture: UNKNOWN",
            "Log correlation: 1");
}
