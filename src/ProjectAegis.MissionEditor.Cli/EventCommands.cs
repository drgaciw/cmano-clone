namespace ProjectAegis.MissionEditor.Cli;

using ProjectAegis.Data.Scenario.Authoring;

/// <summary>
/// Headless MCP/CLI verb <c>event_add</c> / <c>event_update</c> — upsert a scenario event
/// (ME-W2 / AME-5.x event graph CRUD).
/// </summary>
public static class EventAddCommand
{
    /// <summary>
    /// Loads the scenario, upserts the event (insert or replace by id), and persists with
    /// edit-version + undo semantics.
    /// </summary>
    public static int Run(
        string scenarioPath,
        int editVersion,
        string eventId,
        string triggerType,
        IReadOnlyList<ScenarioEventConditionDto> conditions,
        IReadOnlyList<ScenarioEventActionDto> actions,
        TextWriter output)
    {
        if (!File.Exists(scenarioPath))
        {
            return McpToolResult.WriteError(output, "NOT_FOUND", $"Scenario not found: {scenarioPath}");
        }

        if (string.IsNullOrWhiteSpace(eventId))
        {
            return McpToolResult.WriteError(output, "INVALID_EVENT", "A --id is required.");
        }

        if (string.IsNullOrWhiteSpace(triggerType))
        {
            return McpToolResult.WriteError(output, "INVALID_TRIGGER", "A --trigger is required.");
        }

        try
        {
            var editor = ScenarioDocumentEditor.Load(scenarioPath);
            editor.RequireEditVersion(editVersion, scenarioPath);
            var undoSnapshot = editor.CaptureUndoSnapshot();
            editor.UpsertEvent(new ScenarioEventDto
            {
                Id = eventId,
                TriggerType = triggerType,
                Conditions = conditions ?? Array.Empty<ScenarioEventConditionDto>(),
                Actions = actions ?? Array.Empty<ScenarioEventActionDto>(),
            });
            editor.PersistUndoSnapshot(scenarioPath, undoSnapshot);
            editor.CommitMutation();
            editor.Save(scenarioPath);

            var fireOrder = EventFireOrderCalculator.ComputeFireOrder(editor.Events);

            return McpToolResult.WriteOk(output, new
            {
                ok = true,
                eventId,
                triggerType,
                conditionCount = editor.Events
                    .First(e => string.Equals(e.Id, eventId, StringComparison.OrdinalIgnoreCase))
                    .Conditions.Count,
                actionCount = editor.Events
                    .First(e => string.Equals(e.Id, eventId, StringComparison.OrdinalIgnoreCase))
                    .Actions.Count,
                fireOrder,
                editVersion = editor.Metadata.EditVersion,
                fileHash = editor.ComputeFileHash(),
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
            return McpToolResult.WriteError(output, "INVALID_EVENT", ex.Message);
        }
    }

    /// <summary>
    /// Parses <c>--condition</c> tokens: <c>Type</c>, <c>Type:UnitId:ZoneId</c>, or
    /// <c>Type,UnitId,ZoneId</c>.
    /// </summary>
    public static ScenarioEventConditionDto ParseCondition(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            throw new FormatException("Condition token is empty.");
        }

        var parts = SplitToken(raw, maxParts: 3);
        return new ScenarioEventConditionDto
        {
            Type = parts[0],
            UnitId = parts.Length > 1 && parts[1].Length > 0 ? parts[1] : null,
            ZoneId = parts.Length > 2 && parts[2].Length > 0 ? parts[2] : null,
        };
    }

    /// <summary>
    /// Parses <c>--action</c> tokens: <c>Type</c> or <c>Type:UnitId</c> / <c>Type,UnitId</c>.
    /// </summary>
    public static ScenarioEventActionDto ParseAction(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            throw new FormatException("Action token is empty.");
        }

        var parts = SplitToken(raw, maxParts: 2);
        return new ScenarioEventActionDto
        {
            Type = parts[0],
            UnitId = parts.Length > 1 && parts[1].Length > 0 ? parts[1] : null,
        };
    }

    private static string[] SplitToken(string raw, int maxParts)
    {
        var separator = raw.Contains(':', StringComparison.Ordinal) ? ':' : ',';
        return raw.Split(separator, maxParts, StringSplitOptions.TrimEntries);
    }
}

/// <summary>
/// Headless MCP/CLI verb <c>event_delete</c> — remove a scenario event by id.
/// </summary>
public static class EventDeleteCommand
{
    /// <summary>
    /// Loads the scenario, removes the event, and persists with edit-version + undo semantics.
    /// </summary>
    public static int Run(string scenarioPath, int editVersion, string eventId, TextWriter output)
    {
        if (!File.Exists(scenarioPath))
        {
            return McpToolResult.WriteError(output, "NOT_FOUND", $"Scenario not found: {scenarioPath}");
        }

        if (string.IsNullOrWhiteSpace(eventId))
        {
            return McpToolResult.WriteError(output, "INVALID_EVENT", "A --id is required.");
        }

        try
        {
            var editor = ScenarioDocumentEditor.Load(scenarioPath);
            editor.RequireEditVersion(editVersion, scenarioPath);
            var undoSnapshot = editor.CaptureUndoSnapshot();
            if (!editor.TryRemoveEvent(eventId))
            {
                return McpToolResult.WriteError(
                    output,
                    "EVENT_NOT_FOUND",
                    $"Event '{eventId}' was not found.");
            }

            editor.PersistUndoSnapshot(scenarioPath, undoSnapshot);
            editor.CommitMutation();
            editor.Save(scenarioPath);
            return McpToolResult.WriteOk(output, new
            {
                ok = true,
                deletedEventId = eventId,
                editVersion = editor.Metadata.EditVersion,
                fileHash = editor.ComputeFileHash(),
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
    }
}

/// <summary>Read-only event validation / debugger projection (unchanged AC-7 path).</summary>
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
