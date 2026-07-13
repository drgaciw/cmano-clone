namespace ProjectAegis.MissionEditor.Cli;

using ProjectAegis.Data.Scenario.Authoring;

public static class ScenarioLoadCommand
{
    public static int Run(string sourcePath, string? destPath, TextWriter output)
    {
        if (!File.Exists(sourcePath))
        {
            return McpToolResult.WriteError(output, "NOT_FOUND", $"Scenario not found: {sourcePath}");
        }

        destPath ??= sourcePath;
        ScenarioDocumentDto document;
        if (sourcePath.EndsWith(".aegis-scenario", StringComparison.OrdinalIgnoreCase))
        {
            document = AegisScenarioPackage.Read(sourcePath);
        }
        else
        {
            document = ScenarioDocumentJsonLoader.LoadFromFile(sourcePath);
        }

        ScenarioStableJsonWriter.WriteToFile(document, destPath);
        return McpToolResult.WriteOk(output, new
        {
            ok = true,
            path = destPath,
            editVersion = document.Metadata.EditVersion,
        });
    }
}

public static class ScenarioSaveCommand
{
    public static int Run(string scenarioPath, int editVersion, TextWriter output)
    {
        if (!File.Exists(scenarioPath))
        {
            return McpToolResult.WriteError(output, "NOT_FOUND", $"Scenario not found: {scenarioPath}");
        }

        try
        {
            var editor = ScenarioDocumentEditor.Load(scenarioPath);
            editor.RequireEditVersion(editVersion, scenarioPath);
            editor.Save(scenarioPath);
            return McpToolResult.WriteOk(output, new
            {
                ok = true,
                saved = true,
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
