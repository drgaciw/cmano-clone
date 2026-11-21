namespace ProjectAegis.MissionEditor.Cli;

using System;
using System.IO;
using System.Text.Json;
using Data.Scenario.Authoring;

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

        if (!string.IsNullOrWhiteSpace(path) && !File.Exists(path))
        {
            return McpToolResult.WriteError(output, "NOT_FOUND", $"Scenario not found: {path}");
        }

        ScenarioDocumentEditor editor;
        var loadedFromFile = !string.IsNullOrWhiteSpace(path);
        try
        {
            editor = loadedFromFile ? ScenarioDocumentEditor.Load(path!) : ScenarioDocumentEditor.CreateNew();
        }
        catch (Exception ex)
        {
            error.WriteLine(JsonSerializer.Serialize(new { ok = false, error = "load_failed", detail = ex.Message }, OutputOptions));
            return 2;
        }

        if (loadedFromFile)
        {
            if (!editor.Events.Any(e => string.Equals(e.Id, eventId, StringComparison.OrdinalIgnoreCase)))
            {
                return McpToolResult.WriteError(output, "NOT_FOUND", $"Event '{eventId}' was not found in {path}.");
            }
        }
        else
        {
            // No --path: in-memory demo mode, unchanged (event is created on the fly).
            editor.AddEvent(eventId);
        }

        var json = editor.ExplainEventTrace(eventId);
        output.WriteLine(json);
        return 0;
    }
}
