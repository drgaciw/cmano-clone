using ProjectAegis.Data.Scenario.Authoring;
using NUnit.Framework;

namespace ProjectAegis.Data.Tests.Scenario;

/// <summary>
/// ScenarioEditCommandBus + ScenarioAuthoringSession (Phase 2.1 Task 3):
/// editVersion concurrency, dirty/save, and live findings after mutations.
/// </summary>
public sealed class ScenarioEditCommandBusTests
{
    [Test]
    public void Apply_place_unit_bumps_edit_version_marks_dirty_and_validates()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-bus-{Guid.NewGuid():N}.json");
        try
        {
            ScenarioDocumentEditor.CreateNew().Save(path);
            using var session = ScenarioAuthoringSession.Open(path);
            Assert.That(session.IsDirty, Is.False);
            Assert.That(session.EditVersion, Is.EqualTo(1));

            var result = session.Bus.PlaceUnit(
                expectedEditVersion: 1,
                new ScenarioOrbatUnitDto
                {
                    Id = "u1",
                    SideId = "blue",
                    PlatformId = "ffg",
                    Lat = 57,
                    Lon = 20,
                },
                save: true);

            Assert.That(result.Ok, Is.True);
            Assert.That(result.EditVersion, Is.EqualTo(2));
            Assert.That(result.Report, Is.Not.Null);
            // Fresh save clears dirty if save:true
            Assert.That(session.IsDirty, Is.False);

            var reloaded = ScenarioDocumentJsonLoader.LoadFromFile(path);
            Assert.That(reloaded.Orbat!.Units, Has.Exactly(1).Items);
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
    public void Apply_stale_edit_version_returns_conflict_without_write()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-bus-c-{Guid.NewGuid():N}.json");
        try
        {
            ScenarioDocumentEditor.CreateNew().Save(path);
            using var session = ScenarioAuthoringSession.Open(path);

            var result = session.Bus.PlaceUnit(
                expectedEditVersion: 99,
                new ScenarioOrbatUnitDto
                {
                    Id = "u1",
                    SideId = "blue",
                    PlatformId = "ffg",
                    Lat = 1,
                    Lon = 2,
                },
                save: true);

            Assert.That(result.Ok, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo(ScenarioEditVersionGuard.ConflictCode));
            Assert.That(ScenarioDocumentJsonLoader.LoadFromFile(path).Metadata.EditVersion, Is.EqualTo(1));
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
    public void Live_findings_include_mission_no_units_after_empty_patrol_attach()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-bus-v-{Guid.NewGuid():N}.json");
        try
        {
            ScenarioDocumentEditor.CreateNew().Save(path);
            using var session = ScenarioAuthoringSession.Open(path);
            session.Bus.AttachPatrolFromSelection(
                expectedEditVersion: 1,
                missionId: "patrol-empty",
                unitIds: Array.Empty<string>(),
                zone:
                [
                    new ScenarioWaypointDto { Lat = 57, Lon = 20 },
                    new ScenarioWaypointDto { Lat = 57.1, Lon = 20.1 },
                    new ScenarioWaypointDto { Lat = 57.2, Lon = 20.2 },
                ],
                save: true);

            var codes = session.Bus.LastReport!.Findings.Select(f => f.Code).ToHashSet();
            Assert.That(codes, Does.Contain("MISSION_NO_UNITS"));
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
