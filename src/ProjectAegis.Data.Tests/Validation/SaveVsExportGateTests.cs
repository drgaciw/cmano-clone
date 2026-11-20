namespace ProjectAegis.Data.Tests.Validation;

using System.Collections.Generic;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Scenario.Authoring;
using ProjectAegis.Data.Validation;
using NUnit.Framework;

using CatalogTlTier = ProjectAegis.Data.Catalog.CatalogTlTier;

/// <summary>
/// QA-plan unit #12 (production/qa/qa-plan-scenario-editor-2026-07-01.md, AC-12): proves the
/// "save-vs-export gate" — a scenario with blocking validation errors can still be SAVED, but
/// export/play/simulate must be REJECTED for that same file via
/// <see cref="ScenarioValidationExportGate"/>. Also proves the export-block severity floor
/// tuning knob (<see cref="ValidationConfig.ExportBlockSeverityFloor"/>).
///
/// S82-03: clear save-ok/export-blocked test; hardened gate. Cites boundary + sprint-82 + execute-plan + roadmap.
/// </summary>
public sealed class SaveVsExportGateTests
{
    [Test]
    public void Save_succeeds_for_scenario_with_blocking_error()
    {
        var editor = ScenarioDocumentEditor.CreateNew(dbRef: "baltic_patrol");
        editor.AddStrikeMission("strike-1", new[] { "u1" }, new string[0]);

        var path = Path.Combine(Path.GetTempPath(), $"aegis-save-vs-export-{Guid.NewGuid():N}.json");
        try
        {
            editor.Save(path);

            Assert.That(File.Exists(path), Is.True);

            var expectedJson = ScenarioDocumentJsonWriter.Serialize(editor.ToDto());
            var actualJson = File.ReadAllText(path);
            Assert.That(actualJson, Is.EqualTo(expectedJson));

            var reloaded = ScenarioDocumentEditor.Load(path);
            var mission = AssertEx.Single(reloaded.Missions);
            Assert.That(mission.Id, Is.EqualTo("strike-1"));
            Assert.That(mission.Type, Is.EqualTo("Strike"));
            Assert.That(mission.TargetIds, Is.Empty);
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
    public void Export_is_rejected_for_the_same_scenario_that_saved_successfully()
    {
        // S82-03 / AC-12 (qa-plan-scenario-editor-2026-07-01.md unit #12):
        // Clear assertion: save path succeeds for blocking-error state; export gate + Prepare
        // (used by scenario_export_brief / simulate / publish) are blocked on the *same file*.
        // Cites: scenario-editor-scope-boundary-2026-07-04.md, roadmap-execute-plan-07042026.md,
        // production/sprints/sprint-82-validation-tracks-ac.md, 11-Agentic-Mission-Editor.md AME-6.5.
        var editor = ScenarioDocumentEditor.CreateNew(dbRef: "baltic_patrol");
        editor.AddStrikeMission("strike-1", new[] { "u1" }, new string[0]);

        var path = Path.Combine(Path.GetTempPath(), $"aegis-save-vs-export-{Guid.NewGuid():N}.json");
        try
        {
            editor.Save(path);
            Assert.That(File.Exists(path), Is.True, "save must succeed for scenario with blocking validation errors");

            // Reload from the persisted file to prove the saved artifact carries the bad state.
            var reloaded = ScenarioDocumentEditor.Load(path);
            var reloadedDto = reloaded.ToDto();

            var catalog = ValidationCatalogFixture.Default();

            // Direct gate (used by scenario_validate and export_brief pre-check)
            var (allowed, report) = ScenarioValidationExportGate.EvaluateExport(reloadedDto, catalog);
            Assert.That(allowed, Is.False, "export must be blocked (via gate) for the scenario that saved successfully");
            Assert.That(report.Findings.Any(f => f.Code == "STRIKE_NO_TARGETS" && f.Severity == ValidationSeverity.Error), Is.True);

            // Real export pipeline path (Prepare applies transforms then gate; used by simulate + publish)
            var exportPackage = ScenarioExportCommand.Prepare(reloadedDto, catalog);
            Assert.That(exportPackage.Allowed, Is.False);
            Assert.That(exportPackage.ValidationReport, Is.Not.Null);
            Assert.That(exportPackage.ValidationReport.Findings.Any(f => f.Code == "STRIKE_NO_TARGETS"), Is.True);
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
    public void Warning_only_scenario_saves_and_is_allowed_to_export_under_default_severity_floor()
    {
        // dbRef intentionally omitted: the test catalog fixture only resolves TL-0 via the
        // release-train path, and an explicit dbRef binding would introduce an unrelated
        // TL_RELEASE_TRAIN_MISMATCH finding that is orthogonal to what this test isolates.
        var editor = ScenarioDocumentEditor.CreateNew(dbRef: null);
        editor.AddPatrolMission(
            "patrol-1",
            new[] { "u1" },
            new[]
            {
                new ScenarioWaypointDto { Lat = 57.0, Lon = 20.0 },
                new ScenarioWaypointDto { Lat = 57.1, Lon = 20.1 },
                new ScenarioWaypointDto { Lat = 57.2, Lon = 20.2 },
            });

        var path = Path.Combine(Path.GetTempPath(), $"aegis-save-vs-export-{Guid.NewGuid():N}.json");
        try
        {
            editor.Save(path);
            Assert.That(File.Exists(path), Is.True);

            var warningOnlyReport = ValidationReport.FromFindings(new List<ValidationFinding>
            {
                new ValidationFinding(
                    Code: "PATROL_ZONE_SUBOPTIMAL",
                    Severity: ValidationSeverity.Warning,
                    Message: "Patrol zone geometry is suboptimal but not blocking.",
                    MissionId: "patrol-1"),
            });

            // Hand-built report proves the gate's contract directly: Warning findings do not
            // block export under the default severity floor (Error).
            Assert.That(warningOnlyReport.CanExport(new ValidationConfig()), Is.True);

            var catalog = ValidationCatalogFixture.Default();
            var (allowed, _) = ScenarioValidationExportGate.EvaluateExport(editor.ToDto(), catalog);
            Assert.That(allowed, Is.True);
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
    public void Warning_only_report_is_blocked_when_severity_floor_is_tuned_to_warning()
    {
        var warningOnlyReport = ValidationReport.FromFindings(new List<ValidationFinding>
        {
            new ValidationFinding(
                Code: "PATROL_ZONE_SUBOPTIMAL",
                Severity: ValidationSeverity.Warning,
                Message: "Patrol zone geometry is suboptimal but not blocking.",
                MissionId: "patrol-1"),
        });

        var strictConfig = new ValidationConfig(ExportBlockSeverityFloor: ValidationSeverity.Warning);

        Assert.That(warningOnlyReport.CanExport(strictConfig), Is.False);
    }

    /// <summary>Private, scoped test double for <see cref="ICatalogReader"/> — mirrors the fixture
    /// pattern in <c>ScenarioValidationEngineTests</c> without sharing a type across test classes.</summary>
    private sealed class ValidationCatalogFixture : ICatalogReader
    {
        private readonly double _combatRadiusNm;
        private readonly double _separationNm;

        private ValidationCatalogFixture(double combatRadiusNm, double separationNm)
        {
            _combatRadiusNm = combatRadiusNm;
            _separationNm = separationNm;
        }

        public static ICatalogReader Default() => new ValidationCatalogFixture(500, 200);

        public string LayerVersion => "save-vs-export-test";

        public IReadOnlyList<CatalogSensorBinding> GetSortedSensorBindings() => Array.Empty<CatalogSensorBinding>();

        public bool TryGetBasePd(string platformId, string sensorId, out double basePd)
        {
            basePd = 0;
            return false;
        }

        public bool TryResolveDbRef(string dbRef, out string resolvedSnapshotId)
        {
            resolvedSnapshotId = dbRef;
            return true;
        }

        public bool TryGetSnapshotBranch(string snapshotId, out string branch)
        {
            branch = CatalogTlTier.Tl0;
            return true;
        }

        public bool TryResolveSnapshotForTlBranch(string tlBranch, out string snapshotId, out string dbRef)
        {
            snapshotId = "";
            dbRef = "";

            if (!string.Equals(CatalogTlTier.Normalize(tlBranch), CatalogTlTier.Tl0, StringComparison.Ordinal))
            {
                return false;
            }

            snapshotId = "validation-snapshot";
            dbRef = "validation-snapshot";
            return true;
        }

        public bool TryGetCombatRadiusNm(string platformId, out double combatRadiusNm)
        {
            combatRadiusNm = _combatRadiusNm;
            return platformId == "u1";
        }

        public bool TryGetPlatformPosition(string platformId, out double latDeg, out double lonDeg)
        {
            if (platformId == "u1")
            {
                latDeg = 57.0;
                lonDeg = 20.0;
                return true;
            }

            if (platformId == "tgt-1")
            {
                latDeg = 57.0 + (_separationNm / 60.0);
                lonDeg = 20.0;
                return true;
            }

            latDeg = 0;
            lonDeg = 0;
            return false;
        }
    }
}
