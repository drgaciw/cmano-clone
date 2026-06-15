namespace ProjectAegis.MissionEditor.Cli.Tests;

using ProjectAegis.Data.Scenario.Authoring;
using ProjectAegis.MissionEditor.Cli;
using Xunit;

public sealed class MissionEditorV1CliTests
{
    [Fact]
    public void Four_mission_headless_sample_pipeline_ac5()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ac5-{Guid.NewGuid():N}.json");
        try
        {
            ScenarioCreateCommand.Run(path, "baltic_patrol", "baltic-patrol-catalog", 42, new StringWriter());
            var zone = CliArgParser.ParseWaypoints(["57,20", "57.1,20.1", "57.2,20.2"]);
            var station = CliArgParser.ParseWaypoints(["57.5,20.5"]);

            MissionAddPatrolCommand.Run(path, 1, "patrol-1", ["u1"], zone, new StringWriter());
            MissionAddStrikeCommand.Run(path, 2, "strike-1", ["u1"], ["hostile-1"], new StringWriter());
            MissionAddSupportCommand.Run(path, 3, "support-1", ["u2"], "Tanker", station, new StringWriter());
            MissionAddFerryCommand.Run(path, 4, "ferry-1", ["u3"], "base-alpha", new StringWriter());

            using var validateWriter = new StringWriter();
            Assert.Equal(0, ScenarioValidateCommand.Run(path, quiet: false, validateWriter));

            using var sampleWriter = new StringWriter();
            Assert.Equal(0, ScenarioSimulateSampleCommand.Run(path, ticks: 8, quiet: false, sampleWriter));
            var sampleJson = sampleWriter.ToString();
            Assert.Contains("sampleComplete", sampleJson, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("fireOrder", sampleJson, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Fact]
    public void Event_add_and_validate_round_trip_ac7()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ac7-{Guid.NewGuid():N}.json");
        try
        {
            ScenarioDocumentEditor.CreateNew().Save(path);
            EventAddCommand.Run(path, 1, "evt-zone", 100, "Time", 0, new StringWriter());

            using var writer = new StringWriter();
            Assert.Equal(0, EventValidateCommand.Run(path, "evt-zone", writer));
            Assert.Contains("\"fired\": false", writer.ToString());
            Assert.Contains("evt-zone", writer.ToString());
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
