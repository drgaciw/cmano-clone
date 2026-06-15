namespace ProjectAegis.MissionEditor.Cli;

using ProjectAegis.Data.Scenario.Authoring;

public static class EventAddCommand
{
    public static int Run(
        string scenarioPath,
        int editVersion,
        string eventId,
        int priority,
        string triggerType,
        int? atTick,
        TextWriter output)
    {
        if (!File.Exists(scenarioPath))
        {
            return McpToolResult.WriteError(output, "NOT_FOUND", $"Scenario not found: {scenarioPath}");
        }

        try
        {
            var editor = ScenarioDocumentEditor.Load(scenarioPath);
            editor.RequireEditVersion(editVersion, scenarioPath);
            if (editor.Events.Any(e => string.Equals(e.Id, eventId, StringComparison.OrdinalIgnoreCase)))
            {
                return McpToolResult.WriteError(output, "DUPLICATE_EVENT", $"Event id '{eventId}' already exists.");
            }

            editor.AddEvent(eventId);
            editor.Events.Add(new ScenarioEventDto
            {
                Id = eventId,
                TriggerType = triggerType,
            });
            editor.CommitMutation();
            editor.Save(scenarioPath);

            var fireOrder = editor.Events
                .OrderBy(e => e.Id, StringComparer.OrdinalIgnoreCase)
                .Select(e => e.Id)
                .ToArray();

            return McpToolResult.WriteOk(output, new
            {
                ok = true,
                eventId,
                atTick,
                priority,
                fireOrder,
                editVersion = editor.Metadata.EditVersion,
            });
        }
        catch (ScenarioEditConflictException ex)
        {
            return McpToolResult.WriteError(
                output,
                ex.Code,
                ex.Message,
                new { currentEditVersion = ex.CurrentEditVersion, fileHash = ex.FileHash });
        }
        catch (InvalidOperationException ex)
        {
            return McpToolResult.WriteError(output, "DUPLICATE_EVENT", ex.Message);
        }
    }
}

public static class EventValidateCommand
{
    public static int Run(string scenarioPath, string eventId, TextWriter output)
    {
        if (!File.Exists(scenarioPath))
        {
            return McpToolResult.WriteError(output, "NOT_FOUND", $"Scenario not found: {scenarioPath}");
        }

        var document = ScenarioDocumentJsonLoader.LoadFromFile(scenarioPath);
        var evt = document.Events?.FirstOrDefault(e =>
            string.Equals(e.Id, eventId, StringComparison.OrdinalIgnoreCase));
        if (evt == null)
        {
            return McpToolResult.WriteError(output, "EVENT_NOT_FOUND", $"Event '{eventId}' not found.");
        }

        var projection = EventDebuggerTrace.ToJson(document, eventId);
        output.WriteLine(projection);
        return 0;
    }
}
