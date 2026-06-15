namespace ProjectAegis.MissionEditor.Cli;

using ProjectAegis.Data.Scenario.Authoring;

public static class ReferencePointSetCommand
{
    public static int Run(
        string scenarioPath,
        int editVersion,
        string pointId,
        string geometryType,
        IReadOnlyList<ScenarioWaypointDto> geometry,
        double? radiusNm,
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
            editor.SetReferencePoint(new ScenarioReferencePointDto
            {
                Id = pointId,
                Type = geometryType,
                Geometry = geometry,
                RadiusNm = radiusNm,
            });
            editor.CommitMutation();
            editor.Save(scenarioPath);

            return McpToolResult.WriteOk(output, new
            {
                ok = true,
                pointId,
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
