using NUnit.Framework;
using ProjectAegis.Data.Evidence;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Baltic;
using ProjectAegis.Delegation.UnityAdapter.Presentation;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

[TestFixture]
public sealed class CombatReleaseGateEvaluatorTests
{
    [Test]
    public void CommsLegendTooltipBinder_emits_non_empty_acc11_hud_tooltip_copy_for_nominal_degraded_and_denied()
    {
        var emptyTooltip = C2CommsLegendTooltipBinder.BuildTooltip(
            C2NetworkHealthPanelBinder.Bind(C2NetworkHealthPresentation.Empty));

        Assert.That(emptyTooltip, Does.Contain("NOMINAL"));
        Assert.That(emptyTooltip, Does.Contain("DEGRADED"));
        Assert.That(emptyTooltip, Does.Contain("DENIED"));
        Assert.That(emptyTooltip, Does.Contain("Network:"));
    }

    [Test]
    public void TopBarHost_wires_acc11_comms_legend_tooltip_without_violating_projection_boundary()
    {
        var text = UiIaSourceReader.ReadRuntime("C2TopBarPanelHost.cs");
        Assert.That(text, Does.Contain("C2CommsLegendTooltipBinder.BuildTooltip"));
        Assert.That(text, Does.Contain("_commsLegend.tooltip"));
        Assert.That(text, Does.Not.Contain("CommsStateProjection.Project"));
        Assert.That(text, Does.Not.Contain("DelegationBridge.Tick"));
    }

    [Test]
    public void VerifySeedInvariance_proves_ver04_identical_fingerprints_and_world_hash_across_runs()
    {
        var proof = CombatReleaseGateEvaluator.VerifyVer04SeedInvariance(
            scenarioPolicyId: "baltic-patrol-classify",
            seed: 7,
            ticks: 10,
            runs: 3);

        Assert.That(proof.Passed, Is.True, string.Join("; ", proof.Failures));
        Assert.That(proof.RunsExecuted, Is.EqualTo(3));
        Assert.That(proof.LogFingerprint, Is.Not.Empty);
        Assert.That(proof.FinalWorldHash, Is.Not.EqualTo(0UL));
    }

    [Test]
    public void EvaluateAcc11Checklist_validates_c2_and_platform_editor_rules()
    {
        var passing = CombatReleaseGateEvaluator.EvaluateAcc11(new Acc11AccessibilityInput(
            TierDeclaredStandard: true,
            VisibleMessageLogRowsAt100Pct: 12,
            CommsDegradeTriadVisible: true,
            CommsLegendTooltipText: C2CommsLegendTooltipBinder.DefaultLegendTooltip,
            PlatformEditorButtonLabels: ["Export", "Diff", "Propose", "Approve"],
            ApproveDisabledReadableAt45Pct: true,
            KeyboardOobAndDrawerNavigable: true,
            AffiliationShapeNotColorOnly: true));

        Assert.That(passing.Passed, Is.True, string.Join("; ", passing.Failures));

        var failing = CombatReleaseGateEvaluator.EvaluateAcc11(new Acc11AccessibilityInput(
            TierDeclaredStandard: true,
            VisibleMessageLogRowsAt100Pct: 9,
            CommsDegradeTriadVisible: true,
            CommsLegendTooltipText: string.Empty,
            PlatformEditorButtonLabels: ["Export", "Propose"],
            ApproveDisabledReadableAt45Pct: false,
            KeyboardOobAndDrawerNavigable: false,
            AffiliationShapeNotColorOnly: true));

        Assert.That(failing.Passed, Is.False);
        Assert.That(failing.Failures, Has.Some.Contains("12 visible rows"));
        Assert.That(failing.Failures, Has.Some.Contains("COMMS legend tooltip"));
        Assert.That(failing.Failures, Has.Some.Contains("Approve"));
        Assert.That(failing.Failures, Has.Some.Contains("45%"));
    }

    [Test]
    public void EvaluateVer06CiGate_enforces_test_floor_golden_hash_and_zero_bridge_touch()
    {
        var pass = CombatReleaseGateEvaluator.EvaluateVer06CiGate(new Ver06CiGateInput(
            ReportedPassedTests: 5067,
            RequiredTestFloor: 4361,
            ReplayGoldenPassed: 17,
            ReplayGoldenRequired: 6,
            GoldenHashPreserved: true,
            DelegationBridgeHotpathDiffLines: 0,
            GauntletOracleCiFailClosedVerified: true));

        Assert.That(pass.Passed, Is.True, string.Join("; ", pass.Failures));

        var fail = CombatReleaseGateEvaluator.EvaluateVer06CiGate(new Ver06CiGateInput(
            ReportedPassedTests: 4000,
            RequiredTestFloor: 4361,
            ReplayGoldenPassed: 5,
            ReplayGoldenRequired: 6,
            GoldenHashPreserved: false,
            DelegationBridgeHotpathDiffLines: 2,
            GauntletOracleCiFailClosedVerified: false));

        Assert.That(fail.Passed, Is.False);
        Assert.That(fail.Failures.Count, Is.EqualTo(5));
    }
}
