using ProjectAegis.Data.Scenario.Authoring;
using NUnit.Framework;

namespace ProjectAegis.MissionEditor.Cli.Tests;

[TestFixture]
public sealed class SampleCompletePipelineTests
{
    /// <summary>
    /// DRG-345: Support/Tanker and Ferry are authored and saved, but the replay backend does not
    /// execute them, so validate/simulate reject until they are removed; Strike+Patrol then completes.
    /// </summary>
    [Test]
    public void strike_patrol_support_ferry_pipeline_rejects_unexecuted_roles_then_emits_sample_complete()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-ac5-{Guid.NewGuid():N}.json");
        try
        {
            using (var writer = new StringWriter())
            {
                Assert.That(ScenarioCreateCommand.Run(path, "baltic_patrol", "baltic-patrol-catalog", 42, writer), Is.EqualTo(0));
            }

            var zone = CliArgParser.ParseWaypoints(["57,20", "57.1,20.1", "57.2,20.2"]);
            var station = CliArgParser.ParseWaypoints(["57.3,20.3", "57.4,20.4", "57.5,20.5"]);

            Assert.That(MissionAddPatrolCommand.Run(path, 1, "patrol-1", ["u1"], zone, new StringWriter()), Is.EqualTo(0));
            Assert.That(MissionAddStrikeCommand.Run(path, 2, "strike-1", ["u1"], ["hostile-1"], new StringWriter()), Is.EqualTo(0));
            Assert.That(MissionAddSupportCommand.Run(path, 3, "support-1", ["u1"], "Tanker", station, new StringWriter()), Is.EqualTo(0));
            Assert.That(MissionAddFerryCommand.Run(path, 4, "ferry-1", ["u1"], "u1", new StringWriter()), Is.EqualTo(0));

            var saved = ScenarioDocumentJsonLoader.LoadFromFile(path);
            Assert.That(saved.Missions.Count, Is.EqualTo(4));
            Assert.That(saved.Missions.Any(m => m.Type == "Support" && m.SupportRole == "Tanker"), Is.True);

            using (var writer = new StringWriter())
            {
                Assert.That(ScenarioValidateCommand.Run(path, quiet: false, writer), Is.EqualTo(1));
                var output = writer.ToString();
                Assert.That(output, Does.Contain(MissionRoleCapabilityManifest.CodeRoleNotExecuted));
                Assert.That(output, Does.Contain("support-1"));
                Assert.That(output, Does.Contain("ferry-1"));
            }

            using (var writer = new StringWriter())
            {
                Assert.That(ScenarioSimulateSampleCommand.Run(
                        path,
                        ticks: SampleCompleteRecorder.FifteenMinuteSampleTicks,
                        quiet: false,
                        writer), Is.EqualTo(1));
                Assert.That(writer.ToString(), Does.Not.Contain("sample-complete"));
            }

            Assert.That(MissionDeleteCommand.Run(path, 5, "support-1", new StringWriter()), Is.EqualTo(0));
            Assert.That(MissionDeleteCommand.Run(path, 6, "ferry-1", new StringWriter()), Is.EqualTo(0));

            using (var writer = new StringWriter())
            {
                Assert.That(ScenarioValidateCommand.Run(path, quiet: false, writer), Is.EqualTo(0));
            }

            using (var writer = new StringWriter())
            {
                Assert.That(ScenarioSimulateSampleCommand.Run(
                        path,
                        ticks: SampleCompleteRecorder.FifteenMinuteSampleTicks,
                        quiet: false,
                        writer), Is.EqualTo(0));
                var output = writer.ToString();
                Assert.That(output, Does.Contain("\"recordType\": \"sample-complete\""));
                Assert.That(output, Does.Contain("Strike"));
                Assert.That(output, Does.Contain("Patrol"));
                Assert.That(output, Does.Contain("worldHash").IgnoreCase);
            }
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