using ProjectAegis.Data.Scenario.Authoring;
using NUnit.Framework;

namespace ProjectAegis.Data.Tests.Scenario;

public sealed class ScenarioMissionCloneEditorTests
{
    [Test]
    public void CloneMission_copies_fields_under_new_id()
    {
        var editor = ScenarioDocumentEditor.CreateNew();
        editor.AddPatrolMission("patrol-1", ["u1"],
        [
            new ScenarioWaypointDto { Lat = 57, Lon = 20 },
            new ScenarioWaypointDto { Lat = 57.1, Lon = 20.1 },
            new ScenarioWaypointDto { Lat = 57.2, Lon = 20 },
        ]);
        editor.CloneMission("patrol-1", "patrol-1-copy");

        Assert.That(editor.Missions.Count, Is.EqualTo(2));
        var copy = editor.Missions.Single(m => m.Id == "patrol-1-copy");
        Assert.That(copy.Type, Is.EqualTo("Patrol"));
        Assert.That(copy.AssignedUnitIds, Is.EqualTo(new[] { "u1" }));
        Assert.That(copy.PatrolZone.Count, Is.EqualTo(3));
    }

    [Test]
    public void CloneMission_duplicate_new_id_throws()
    {
        var editor = ScenarioDocumentEditor.CreateNew();
        editor.AddStrikeMission("s1", ["u1"], ["t1"]);
        Assert.Throws<InvalidOperationException>(() => editor.CloneMission("s1", "s1"));
    }

    [Test]
    public void CloneMission_missing_source_throws()
    {
        var editor = ScenarioDocumentEditor.CreateNew();
        Assert.Throws<InvalidOperationException>(() => editor.CloneMission("nope", "x"));
    }

    [Test]
    public void CloneMission_strike_and_ferry_and_support()
    {
        var editor = ScenarioDocumentEditor.CreateNew();
        editor.AddStrikeMission("s1", ["u1"], ["t1"]);
        editor.AddFerryMission("f1", ["u1"], "base-a");
        editor.AddSupportMission("sup1", ["u1"], "Tanker",
        [
            new ScenarioWaypointDto { Lat = 1, Lon = 1 },
            new ScenarioWaypointDto { Lat = 2, Lon = 2 },
            new ScenarioWaypointDto { Lat = 3, Lon = 3 },
        ]);
        editor.CloneMission("s1", "s2");
        editor.CloneMission("f1", "f2");
        editor.CloneMission("sup1", "sup2");
        Assert.That(editor.Missions.Single(m => m.Id == "s2").Type, Is.EqualTo("Strike"));
        Assert.That(editor.Missions.Single(m => m.Id == "f2").FerryDestinationBaseId, Is.EqualTo("base-a"));
        Assert.That(editor.Missions.Single(m => m.Id == "sup2").SupportRole, Is.EqualTo("Tanker"));
    }
}
