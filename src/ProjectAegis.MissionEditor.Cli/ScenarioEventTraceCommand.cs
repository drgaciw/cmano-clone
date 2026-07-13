namespace ProjectAegis.MissionEditor.Cli;

using System;
using System.IO;
using System.Text.Json;
using ProjectAegis.Data.Scenario.Authoring;

/// <summary>
/// CLI surface for AC-7 event debugger JSON (S84-01).
/// `scenario_event_trace --path &lt;file&gt; [--event ID]`
/// Delegates to EventDebuggerTrace / ScenarioDocumentEditor.ExplainEventTrace.
/// Produces structured {event_id, fired, last_evaluated_tick, unmet_conditions[]}.
/// </summary>
public static class ScenarioEventTraceCommand
{
    private static readonly JsonSerializerOptions OutputOptions = new()
    {
        WriteIndented = false,
    };

    public static int Run(string[] args, TextWriter output, TextWriter error)
    {
        var path = CliArgParser.GetFlag(args, "--path");
        var eventId = CliArgParser.GetFlag(args, "--event")
            ?? CliArgParser.GetFlag(args, "--id")
            ?? "evt-no-fire";

        ScenarioDocumentEditor editor;
        try
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                editor = ScenarioDocumentEditor.CreateNew();
                if (!string.IsNullOrWhiteSpace(path))
                {
                    error.WriteLine($"warning: path not found, using in-memory default for event trace");
                }
            }
            else
            {
                editor = ScenarioDocumentEditor.Load(path);
            }
        }
        catch (Exception ex)
        {
            error.WriteLine(JsonSerializer.Serialize(new { ok = false, error = "load_failed", detail = ex.Message }, OutputOptions));
            return 2;
        }

        // Ensure event is known for trace (idempotent)
        editor.AddEvent(eventId);

        var json = editor.ExplainEventTrace(eventId);
        output.WriteLine(json);
        return 0;
    }
}
