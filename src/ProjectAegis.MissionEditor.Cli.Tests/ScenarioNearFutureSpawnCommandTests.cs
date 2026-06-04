using ProjectAegis.Data.Scenario.Authoring;
using ProjectAegis.MissionEditor.Cli;
using System.Text.Json;
using Xunit;

namespace ProjectAegis.MissionEditor.Cli.Tests;

public sealed class ScenarioNearFutureSpawnCommandTests
{
    [Fact]
    public void Run_lists_gated_spawns_from_scenario_metadata()
    {
        var path = Path.Combine(Path.GetTempPath(), $"nf-spawn-{Guid.NewGuid():N}.json");
        var scenario = new ScenarioDocumentDto
        {
            Metadata = new ScenarioMetadataDto
            {
                MaxTechnologyLevel = 2,
                NearFutureUnits =
                [
                    new ScenarioNearFutureUnitDto { ArchetypeId = "cca-wingman", UnitId = "cca-1" },
                ],
            },
        };
        File.WriteAllText(path, JsonSerializer.Serialize(scenario));

        try
        {
            using var writer = new StringWriter();
            var exit = ScenarioNearFutureSpawnCommand.Run(path, writer);
            var json = writer.ToString();

            Assert.Equal(0, exit);
            Assert.Contains("cca-wingman", json);
            Assert.Contains("\"accepted\": 1", json);
        }
        finally
        {
            File.Delete(path);
        }
    }
}