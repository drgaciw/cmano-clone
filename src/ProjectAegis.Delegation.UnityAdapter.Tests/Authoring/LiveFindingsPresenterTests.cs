using ProjectAegis.Data.Scenario.Authoring;
using ProjectAegis.Data.Validation;
using ProjectAegis.Delegation.UnityAdapter.Authoring;
using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Authoring;

/// <summary>
/// LiveFindingsPresenter (Phase 2.1 Task 5): code parity with LiveValidate, debounceMs:0 schedule.
/// DRG-55: ADR-008 severity-first ordering must survive the presenter.
/// DRG-56: export gate state (counts + CanExport + blocking reason) must be reachable from a UI.
/// </summary>
public sealed class LiveFindingsPresenterTests
{
    [Test]
    public void RefreshImmediate_codes_match_editor_LiveValidate_codes()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-findings-parity-{Guid.NewGuid():N}.json");
        try
        {
            ScenarioDocumentEditor.CreateNew().Save(path);
            using var session = ScenarioAuthoringSession.Open(path);
            session.Bus.AttachPatrolFromSelection(
                expectedEditVersion: session.EditVersion,
                missionId: "patrol-empty",
                unitIds: Array.Empty<string>(),
                zone:
                [
                    new ScenarioWaypointDto { Lat = 57, Lon = 20 },
                    new ScenarioWaypointDto { Lat = 57.1, Lon = 20.1 },
                    new ScenarioWaypointDto { Lat = 57.2, Lon = 20.2 },
                ],
                save: true);

            var presenter = new LiveFindingsPresenter(session, debounceMs: 0);
            presenter.RefreshImmediate();

            // Parity is about *which* codes surface, not their order — ordering is owned by
            // ADR-008 and asserted separately (DRG-55). Normalize both sides before comparing.
            var editorCodes = session.Editor.LiveValidate().Findings
                .Select(f => f.Code)
                .OrderBy(c => c, StringComparer.Ordinal)
                .ToArray();
            var presenterCodes = presenter.LastCodes
                .OrderBy(c => c, StringComparer.Ordinal)
                .ToArray();

            Assert.That(presenterCodes, Is.EqualTo(editorCodes));
            Assert.That(presenter.LastReport, Is.Not.Null);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Test]
    public void After_place_unit_and_attach_empty_patrol_contains_MISSION_NO_UNITS()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-findings-no-units-{Guid.NewGuid():N}.json");
        try
        {
            ScenarioDocumentEditor.CreateNew().Save(path);
            using var session = ScenarioAuthoringSession.Open(path);

            session.Bus.PlaceUnit(
                expectedEditVersion: session.EditVersion,
                new ScenarioOrbatUnitDto
                {
                    Id = "u1",
                    SideId = "blue",
                    PlatformId = "ffg",
                    Lat = 57,
                    Lon = 20,
                },
                save: true);

            session.Bus.AttachPatrolFromSelection(
                expectedEditVersion: session.EditVersion,
                missionId: "patrol-empty",
                unitIds: Array.Empty<string>(),
                zone:
                [
                    new ScenarioWaypointDto { Lat = 57, Lon = 20 },
                    new ScenarioWaypointDto { Lat = 57.1, Lon = 20.1 },
                    new ScenarioWaypointDto { Lat = 57.2, Lon = 20.2 },
                ],
                save: true);

            var presenter = new LiveFindingsPresenter(session, debounceMs: 0);
            presenter.RefreshImmediate();

            Assert.That(presenter.LastCodes, Does.Contain("MISSION_NO_UNITS"));
            Assert.That(presenter.HasErrorSeverity, Is.True);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Test]
    public void ScheduleRefresh_with_debounceMs_0_updates_LastCodes()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-findings-sched-{Guid.NewGuid():N}.json");
        try
        {
            ScenarioDocumentEditor.CreateNew().Save(path);
            using var session = ScenarioAuthoringSession.Open(path);
            session.Bus.AttachPatrolFromSelection(
                expectedEditVersion: session.EditVersion,
                missionId: "patrol-empty",
                unitIds: Array.Empty<string>(),
                zone:
                [
                    new ScenarioWaypointDto { Lat = 57, Lon = 20 },
                    new ScenarioWaypointDto { Lat = 57.1, Lon = 20.1 },
                    new ScenarioWaypointDto { Lat = 57.2, Lon = 20.2 },
                ],
                save: true);

            var presenter = new LiveFindingsPresenter(session, debounceMs: 0);
            Assert.That(presenter.LastCodes, Is.Empty);

            presenter.ScheduleRefresh();

            Assert.That(presenter.LastCodes, Is.Not.Empty);
            Assert.That(presenter.LastCodes, Does.Contain("MISSION_NO_UNITS"));
            Assert.That(presenter.LastReport, Is.Not.Null);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    // ---- DRG-55: ADR-008 ordering ------------------------------------------------------

    /// <summary>
    /// ADR-008 pins (Severity DESC, Code ASC, MissionId, UnitId, TargetId, Message).
    /// Ordinal code sort alone would put the two EVENT_* warnings ahead of the blocking
    /// MISSION_NO_UNITS error — burying the only finding that stops an export.
    /// </summary>
    [Test]
    public void EvaluateGate_orders_codes_severity_first_per_ADR008()
    {
        var report = ValidationReport.FromFindings(new[]
        {
            new ValidationFinding("EVENT_GRAPH_COMPLEXITY_HIGH", ValidationSeverity.Warning, "soft"),
            new ValidationFinding("MISSION_NO_UNITS", ValidationSeverity.Error, "blocking"),
            new ValidationFinding("EVENT_GRAPH_PEAK_TICK_DENSITY_HIGH", ValidationSeverity.Warning, "soft"),
        });

        var gate = LiveFindingsPresenter.EvaluateGate(report);

        Assert.That(gate.Codes, Is.EqualTo(new[]
        {
            "MISSION_NO_UNITS",
            "EVENT_GRAPH_COMPLEXITY_HIGH",
            "EVENT_GRAPH_PEAK_TICK_DENSITY_HIGH",
        }));
    }

    /// <summary>
    /// ADR-008 ordering is part of the deterministic contract behind <c>ReportHash</c>:
    /// two refreshes of an unchanged document must produce an identical code sequence.
    /// </summary>
    [Test]
    public void RefreshImmediate_is_byte_identical_across_two_runs()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-findings-determinism-{Guid.NewGuid():N}.json");
        try
        {
            ScenarioDocumentEditor.CreateNew().Save(path);
            using var session = ScenarioAuthoringSession.Open(path);
            session.Bus.AttachPatrolFromSelection(
                expectedEditVersion: session.EditVersion,
                missionId: "patrol-empty",
                unitIds: Array.Empty<string>(),
                zone:
                [
                    new ScenarioWaypointDto { Lat = 57, Lon = 20 },
                    new ScenarioWaypointDto { Lat = 57.1, Lon = 20.1 },
                    new ScenarioWaypointDto { Lat = 57.2, Lon = 20.2 },
                ],
                save: true);

            var presenter = new LiveFindingsPresenter(session, debounceMs: 0);

            presenter.RefreshImmediate();
            var firstCodes = presenter.LastCodes.ToArray();
            var firstHash = presenter.LastReport!.ReportHash;

            presenter.RefreshImmediate();

            Assert.That(presenter.LastCodes, Is.EqualTo(firstCodes));
            Assert.That(presenter.LastReport!.ReportHash, Is.EqualTo(firstHash));
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Test]
    public void EvaluateGate_ties_within_severity_break_by_code_ordinal()
    {
        var report = ValidationReport.FromFindings(new[]
        {
            new ValidationFinding("STRIKE_NO_TARGETS", ValidationSeverity.Error, "b"),
            new ValidationFinding("FERRY_NO_DESTINATION", ValidationSeverity.Error, "a"),
        });

        var gate = LiveFindingsPresenter.EvaluateGate(report);

        Assert.That(gate.Codes, Is.EqualTo(new[] { "FERRY_NO_DESTINATION", "STRIKE_NO_TARGETS" }));
    }

    // ---- DRG-56: export gate state ------------------------------------------------------

    [Test]
    public void EvaluateGate_blocks_export_and_counts_by_severity()
    {
        var report = ValidationReport.FromFindings(new[]
        {
            new ValidationFinding("EVENT_GRAPH_COMPLEXITY_HIGH", ValidationSeverity.Warning, "soft"),
            new ValidationFinding("MISSION_NO_UNITS", ValidationSeverity.Error, "blocking"),
            new ValidationFinding("DOCTRINE_RESOLVED", ValidationSeverity.Info, "fyi"),
        });

        var gate = LiveFindingsPresenter.EvaluateGate(report);

        Assert.That(gate.CanExport, Is.False);
        Assert.That(gate.ErrorCount, Is.EqualTo(1));
        Assert.That(gate.WarningCount, Is.EqualTo(1));
        Assert.That(gate.InfoCount, Is.EqualTo(1));
        Assert.That(gate.BlockingReason, Is.Not.Null);
        Assert.That(gate.BlockingReason, Does.Contain("1 error"));
    }

    [Test]
    public void EvaluateGate_allows_export_when_only_warnings_stand()
    {
        var report = ValidationReport.FromFindings(new[]
        {
            new ValidationFinding("EVENT_GRAPH_COMPLEXITY_HIGH", ValidationSeverity.Warning, "soft"),
        });

        var gate = LiveFindingsPresenter.EvaluateGate(report);

        Assert.That(gate.CanExport, Is.True);
        Assert.That(gate.BlockingReason, Is.Null);
        Assert.That(gate.WarningCount, Is.EqualTo(1));
        Assert.That(gate.ErrorCount, Is.Zero);
    }

    [Test]
    public void EvaluateGate_pluralizes_the_blocking_reason()
    {
        var report = ValidationReport.FromFindings(new[]
        {
            new ValidationFinding("MISSION_NO_UNITS", ValidationSeverity.Error, "a"),
            new ValidationFinding("STRIKE_NO_TARGETS", ValidationSeverity.Error, "b"),
        });

        var gate = LiveFindingsPresenter.EvaluateGate(report);

        Assert.That(gate.BlockingReason, Does.Contain("2 errors"));
    }

    [Test]
    public void EvaluateGate_with_no_report_allows_export()
    {
        var gate = LiveFindingsPresenter.EvaluateGate(null);

        Assert.That(gate.CanExport, Is.True);
        Assert.That(gate.Codes, Is.Empty);
        Assert.That(gate.ErrorCount, Is.Zero);
        Assert.That(gate.BlockingReason, Is.Null);
    }

    [Test]
    public void EvaluateGate_honours_a_warning_export_block_floor()
    {
        var report = ValidationReport.FromFindings(new[]
        {
            new ValidationFinding("EVENT_GRAPH_COMPLEXITY_HIGH", ValidationSeverity.Warning, "soft"),
        });

        var gate = LiveFindingsPresenter.EvaluateGate(
            report,
            new ValidationConfig(ExportBlockSeverityFloor: ValidationSeverity.Warning));

        Assert.That(gate.CanExport, Is.False);
    }

    // ---- Integration: the gate reaches a UI through the presenter -----------------------

    [Test]
    public void RefreshImmediate_surfaces_a_blocked_export_gate()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-findings-gate-{Guid.NewGuid():N}.json");
        try
        {
            ScenarioDocumentEditor.CreateNew().Save(path);
            using var session = ScenarioAuthoringSession.Open(path);
            session.Bus.AttachPatrolFromSelection(
                expectedEditVersion: session.EditVersion,
                missionId: "patrol-empty",
                unitIds: Array.Empty<string>(),
                zone:
                [
                    new ScenarioWaypointDto { Lat = 57, Lon = 20 },
                    new ScenarioWaypointDto { Lat = 57.1, Lon = 20.1 },
                    new ScenarioWaypointDto { Lat = 57.2, Lon = 20.2 },
                ],
                save: true);

            var presenter = new LiveFindingsPresenter(session, debounceMs: 0);
            presenter.RefreshImmediate();

            Assert.That(presenter.CanExport, Is.False);
            Assert.That(presenter.ErrorCount, Is.GreaterThanOrEqualTo(1));
            Assert.That(presenter.BlockingReason, Is.Not.Null);
            Assert.That(presenter.HasErrorSeverity, Is.True);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Test]
    public void RefreshImmediate_allows_export_on_a_clean_document()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-findings-clean-{Guid.NewGuid():N}.json");
        try
        {
            ScenarioDocumentEditor.CreateNew().Save(path);
            using var session = ScenarioAuthoringSession.Open(path);
            session.Bus.PlaceUnit(
                expectedEditVersion: session.EditVersion,
                new ScenarioOrbatUnitDto
                {
                    Id = "u1",
                    SideId = "blue",
                    PlatformId = "ffg",
                    Lat = 57,
                    Lon = 20,
                },
                save: true);

            var presenter = new LiveFindingsPresenter(session, debounceMs: 0);
            presenter.RefreshImmediate();

            Assert.That(presenter.CanExport, Is.True);
            Assert.That(presenter.BlockingReason, Is.Null);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    /// <summary>
    /// Regression guard for DRG-55: the report arrives ADR-008 sorted from
    /// <see cref="ValidationReport.FromFindings"/>; the presenter must not re-sort it.
    /// </summary>
    [Test]
    public void LastCodes_preserve_report_order()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-findings-order-{Guid.NewGuid():N}.json");
        try
        {
            ScenarioDocumentEditor.CreateNew().Save(path);
            using var session = ScenarioAuthoringSession.Open(path);
            session.Bus.AttachPatrolFromSelection(
                expectedEditVersion: session.EditVersion,
                missionId: "patrol-empty",
                unitIds: Array.Empty<string>(),
                zone:
                [
                    new ScenarioWaypointDto { Lat = 57, Lon = 20 },
                    new ScenarioWaypointDto { Lat = 57.1, Lon = 20.1 },
                    new ScenarioWaypointDto { Lat = 57.2, Lon = 20.2 },
                ],
                save: true);

            var presenter = new LiveFindingsPresenter(session, debounceMs: 0);
            presenter.RefreshImmediate();

            Assert.That(
                presenter.LastCodes,
                Is.EqualTo(presenter.LastReport!.Findings.Select(f => f.Code).ToArray()));
            Assert.That(
                presenter.LastFindings,
                Is.EqualTo(presenter.LastReport!.Findings));
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
