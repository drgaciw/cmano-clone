using ProjectAegis.Data.Scenario.Authoring;
using ProjectAegis.Data.Validation;
using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

/// <summary>CMD-35 live-edit apply-state: empty, blocking, warnings, mutation failure.</summary>
[TestFixture]
public sealed class LiveEditApplyStateTests
{
    [Test]
    public void FromValidationReport_null_report_is_empty_findings_with_version_status()
    {
        var presentation = LiveEditApplyState.FromValidationReport(
            report: null,
            editVersion: 3,
            lastMutationOk: true,
            lastReason: null);

        Assert.That(presentation.EditVersion, Is.EqualTo(3));
        Assert.That(presentation.BlockingCount, Is.EqualTo(0));
        Assert.That(presentation.WarningCount, Is.EqualTo(0));
        Assert.That(presentation.Findings, Is.Empty);
        Assert.That(presentation.LastMutationOk, Is.True);
        Assert.That(presentation.LastMutationReason, Is.Null);
        Assert.That(presentation.StatusLine, Is.EqualTo("LIVE EDIT v3 · 0 blocking · 0 warnings"));
        Assert.That(presentation.CommitEnabled, Is.True);
        Assert.That(presentation.CommitDisabledReason, Is.Null);
    }

    [Test]
    public void FromValidationReport_warnings_only_keeps_commit_enabled()
    {
        var report = ValidationReport.FromFindings(new[]
        {
            new ValidationFinding("WARN_A", ValidationSeverity.Warning, "watch this", UnitId: "u1"),
            new ValidationFinding("WARN_B", ValidationSeverity.Warning, "also watch"),
            new ValidationFinding("INFO_C", ValidationSeverity.Info, "fyi"),
        });

        var presentation = LiveEditApplyState.FromValidationReport(report, editVersion: 5, lastMutationOk: true, lastReason: null);

        Assert.That(presentation.BlockingCount, Is.EqualTo(0));
        Assert.That(presentation.WarningCount, Is.EqualTo(2));
        Assert.That(presentation.Findings, Has.Count.EqualTo(3));
        Assert.That(presentation.StatusLine, Is.EqualTo("LIVE EDIT v5 · 0 blocking · 2 warnings"));
        Assert.That(presentation.CommitEnabled, Is.True);

        var warnA = presentation.Findings.First(f => f.Code == "WARN_A");
        Assert.That(warnA.Severity, Is.EqualTo("Warning"));
        Assert.That(warnA.Path, Is.EqualTo("unit/u1"));
        Assert.That(presentation.Findings.Any(f => f.Severity == "Info"), Is.True);
    }

    [Test]
    public void FromValidationReport_blocking_errors_disable_commit_and_block_status()
    {
        var report = ValidationReport.FromFindings(new[]
        {
            new ValidationFinding("MISSION_NO_UNITS", ValidationSeverity.Error, "mission has no units", MissionId: "m1"),
            new ValidationFinding("SOFT_WARN", ValidationSeverity.Warning, "soft"),
        });

        var presentation = LiveEditApplyState.FromValidationReport(report, editVersion: 7, lastMutationOk: true, lastReason: null);

        Assert.That(presentation.BlockingCount, Is.EqualTo(1));
        Assert.That(presentation.WarningCount, Is.EqualTo(1));
        Assert.That(presentation.StatusLine, Is.EqualTo("LIVE EDIT BLOCKED: MISSION_NO_UNITS"));
        Assert.That(presentation.CommitEnabled, Is.False);
        Assert.That(presentation.CommitDisabledReason, Does.Contain("MISSION_NO_UNITS"));

        var err = presentation.Findings.First(f => f.Code == "MISSION_NO_UNITS");
        Assert.That(err.Severity, Is.EqualTo("Error"));
        Assert.That(err.Path, Is.EqualTo("mission/m1"));
        Assert.That(err.Message, Is.EqualTo("mission has no units"));
    }

    [Test]
    public void FromValidationReport_mutation_failure_reason_surfaces_blocked_status()
    {
        var presentation = LiveEditApplyState.FromValidationReport(
            report: null,
            editVersion: 4,
            lastMutationOk: false,
            lastReason: "CONFLICT");

        Assert.That(presentation.LastMutationOk, Is.False);
        Assert.That(presentation.LastMutationReason, Is.EqualTo("CONFLICT"));
        Assert.That(presentation.StatusLine, Is.EqualTo("LIVE EDIT BLOCKED: CONFLICT"));
        Assert.That(presentation.CommitEnabled, Is.False);
        Assert.That(presentation.CommitDisabledReason, Is.EqualTo("CONFLICT"));
        Assert.That(presentation.Findings, Is.Empty);
        Assert.That(presentation.BlockingCount, Is.EqualTo(0));
    }

    [Test]
    public void FromMutationResult_ok_with_report_maps_findings()
    {
        var report = ValidationReport.FromFindings(new[]
        {
            new ValidationFinding("WARN_ONLY", ValidationSeverity.Warning, "ok-ish"),
        });
        var result = new ScenarioMutationResult
        {
            Ok = true,
            EditVersion = 9,
            Report = report,
        };

        var presentation = LiveEditApplyState.FromMutationResult(result);

        Assert.That(presentation.EditVersion, Is.EqualTo(9));
        Assert.That(presentation.LastMutationOk, Is.True);
        Assert.That(presentation.WarningCount, Is.EqualTo(1));
        Assert.That(presentation.StatusLine, Is.EqualTo("LIVE EDIT v9 · 0 blocking · 1 warnings"));
    }

    [Test]
    public void FromMutationResult_failure_uses_error_code()
    {
        var result = new ScenarioMutationResult
        {
            Ok = false,
            ErrorCode = "INVALID_OPERATION",
            ErrorMessage = "Mission id 'x' was not found.",
            EditVersion = 2,
        };

        var presentation = LiveEditApplyState.FromMutationResult(result);

        Assert.That(presentation.LastMutationOk, Is.False);
        Assert.That(presentation.LastMutationReason, Is.EqualTo("INVALID_OPERATION"));
        Assert.That(presentation.StatusLine, Is.EqualTo("LIVE EDIT BLOCKED: INVALID_OPERATION"));
        Assert.That(presentation.CommitEnabled, Is.False);
    }

    [Test]
    public void PanelBinder_exposes_finding_codes_and_commit_gate()
    {
        var report = ValidationReport.FromFindings(new[]
        {
            new ValidationFinding("ERR_A", ValidationSeverity.Error, "bad", TargetId: "t1"),
            new ValidationFinding("WARN_B", ValidationSeverity.Warning, "meh"),
        });

        var panel = LiveEditPanelBinder.BindFromReport(report, editVersion: 1, lastMutationOk: true, lastReason: null);

        Assert.That(panel.FindingCodes, Does.Contain("ERR_A"));
        Assert.That(panel.FindingCodes, Does.Contain("WARN_B"));
        Assert.That(panel.FindingCodes.Count, Is.EqualTo(2));
        Assert.That(panel.CommitEnabled, Is.False);
        Assert.That(panel.CommitDisabledReason, Is.Not.Null.And.Not.Empty);
        Assert.That(panel.Rows.Count, Is.EqualTo(2));
        Assert.That(panel.Rows.First(r => r.Code == "ERR_A").Path, Is.EqualTo("target/t1"));
        Assert.That(panel.Rows.First(r => r.Code == "ERR_A").DisplayLine, Does.Contain("ERR_A"));
        Assert.That(panel.StatusLine, Is.EqualTo("LIVE EDIT BLOCKED: ERR_A"));
    }

    [Test]
    public void LiveEditContract_Project_matches_apply_state()
    {
        var report = ValidationReport.FromFindings(new[]
        {
            new ValidationFinding("X", ValidationSeverity.Error, "x"),
        });
        var viaApply = LiveEditApplyState.FromValidationReport(report, 11, true, null);
        var viaContract = LiveEditContract.Project(report, 11, true, null);

        Assert.That(viaContract.EditVersion, Is.EqualTo(viaApply.EditVersion));
        Assert.That(viaContract.BlockingCount, Is.EqualTo(viaApply.BlockingCount));
        Assert.That(viaContract.WarningCount, Is.EqualTo(viaApply.WarningCount));
        Assert.That(viaContract.StatusLine, Is.EqualTo(viaApply.StatusLine));
        Assert.That(viaContract.LastMutationOk, Is.EqualTo(viaApply.LastMutationOk));
        Assert.That(viaContract.Findings.Select(f => f.Code), Is.EqualTo(viaApply.Findings.Select(f => f.Code)));
        Assert.That(LiveEditContract.BindPanel(viaContract).BlockingCount, Is.EqualTo(1));
    }

    [Test]
    public void MapSeverity_is_honest()
    {
        Assert.That(LiveEditApplyState.MapSeverity(ValidationSeverity.Error), Is.EqualTo("Error"));
        Assert.That(LiveEditApplyState.MapSeverity(ValidationSeverity.Warning), Is.EqualTo("Warning"));
        Assert.That(LiveEditApplyState.MapSeverity(ValidationSeverity.Info), Is.EqualTo("Info"));
    }
}
