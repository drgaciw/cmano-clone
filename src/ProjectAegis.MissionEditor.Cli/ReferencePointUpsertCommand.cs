namespace ProjectAegis.MissionEditor.Cli;

using ProjectAegis.Data.Scenario.Authoring;

/// <summary>
/// Headless MCP/CLI verb <c>reference_point_upsert</c> — place or replace a reference point.
/// </summary>
public static class ReferencePointUpsertCommand
{
    /// <summary>
    /// Loads the scenario, upserts the reference point, and persists with edit-version + undo semantics.
    /// </summary>
    public static int Run(
        string scenarioPath,
        int editVersion,
        string referencePointId,
        string type,
        IReadOnlyList<ScenarioWaypointDto> geometry,
        double? radiusNm,
        TextWriter output)
    {
        if (!File.Exists(scenarioPath))
        {
            return McpToolResult.WriteError(output, "NOT_FOUND", $"Scenario not found: {scenarioPath}");
        }

        if (string.IsNullOrWhiteSpace(referencePointId))
        {
            return McpToolResult.WriteError(output, "INVALID_REFERENCE_POINT", "A --id is required.");
        }

        if (string.IsNullOrWhiteSpace(type))
        {
            return McpToolResult.WriteError(output, "INVALID_TYPE", "A --type is required.");
        }

        if (geometry.Count == 0)
        {
            return McpToolResult.WriteError(
                output,
                "INVALID_GEOMETRY",
                "At least one --latlon lat,lon is required.");
        }

        try
        {
            var editor = ScenarioDocumentEditor.Load(scenarioPath);
            editor.RequireEditVersion(editVersion, scenarioPath);
            var undoSnapshot = editor.CaptureUndoSnapshot();
            editor.UpsertReferencePoint(new ScenarioReferencePointDto
            {
                Id = referencePointId,
                Type = type,
                Geometry = geometry,
                RadiusNm = radiusNm,
            });
            editor.PersistUndoSnapshot(scenarioPath, undoSnapshot);
            editor.CommitMutation();
            editor.Save(scenarioPath);

            return McpToolResult.WriteOk(output, new
            {
                ok = true,
                referencePointId,
                type,
                vertexCount = geometry.Count,
                radiusNm,
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
            return McpToolResult.WriteError(output, "INVALID_REFERENCE_POINT", ex.Message);
        }
    }
}
