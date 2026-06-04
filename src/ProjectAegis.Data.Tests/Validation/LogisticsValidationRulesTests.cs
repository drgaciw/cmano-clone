using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Scenario.Authoring;
using ProjectAegis.Data.Validation;
using Xunit;

namespace ProjectAegis.Data.Tests.Validation;

public sealed class LogisticsValidationRulesTests
{
    [Fact]
    public void Strike_with_not_ready_unit_emits_AIR_NOT_READY()
    {
        var scenario = new ScenarioDocumentDto
        {
            Metadata = new ScenarioMetadataDto
            {
                UnitReadiness = new Dictionary<string, ScenarioUnitReadinessDto>
                {
                    ["u1"] = new() { ReadyForLaunch = false },
                },
            },
            Missions =
            [
                new ScenarioMissionDto
                {
                    Id = "strike-1",
                    Type = "Strike",
                    AssignedUnitIds = ["u1"],
                    TargetIds = ["hostile-1"],
                },
            ],
        };

        var report = new ScenarioValidationEngine().Validate(
            scenario,
            InMemoryCatalogReader.BalticPatrolFixture(),
            new ValidationConfig());

        Assert.Contains(report.Findings, f => f.Code == "AIR_NOT_READY");
    }

    [Fact]
    public void Ferry_beyond_fuel_range_emits_FERRY_UNREACHABLE_FUEL()
    {
        var scenario = new ScenarioDocumentDto
        {
            Missions =
            [
                new ScenarioMissionDto
                {
                    Id = "ferry-1",
                    Type = "Ferry",
                    AssignedUnitIds = ["u1"],
                    FerryDestinationBaseId = "hostile-far",
                },
            ],
        };

        var catalog = InMemoryCatalogReader.BalticPatrolFixture();
        var report = new ScenarioValidationEngine().Validate(scenario, catalog, new ValidationConfig());
        Assert.Contains(
            report.Findings,
            f => f.Code is "FERRY_UNREACHABLE" or "FERRY_UNREACHABLE_FUEL");
    }
}