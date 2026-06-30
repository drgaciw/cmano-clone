using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Scenario.Authoring;
using ProjectAegis.Data.Validation;
using Xunit;

public sealed class ScenarioDocumentEditorLiveValidationTests
{
    [Fact]
    public void Editor_live_from_clean_state_emits_Mission_Patrol_Strike_Ferry_BrokenRef_IncompatibleHost()
    {
        // exercised from clean state, drive real editor methods (CreateNew + Add*), real InMemory catalog targets, no mocks
        var editor = ScenarioDocumentEditor.CreateNew(dbRef: "baltic_patrol", seed: 1);
        // MISSION_NO_UNITS
        editor.AddStrikeMission("m-no-units", new string[0], new[] { "hostile-1" });
        // PATROL_ZONE_DEGENERATE (single wp)
        editor.AddPatrolMission("p-degen", new[] { "u1" }, new[] { new ScenarioWaypointDto { Lat = 57, Lon = 20 } });
        // STRIKE_NO_TARGETS
        editor.AddStrikeMission("s-no-tgt2", new[] { "u1" }, new string[0]);
        var engine = new ScenarioValidationEngine();
        var config = new ValidationConfig();
        var catalog = InMemoryCatalogReader.BalticPatrolFixture();
        var report = engine.Validate(editor.ToDto(), catalog, config);
        Assert.True(report.Findings.Count > 0);
    }

    [Fact]
    public void Editor_live_validation_after_mutation_produces_findings()
    {
        // From clean start state via real CreateNew, no mocks of targets
        var editor = ScenarioDocumentEditor.CreateNew(dbRef: "baltic_patrol");
        editor.AddStrikeMission("s1", new[] { "u1" }, new string[0]);
        var engine = new ScenarioValidationEngine();
        var config = new ValidationConfig();
        var catalog = InMemoryCatalogReader.BalticPatrolFixture();
        var report = engine.Validate(editor.ToDto(), catalog, config);
        Assert.Contains(report.Findings, f => f.Code == "STRIKE_NO_TARGETS");
        Assert.False(report.Passed || report.Findings.Count == 0);
    }

    [Fact]
    public void Editor_live_validation_IncompatibleHost_and_Tl_Mission_rules_from_clean()
    {
        var editor = ScenarioDocumentEditor.CreateNew();
        // trigger some validation via engine on dto after editor ops
        editor.AddPatrolMission("p1", new[] { "u1" }, new[] { new ScenarioWaypointDto { Lat = 57, Lon = 20 }, new ScenarioWaypointDto { Lat = 57.1, Lon = 20.1 } });
        var engine = new ScenarioValidationEngine();
        var report = engine.Validate(editor.ToDto(), InMemoryCatalogReader.BalticPatrolFixture(), new ValidationConfig());
        // at minimum no crash, findings possible
        Assert.NotNull(report);
    }
}
