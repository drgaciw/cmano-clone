using ProjectAegis.Data.Scenario.Authoring;
using Xunit;

namespace ProjectAegis.Data.Tests.Scenario;

public sealed class MissionTemplateCatalogTests
{
    [Fact]
    public void All_contains_four_builtin_templates()
    {
        Assert.Equal(4, MissionTemplateCatalog.All.Count);
        Assert.Contains(MissionTemplateCatalog.All, t => t.TemplateId == "tpl-patrol-empty");
    }

    [Fact]
    public void Materialize_patrol_has_three_waypoints_and_no_units()
    {
        var m = MissionTemplateCatalog.Materialize("tpl-patrol-empty", "p-new");
        Assert.Equal("p-new", m.Id);
        Assert.Equal("Patrol", m.Type);
        Assert.Empty(m.AssignedUnitIds);
        Assert.Equal(3, m.PatrolZone.Count);
    }

    [Fact]
    public void Materialize_unknown_throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            MissionTemplateCatalog.Materialize("nope", "x"));
    }

    [Fact]
    public void Editor_AddMissionFromTemplate_adds_and_rejects_duplicate()
    {
        var editor = ScenarioDocumentEditor.CreateNew();
        editor.AddMissionFromTemplate("tpl-strike-empty", "s-new");
        Assert.Single(editor.Missions);
        Assert.Equal("Strike", editor.Missions[0].Type);
        Assert.Throws<InvalidOperationException>(() =>
            editor.AddMissionFromTemplate("tpl-strike-empty", "s-new"));
    }
}
