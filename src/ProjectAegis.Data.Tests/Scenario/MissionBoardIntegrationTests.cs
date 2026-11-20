using ProjectAegis.Data.Scenario.Authoring;
using NUnit.Framework;

namespace ProjectAegis.Data.Tests.Scenario;

/// <summary>
/// ME-W1 Task 8 — Mission Board integration: create → template → clone → list → findings → reload.
/// Headless only (Unity ScenarioMissionBoardWindow deferred).
/// </summary>
public sealed class MissionBoardIntegrationTests
{
    [Test]
    public void Board_list_clone_template_persist_and_findings()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-mb-int-{Guid.NewGuid():N}.json");
        try
        {
            ScenarioDocumentEditor.CreateNew().Save(path);
            using var session = ScenarioAuthoringSession.Open(path);

            var add = session.Bus.AddFromTemplate(
                session.EditVersion,
                templateId: "tpl-patrol-empty",
                newMissionId: "patrol-1",
                save: true);
            Assert.That(add.Ok, Is.True, add.ErrorMessage);
            Assert.That(add.Report, Is.Not.Null);
            Assert.That(add.Report!.Findings.Any(f => f.Code == "MISSION_NO_UNITS"), Is.True);

            var clone = session.Bus.CloneMission(
                session.EditVersion,
                sourceId: "patrol-1",
                newId: "patrol-2",
                save: true);
            Assert.That(clone.Ok, Is.True, clone.ErrorMessage);
            Assert.That(clone.Report, Is.Not.Null);
            Assert.That(clone.Report!.Findings.Any(f => f.Code == "MISSION_NO_UNITS"), Is.True);

            var rows = MissionBoardQuery.List(session.Editor.ToDto(), typeFilter: "Patrol");
            Assert.That(rows.Count, Is.EqualTo(2));
            Assert.That(rows.Select(r => r.Id).ToArray(), Is.EqualTo(new[] { "patrol-1", "patrol-2" }));
            foreach (var r in rows)
{
Assert.That(r.Status, Is.EqualTo("Unassigned"));
}

            var live = session.Editor.LiveValidate();
            Assert.That(live.Findings.Any(f => f.Code == "MISSION_NO_UNITS"), Is.True);

            var reloaded = ScenarioDocumentJsonLoader.LoadFromFile(path);
            Assert.That(reloaded.Missions.Count, Is.EqualTo(2));
            Assert.That(                reloaded.Missions.Any(                m => m.Id == "patrol-1"
                     && string.Equals(m.Type, "Patrol", StringComparison.OrdinalIgnoreCase)), Is.True);
            Assert.That(                reloaded.Missions.Any(                m => m.Id == "patrol-2"
                     && string.Equals(m.Type, "Patrol", StringComparison.OrdinalIgnoreCase)), Is.True);
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
