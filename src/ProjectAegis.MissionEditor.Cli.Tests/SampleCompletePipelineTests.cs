using ProjectAegis.Data.Scenario.Authoring;
using ProjectAegis.MissionEditor.Cli;
using Xunit;

namespace ProjectAegis.MissionEditor.Cli.Tests;

public sealed class SampleCompletePipelineTests
{
    [Fact]
    public void strike_patrol_support_ferry_pipeline_validates_and_emits_sample_complete()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-ac5-{Guid.NewGuid():N}.json");
        try
        {
            using (var writer = new StringWriter())
            {
                Assert.Equal(0, ScenarioCreateCommand.Run(path, "baltic_patrol", "baltic-patrol-catalog", 42, writer));
            }

            var zone = CliArgParser.ParseWaypoints(["57,20", "57.1,20.1", "57.2,20.2"]);
            var station = CliArgParser.ParseWaypoints(["57.3,20.3", "57.4,20.4", "57.5,20.5"]);

            Assert.Equal(0, MissionAddPatrolCommand.Run(path, 1, "patrol-1", ["u1"], zone, new StringWriter()));
            Assert.Equal(0, MissionAddStrikeCommand.Run(path, 2, "strike-1", ["u1"], ["hostile-1"], new StringWriter()));
            Assert.Equal(0, MissionAddSupportCommand.Run(path, 3, "support-1", ["u1"], "Tanker", station, new StringWriter()));
            Assert.Equal(0, MissionAddFerryCommand.Run(path, 4, "ferry-1", ["u1"], "u1", new StringWriter()));

            using (var writer = new StringWriter())
            {
                Assert.Equal(0, ScenarioValidateCommand.Run(path, quiet: false, writer));
            }

            using (var writer = new StringWriter())
            {
                Assert.Equal(
                    0,
                    ScenarioSimulateSampleCommand.Run(
                        path,
                        ticks: SampleCompleteRecorder.FifteenMinuteSampleTicks,
                        quiet: false,
                        writer));
                var output = writer.ToString();
                Assert.Contains("\"recordType\": \"sample-complete\"", output);
                Assert.Contains("Strike", output);
                Assert.Contains("Patrol", output);
                Assert.Contains("Support", output);
                Assert.Contains("Ferry", output);
                Assert.Contains("worldHash", output, StringComparison.OrdinalIgnoreCase);
            }

            var dto = ScenarioDocumentJsonLoader.LoadFromFile(path);
            Assert.Equal(4, dto.Missions.Count);
            Assert.Contains(dto.Missions, m => m.Type == "Support" && m.SupportRole == "Tanker");
        }
        finally
        {
            DeleteIfExists(path);
            DeleteIfExists(path + ".undo-stack.json");
        }
    }

    private static void DeleteIfExists(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}