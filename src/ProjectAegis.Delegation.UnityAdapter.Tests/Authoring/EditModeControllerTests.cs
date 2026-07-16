using ProjectAegis.Data.Scenario.Authoring;
using ProjectAegis.Delegation.UnityAdapter.Authoring;
using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Authoring;

/// <summary>
/// EditModeController (Phase 2.1 Task 5): default Edit, play gate on error findings, ORBAT unchanged.
/// </summary>
public sealed class EditModeControllerTests
{
    [Test]
    public void Default_mode_is_Edit_when_constructed()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-editmode-default-{Guid.NewGuid():N}.json");
        try
        {
            ScenarioDocumentEditor.CreateNew().Save(path);
            using var session = ScenarioAuthoringSession.Open(path);
            var findings = new LiveFindingsPresenter(session, debounceMs: 0);
            var controller = new EditModeController(session, findings);

            Assert.That(controller.Mode, Is.EqualTo(ScenarioHostMode.Edit));
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
    public void TryEnterPlay_blocked_when_error_findings_present()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-editmode-block-{Guid.NewGuid():N}.json");
        try
        {
            ScenarioDocumentEditor.CreateNew().Save(path);
            using var session = ScenarioAuthoringSession.Open(path);
            // Empty patrol → MISSION_NO_UNITS (Error severity).
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

            var findings = new LiveFindingsPresenter(session, debounceMs: 0);
            var controller = new EditModeController(session, findings);

            var entered = controller.TryEnterPlay(forceConfirmInvalid: false);

            Assert.That(entered, Is.False);
            Assert.That(controller.Mode, Is.EqualTo(ScenarioHostMode.Edit));
            Assert.That(findings.HasErrorSeverity, Is.True);
            Assert.That(findings.LastCodes, Does.Contain("MISSION_NO_UNITS"));
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
    public void TryEnterPlay_allowed_when_no_error_findings()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-editmode-allow-{Guid.NewGuid():N}.json");
        try
        {
            ScenarioDocumentEditor.CreateNew().Save(path);
            using var session = ScenarioAuthoringSession.Open(path);
            // Clean place-unit only — no missions, so no MISSION_NO_UNITS.
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

            var findings = new LiveFindingsPresenter(session, debounceMs: 0);
            var controller = new EditModeController(session, findings);

            var entered = controller.TryEnterPlay();

            Assert.That(entered, Is.True);
            Assert.That(controller.Mode, Is.EqualTo(ScenarioHostMode.Play));
            Assert.That(findings.HasErrorSeverity, Is.False);
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
    public void Mode_switch_does_not_change_ORBAT_unit_count()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-editmode-orbat-{Guid.NewGuid():N}.json");
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

            var countBefore = session.Editor.ToDto().Orbat?.Units.Count ?? 0;
            Assert.That(countBefore, Is.EqualTo(1));

            var findings = new LiveFindingsPresenter(session, debounceMs: 0);
            var controller = new EditModeController(session, findings);

            Assert.That(controller.TryEnterPlay(), Is.True);
            Assert.That(session.Editor.ToDto().Orbat?.Units.Count ?? 0, Is.EqualTo(countBefore));

            controller.EnterEdit();
            Assert.That(controller.Mode, Is.EqualTo(ScenarioHostMode.Edit));
            Assert.That(session.Editor.ToDto().Orbat?.Units.Count ?? 0, Is.EqualTo(countBefore));
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
