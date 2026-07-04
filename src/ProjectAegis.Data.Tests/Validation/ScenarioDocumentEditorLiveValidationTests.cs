using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Scenario.Authoring;
using ProjectAegis.Data.Validation;
using Xunit;

public sealed class ScenarioDocumentEditorLiveValidationTests
{
    [Fact]
    public void Editor_live_MissionNoUnitsRule_from_clean_CreateNew()
    {
        var editor = ScenarioDocumentEditor.CreateNew(dbRef: "baltic_patrol");
        editor.AddStrikeMission("m-no-units", new string[0], new[] { "hostile-1" });
        var report = new ScenarioValidationEngine().Validate(editor.ToDto(), InMemoryCatalogReader.BalticPatrolFixture(), new ValidationConfig());
        Assert.Contains(report.Findings, f => f.Code == "MISSION_NO_UNITS");
    }

    [Fact]
    public void Editor_live_PatrolZoneRule_from_clean_CreateNew()
    {
        var editor = ScenarioDocumentEditor.CreateNew(dbRef: "baltic_patrol");
        editor.AddPatrolMission("p-degen", new[] { "u1" }, new[] { new ScenarioWaypointDto { Lat = 57, Lon = 20 } });
        var report = new ScenarioValidationEngine().Validate(editor.ToDto(), InMemoryCatalogReader.BalticPatrolFixture(), new ValidationConfig());
        Assert.Contains(report.Findings, f => f.Code == "PATROL_ZONE_DEGENERATE");
    }

    [Fact]
    public void Editor_live_StrikeNoTargetsRule_from_clean_CreateNew()
    {
        var editor = ScenarioDocumentEditor.CreateNew(dbRef: "baltic_patrol");
        editor.AddStrikeMission("s-no-tgt", new[] { "u1" }, new string[0]);
        var report = new ScenarioValidationEngine().Validate(editor.ToDto(), InMemoryCatalogReader.BalticPatrolFixture(), new ValidationConfig());
        Assert.Contains(report.Findings, f => f.Code == "STRIKE_NO_TARGETS");
    }

    [Fact]
    public void Editor_live_FerryDestinationRule_from_clean_CreateNew()
    {
        var editor = ScenarioDocumentEditor.CreateNew(dbRef: "baltic_patrol");
        editor.AddFerryMission("f-no-dest", new[] { "u1" }, "");
        var report = new ScenarioValidationEngine().Validate(editor.ToDto(), InMemoryCatalogReader.BalticPatrolFixture(), new ValidationConfig());
        Assert.Contains(report.Findings, f => f.Code == "FERRY_NO_DESTINATION");
    }

    [Fact]
    public void Editor_live_IncompatibleHostRule_from_clean_CreateNew()
    {
        var editor = ScenarioDocumentEditor.CreateNew(dbRef: "baltic_patrol");
        // air unit + no carrier mission triggers the rule
        editor.AddStrikeMission("inc-host", new[] { "air-unit-incompatible" }, new[] { "bad-host" });
        var report = new ScenarioValidationEngine().Validate(editor.ToDto(), InMemoryCatalogReader.BalticPatrolFixture(), new ValidationConfig());
        Assert.Contains(report.Findings, f => f.Code == "INCOMPATIBLE_HOST");
    }

    [Fact]
    public void Editor_live_BrokenRefRule_from_clean_CreateNew()
    {
        var editor = ScenarioDocumentEditor.CreateNew(dbRef: "baltic_patrol");
        editor.AddStrikeMission("broke", new[] { "u1" }, new[] { "ref:missing-unit" });
        var report = new ScenarioValidationEngine().Validate(editor.ToDto(), InMemoryCatalogReader.BalticPatrolFixture(), new ValidationConfig());
        Assert.Contains(report.Findings, f => f.Code == "BROKEN_REF");
    }
}
