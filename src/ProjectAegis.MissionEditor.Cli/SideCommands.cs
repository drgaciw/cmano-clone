namespace ProjectAegis.MissionEditor.Cli;

using ProjectAegis.Data.Scenario.Authoring;

/// <summary>
/// Headless MCP/CLI verb <c>side_list</c> — read-only listing of scenario sides/factions (AME-4.5).
/// </summary>
public static class SideListCommand
{
    /// <summary>
    /// Loads the scenario document and returns sides as JSON. Does not require <c>--edit-version</c>.
    /// </summary>
    public static int Run(string scenarioPath, TextWriter output)
    {
        if (!File.Exists(scenarioPath))
        {
            return McpToolResult.WriteError(output, "NOT_FOUND", $"Scenario not found: {scenarioPath}");
        }

        var document = ScenarioDocumentJsonLoader.LoadFromFile(scenarioPath);
        var sides = document.Sides
            .OrderBy(s => s.Id, StringComparer.Ordinal)
            .Select(s => new
            {
                id = s.Id,
                name = s.Name,
                defaultRoe = s.DefaultRoe,
                defaultEmcon = s.DefaultEmcon,
                postures = s.Postures,
            })
            .ToArray();

        return McpToolResult.WriteOk(output, new
        {
            ok = true,
            sides,
        });
    }
}

/// <summary>
/// Headless MCP/CLI verb <c>side_upsert</c> — insert or replace a side/faction by id (AME-4.5).
/// </summary>
public static class SideUpsertCommand
{
    /// <summary>
    /// Loads the scenario, upserts the side, and persists with edit-version + undo semantics.
    /// </summary>
    public static int Run(
        string scenarioPath,
        int editVersion,
        string sideId,
        string name,
        string? defaultRoe,
        string? defaultEmcon,
        IReadOnlyList<string>? postures,
        TextWriter output)
    {
        if (!File.Exists(scenarioPath))
        {
            return McpToolResult.WriteError(output, "NOT_FOUND", $"Scenario not found: {scenarioPath}");
        }

        if (string.IsNullOrWhiteSpace(sideId))
        {
            return McpToolResult.WriteError(output, "INVALID_SIDE", "A --id is required.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return McpToolResult.WriteError(output, "INVALID_SIDE", "A --name is required.");
        }

        try
        {
            var editor = ScenarioDocumentEditor.Load(scenarioPath);
            editor.RequireEditVersion(editVersion, scenarioPath);
            var undoSnapshot = editor.CaptureUndoSnapshot();
            editor.UpsertSide(new ScenarioSideDto
            {
                Id = sideId,
                Name = name,
                DefaultRoe = defaultRoe,
                DefaultEmcon = defaultEmcon,
                Postures = postures ?? Array.Empty<string>(),
            });
            editor.PersistUndoSnapshot(scenarioPath, undoSnapshot);
            editor.CommitMutation();
            editor.Save(scenarioPath);

            return McpToolResult.WriteOk(output, new
            {
                ok = true,
                sideId,
                name,
                defaultRoe,
                defaultEmcon,
                postureCount = (postures ?? Array.Empty<string>()).Count,
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
            return McpToolResult.WriteError(output, "INVALID_SIDE", ex.Message);
        }
    }
}

/// <summary>
/// Headless MCP/CLI verb <c>side_delete</c> — remove a side by id without cascading ORBAT units.
/// </summary>
public static class SideDeleteCommand
{
    /// <summary>
    /// Loads the scenario, removes the side, and persists with edit-version + undo semantics.
    /// </summary>
    public static int Run(string scenarioPath, int editVersion, string sideId, TextWriter output)
    {
        if (!File.Exists(scenarioPath))
        {
            return McpToolResult.WriteError(output, "NOT_FOUND", $"Scenario not found: {scenarioPath}");
        }

        if (string.IsNullOrWhiteSpace(sideId))
        {
            return McpToolResult.WriteError(output, "INVALID_SIDE", "A --id is required.");
        }

        try
        {
            var editor = ScenarioDocumentEditor.Load(scenarioPath);
            editor.RequireEditVersion(editVersion, scenarioPath);
            var undoSnapshot = editor.CaptureUndoSnapshot();
            if (!editor.TryRemoveSide(sideId))
            {
                return McpToolResult.WriteError(
                    output,
                    "SIDE_NOT_FOUND",
                    $"Side '{sideId}' was not found.");
            }

            editor.PersistUndoSnapshot(scenarioPath, undoSnapshot);
            editor.CommitMutation();
            editor.Save(scenarioPath);
            return McpToolResult.WriteOk(output, new
            {
                ok = true,
                deletedSideId = sideId,
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
