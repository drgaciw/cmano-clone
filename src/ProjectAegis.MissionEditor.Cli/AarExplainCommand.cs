using ProjectAegis.Ai.Contracts;

namespace ProjectAegis.MissionEditor.Cli;

/// <summary>
/// DRG-744 / CMA-15 / DRG-772: thin <c>aar_explain</c> CLI verb delegating across a process boundary
/// to the standalone <c>ProjectAegis.Ai.Host</c> executable (ADR-030).
/// Fake clients only — never opens a paid provider.
/// </summary>
/// <remarks>
/// Exit codes: <c>0</c> structured result written (including non-Succeeded generation statuses);
/// <c>1</c> usage / missing evidence / IO / host failure.
/// Off by default (<c>--enabled</c> required to invoke the fake client).
/// </remarks>
public static class AarExplainCommand
{
    /// <summary>
    /// Runs AAR explain against an exported JSONL evidence file via the out-of-process AI host.
    /// </summary>
    public static Task<int> RunAsync(
        string? evidencePath,
        bool enabled,
        string? artifactRoot,
        string? scriptPath,
        TextWriter output) =>
        RunAsync(evidencePath, enabled, artifactRoot, scriptPath, output, AiHostProcessClient.Default);

    /// <summary>
    /// Runs AAR explain using an explicit <see cref="AiHostProcessClient"/> (for testing host-boundary behavior).
    /// </summary>
    public static async Task<int> RunAsync(
        string? evidencePath,
        bool enabled,
        string? artifactRoot,
        string? scriptPath,
        TextWriter output,
        AiHostProcessClient hostClient)
    {
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(hostClient);

        if (string.IsNullOrWhiteSpace(evidencePath))
        {
            return McpToolResult.WriteError(
                output,
                "MISSING_EVIDENCE",
                "aar_explain requires --evidence <evidence.jsonl>");
        }

        if (!File.Exists(evidencePath))
        {
            return McpToolResult.WriteError(output, "NOT_FOUND", "Evidence file not found: " + evidencePath);
        }

        var request = new AiHostAarExplainRequest(
            EvidencePath: evidencePath,
            Enabled: enabled,
            ArtifactRoot: artifactRoot,
            ScriptPath: scriptPath);

        var response = await hostClient
            .InvokeAsync(AiHostIpcVerbs.AarExplain, request)
            .ConfigureAwait(false);

        output.WriteLine(response.PayloadJson);
        return response.ExitCode;
    }
}
