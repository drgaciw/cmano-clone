namespace ProjectAegis.Delegation.UnityAdapter.Tests.Editor;

using ProjectAegis.Data.Scenario.Authoring;
using ProjectAegis.Delegation.UnityAdapter.Editor;
using NUnit.Framework;

[TestFixture]
public sealed class ScenarioEditorHostAdapterTests
{
    [Test]
    public void Headless_mcp_scenario_loads_with_default_editor_state_ac8()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ac8-{Guid.NewGuid():N}.json");
        try
        {
            var editor = ScenarioDocumentEditor.CreateNew();
            editor.AddPatrolMission(
                "patrol-1",
                ["u1"],
                [
                    new ScenarioWaypointDto { Lat = 58.0, Lon = 21.0 },
                    new ScenarioWaypointDto { Lat = 58.1, Lon = 21.1 },
                    new ScenarioWaypointDto { Lat = 58.2, Lon = 21.2 },
                ]);
            editor.CommitMutation();
            editor.Save(path);

            var host = ScenarioEditorHostAdapter.LoadForEditMode(path);
            Assert.That(host.Document.Missions, Has.Count.EqualTo(1));
            Assert.That(host.EditorState, Is.Not.Null);
            Assert.That(host.EditorState!.LayersVisible, Is.True);
            Assert.That(host.EditorState.CameraLat, Is.EqualTo(58.1).Within(0.1));
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
