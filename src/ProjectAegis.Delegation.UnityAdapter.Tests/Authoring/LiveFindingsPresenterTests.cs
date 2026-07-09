using ProjectAegis.Data.Scenario.Authoring;
using ProjectAegis.Delegation.UnityAdapter.Authoring;
using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Authoring;

/// <summary>
/// LiveFindingsPresenter (Phase 2.1 Task 5): code parity with LiveValidate, debounceMs:0 schedule.
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

            var editorCodes = session.Editor.LiveValidate().Findings
                .Select(f => f.Code)
                .OrderBy(c => c, StringComparer.Ordinal)
                .ToArray();

            Assert.That(presenter.LastCodes, Is.EqualTo(editorCodes));
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
}
