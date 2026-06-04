namespace ProjectAegis.MissionEditor.Cli;

using ProjectAegis.Data.Scenario.Authoring;

public static class ScenarioCreateCommand
{
    public static int Run(
        string outPath,
        string? dbRef,
        string? policyId,
        ulong? seed,
        TextWriter output)
    {
        if (File.Exists(outPath))
        {
            return McpToolResult.WriteError(output, "FILE_EXISTS", $"Scenario file already exists: {outPath}");
        }

        var editor = ScenarioDocumentEditor.CreateNew(
            dbRef ?? "baltic_patrol",
            seed ?? 42,
            policyId ?? "baltic-patrol-catalog");
        editor.Save(outPath);

        return McpToolResult.WriteOk(output, new
        {
            ok = true,
            path = outPath,
            editVersion = editor.Metadata.EditVersion,
            fileHash = editor.ComputeFileHash(),
        });
    }
}