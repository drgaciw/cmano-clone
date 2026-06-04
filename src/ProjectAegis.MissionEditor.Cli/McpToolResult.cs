namespace ProjectAegis.MissionEditor.Cli;

using System.Text.Json;
using ProjectAegis.Data.Scenario.Authoring;

internal static class McpToolResult
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public static int WriteOk(TextWriter output, object payload)
    {
        output.WriteLine(JsonSerializer.Serialize(payload, Options));
        return 0;
    }

    public static int WriteError(TextWriter output, string code, string message, object? extra = null)
    {
        var payload = new Dictionary<string, object?>
        {
            ["ok"] = false,
            ["code"] = code,
            ["message"] = message,
        };
        if (extra != null)
        {
            payload["details"] = extra;
        }

        output.WriteLine(JsonSerializer.Serialize(payload, Options));
        return code == ScenarioEditVersionGuard.ConflictCode ? 3 : 1;
    }
}