using ProjectAegis.Data.Scenario.Authoring;
using System.Text.Json;
using NUnit.Framework;

namespace ProjectAegis.MissionEditor.Cli.Tests;

[TestFixture]
public sealed class ScenarioNearFutureSpawnCommandTests
{
    [Test]
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

            Assert.That(exit, Is.EqualTo(0));
            Assert.That(json, Does.Contain("cca-wingman"));
            Assert.That(json, Does.Contain("\"accepted\": 1"));
        }
        finally
        {
            File.Delete(path);
        }
    }
}