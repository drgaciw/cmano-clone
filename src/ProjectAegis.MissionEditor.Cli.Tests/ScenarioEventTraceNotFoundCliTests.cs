using System.Text.Json;
using NUnit.Framework;
using ProjectAegis.Data.Scenario.Authoring;

namespace ProjectAegis.MissionEditor.Cli.Tests;

/// <summary>UAT CLI-4: scenario_event_trace reports NOT_FOUND for an unknown event id or a missing --path.</summary>
[TestFixture]
public sealed class ScenarioEventTraceNotFoundCliTests
{
    [TestCase("unknown-event", true)]
    [TestCase("evt-1", false)]
    [TestCase("EVT-1", true)]
    public void event_trace_unknown_event_or_missing_path_returns_not_found(string eventId, bool useRealFile)
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-trace-{Guid.NewGuid():N}.json");
        try
        {
            if (useRealFile)
            {
                var editor = ScenarioDocumentEditor.CreateNew();
                editor.UpsertEvent(new ScenarioEventDto { Id = "evt-1", TriggerType = "Time" });
                editor.Save(path);
            }

            using var output = new StringWriter();
            using var error = new StringWriter();
            var code = ScenarioEventTraceCommand.Run(["--path", path, "--event", eventId], output, error);

            Assert.That(code, Is.EqualTo(1));
            using var doc = JsonDocument.Parse(output.ToString());
            Assert.That(doc.RootElement.GetProperty("ok").GetBoolean(), Is.False);
            Assert.That(doc.RootElement.GetProperty("code").GetString(), Is.EqualTo("NOT_FOUND"));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void event_trace_known_event_still_succeeds()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-trace-ok-{Guid.NewGuid():N}.json");
        try
        {
            var editor = ScenarioDocumentEditor.CreateNew();
            editor.UpsertEvent(new ScenarioEventDto { Id = "evt-1", TriggerType = "Time" });
            editor.Save(path);
            using var output = new StringWriter();
            using var error = new StringWriter();
            Assert.That(ScenarioEventTraceCommand.Run(["--path", path, "--event", "evt-1"], output, error), Is.EqualTo(0));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void event_trace_path_without_event_is_invalid_argument()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-trace-noevt-{Guid.NewGuid():N}.json");
        try
        {
            var editor = ScenarioDocumentEditor.CreateNew();
            editor.UpsertEvent(new ScenarioEventDto { Id = "evt-1", TriggerType = "Time" });
            editor.Save(path);
            using var output = new StringWriter();
            using var error = new StringWriter();
            var code = ScenarioEventTraceCommand.Run(["--path", path], output, error);

            Assert.That(code, Is.EqualTo(1));
            using var doc = JsonDocument.Parse(output.ToString());
            Assert.That(doc.RootElement.GetProperty("code").GetString(), Is.EqualTo("INVALID_ARGUMENT"));
            Assert.That(doc.RootElement.GetProperty("message").GetString(), Does.Contain("--event is required"));
        }
        finally
        {
            File.Delete(path);
        }
    }
}
