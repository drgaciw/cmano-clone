using ProjectAegis.Data.Scenario.Authoring;
using NUnit.Framework;

namespace ProjectAegis.Data.Tests.Scenario;

public sealed class MissionTemplateCatalogTests
{
    [Test]
    public void All_contains_four_builtin_templates()
    {
        Assert.That(MissionTemplateCatalog.All.Count, Is.EqualTo(4));
        Assert.That(MissionTemplateCatalog.All.Any(t => t.TemplateId == "tpl-patrol-empty"), Is.True);
    }

    [Test]
    public void Materialize_patrol_has_three_waypoints_and_no_units()
    {
        var m = MissionTemplateCatalog.Materialize("tpl-patrol-empty", "p-new");
        Assert.That(m.Id, Is.EqualTo("p-new"));
        Assert.That(m.Type, Is.EqualTo("Patrol"));
        Assert.That(m.AssignedUnitIds, Is.Empty);
        Assert.That(m.PatrolZone.Count, Is.EqualTo(3));
    }

    [Test]
    public void Materialize_unknown_throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            MissionTemplateCatalog.Materialize("nope", "x"));
    }

    [Test]
    public void Editor_AddMissionFromTemplate_adds_and_rejects_duplicate()
    {
        var editor = ScenarioDocumentEditor.CreateNew();
        editor.AddMissionFromTemplate("tpl-strike-empty", "s-new");
        Assert.That(editor.Missions, Has.Exactly(1).Items);
        Assert.That(editor.Missions[0].Type, Is.EqualTo("Strike"));
        Assert.Throws<InvalidOperationException>(() =>
            editor.AddMissionFromTemplate("tpl-strike-empty", "s-new"));
    }
}
