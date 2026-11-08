using ProjectAegis.Data.Scenario.Authoring;
using Xunit;

namespace ProjectAegis.Data.Tests.Scenario;

/// <summary>
/// ME-W1 Task 8 — Mission Board integration: create → template → clone → list → findings → reload.
/// Headless only (Unity ScenarioMissionBoardWindow deferred).
/// </summary>
public sealed class MissionBoardIntegrationTests
{
    [Fact]
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
            Assert.True(add.Ok, add.ErrorMessage);
            Assert.NotNull(add.Report);
            Assert.Contains(add.Report!.Findings, f => f.Code == "MISSION_NO_UNITS");

            var clone = session.Bus.CloneMission(
                session.EditVersion,
                sourceId: "patrol-1",
                newId: "patrol-2",
                save: true);
            Assert.True(clone.Ok, clone.ErrorMessage);
            Assert.NotNull(clone.Report);
            Assert.Contains(clone.Report!.Findings, f => f.Code == "MISSION_NO_UNITS");

            var rows = MissionBoardQuery.List(session.Editor.ToDto(), typeFilter: "Patrol");
            Assert.Equal(2, rows.Count);
            Assert.Equal(new[] { "patrol-1", "patrol-2" }, rows.Select(r => r.Id).ToArray());
            Assert.All(rows, r => Assert.Equal("Unassigned", r.Status));

            var live = session.Editor.LiveValidate();
            Assert.Contains(live.Findings, f => f.Code == "MISSION_NO_UNITS");

            var reloaded = ScenarioDocumentJsonLoader.LoadFromFile(path);
            Assert.Equal(2, reloaded.Missions.Count);
            Assert.Contains(
                reloaded.Missions,
                m => m.Id == "patrol-1"
                     && string.Equals(m.Type, "Patrol", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(
                reloaded.Missions,
                m => m.Id == "patrol-2"
                     && string.Equals(m.Type, "Patrol", StringComparison.OrdinalIgnoreCase));
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
