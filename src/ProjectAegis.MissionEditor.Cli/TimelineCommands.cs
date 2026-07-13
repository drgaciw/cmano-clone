namespace ProjectAegis.MissionEditor.Cli;

using ProjectAegis.Data.Scenario.Authoring;

/// <summary>
/// Headless MCP/CLI verb <c>timeline_list</c> — read-only operations timeline listing (AME-3.5 Partial+).
/// </summary>
public static class TimelineListCommand
{
    /// <summary>
    /// Loads the scenario document and returns operations-timeline entries as JSON.
    /// Does not require <c>--edit-version</c> (read-only).
    /// </summary>
    public static int Run(string scenarioPath, TextWriter output)
    {
        if (!File.Exists(scenarioPath))
        {
            return McpToolResult.WriteError(output, "NOT_FOUND", $"Scenario not found: {scenarioPath}");
        }

        var document = ScenarioDocumentJsonLoader.LoadFromFile(scenarioPath);
        var entries = document.OperationsTimeline
            .OrderBy(e => e.ActivateAtTick)
            .ThenBy(e => e.MissionId, StringComparer.Ordinal)
            .Select(e => new
            {
                missionId = e.MissionId,
                activateAtTick = e.ActivateAtTick,
            })
            .ToArray();

        return McpToolResult.WriteOk(output, new
        {
            ok = true,
            entries,
            count = entries.Length,
        });
    }
}

/// <summary>
/// Headless MCP/CLI verb <c>timeline_upsert</c> — insert or replace a timeline entry by mission id.
/// </summary>
public static class TimelineUpsertCommand
{
    /// <summary>
    /// Loads the scenario, upserts the timeline entry, and persists with edit-version + undo semantics.
    /// </summary>
    public static int Run(
        string scenarioPath,
        int editVersion,
        string missionId,
        int activateAtTick,
        TextWriter output)
    {
        if (!File.Exists(scenarioPath))
        {
            return McpToolResult.WriteError(output, "NOT_FOUND", $"Scenario not found: {scenarioPath}");
        }

        if (string.IsNullOrWhiteSpace(missionId))
        {
            return McpToolResult.WriteError(output, "INVALID_TIMELINE", "A --mission is required.");
        }

        if (activateAtTick < 0)
        {
            return McpToolResult.WriteError(
                output,
                "INVALID_TICK",
                "--tick must be a non-negative integer.");
        }

        try
        {
            var editor = ScenarioDocumentEditor.Load(scenarioPath);
            editor.RequireEditVersion(editVersion, scenarioPath);
            var undoSnapshot = editor.CaptureUndoSnapshot();
            editor.UpsertTimelineEntry(new ScenarioOperationTimelineEntryDto
            {
                MissionId = missionId,
                ActivateAtTick = activateAtTick,
            });
            editor.PersistUndoSnapshot(scenarioPath, undoSnapshot);
            editor.CommitMutation();
            editor.Save(scenarioPath);

            return McpToolResult.WriteOk(output, new
            {
                ok = true,
                missionId,
                activateAtTick,
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
            return McpToolResult.WriteError(output, "INVALID_TIMELINE", ex.Message);
        }
    }
}

/// <summary>
/// Headless MCP/CLI verb <c>timeline_delete</c> — remove a timeline entry by mission id.
/// </summary>
public static class TimelineDeleteCommand
{
    /// <summary>
    /// Loads the scenario, removes the timeline entry, and persists with edit-version + undo semantics.
    /// </summary>
    public static int Run(string scenarioPath, int editVersion, string missionId, TextWriter output)
    {
        if (!File.Exists(scenarioPath))
        {
            return McpToolResult.WriteError(output, "NOT_FOUND", $"Scenario not found: {scenarioPath}");
        }

        if (string.IsNullOrWhiteSpace(missionId))
        {
            return McpToolResult.WriteError(output, "INVALID_TIMELINE", "A --mission is required.");
        }

        try
        {
            var editor = ScenarioDocumentEditor.Load(scenarioPath);
            editor.RequireEditVersion(editVersion, scenarioPath);
            var undoSnapshot = editor.CaptureUndoSnapshot();
            if (!editor.TryRemoveTimelineEntry(missionId))
            {
                return McpToolResult.WriteError(
                    output,
                    "TIMELINE_NOT_FOUND",
                    $"Timeline entry for mission '{missionId}' was not found.");
            }

            editor.PersistUndoSnapshot(scenarioPath, undoSnapshot);
            editor.CommitMutation();
            editor.Save(scenarioPath);
            return McpToolResult.WriteOk(output, new
            {
                ok = true,
                deletedMissionId = missionId,
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
