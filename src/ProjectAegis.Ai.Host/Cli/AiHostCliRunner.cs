using System.Text.Json;
using ProjectAegis.Ai.Contracts;

namespace ProjectAegis.Ai.Host.Cli;

/// <summary>
/// Stdin/stdout JSON IPC runner for the standalone <c>ProjectAegis.Ai.Host</c> process (DRG-772 / ADR-030).
/// </summary>
public static class AiHostCliRunner
{
    private static readonly JsonSerializerOptions IpcJsonOptions = new()
    {
        WriteIndented = false,
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>
    /// Dispatches one IPC verb from <paramref name="args"/> using JSON on <paramref name="input"/>
    /// and writes a single-line JSON <see cref="AiHostIpcResponse"/> to <paramref name="output"/>.
    /// </summary>
    public static async Task<int> RunAsync(
        string[] args,
        TextReader input,
        TextWriter output,
        TextWriter error,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);

        if (args.Length == 0 || string.IsNullOrWhiteSpace(args[0]))
        {
            await error.WriteLineAsync("ProjectAegis.Ai.Host requires a verb: aar_explain | mission_plan_suggest").ConfigureAwait(false);
            return 2;
        }

        var verb = args[0];
        var rawJson = await input.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(rawJson))
        {
            await error.WriteLineAsync($"ProjectAegis.Ai.Host verb '{verb}' requires a JSON request on stdin.").ConfigureAwait(false);
            return 2;
        }

        try
        {
            AiHostIpcResponse response;
            switch (verb)
            {
                case AiHostIpcVerbs.AarExplain:
                {
                    var req = JsonSerializer.Deserialize<AiHostAarExplainRequest>(rawJson, IpcJsonOptions)
                        ?? throw new JsonException("Deserialized AiHostAarExplainRequest was null.");
                    response = await AarExplainHostHandler.ExecuteAsync(req, cancellationToken).ConfigureAwait(false);
                    break;
                }

                case AiHostIpcVerbs.MissionPlanSuggest:
                {
                    var req = JsonSerializer.Deserialize<AiHostMissionPlanSuggestRequest>(rawJson, IpcJsonOptions)
                        ?? throw new JsonException("Deserialized AiHostMissionPlanSuggestRequest was null.");
                    response = MissionPlanSuggestHostHandler.Execute(req);
                    break;
                }

                default:
                    await error.WriteLineAsync($"Unknown ProjectAegis.Ai.Host verb: {verb}").ConfigureAwait(false);
                    return 2;
            }

            await output.WriteLineAsync(JsonSerializer.Serialize(response, IpcJsonOptions)).ConfigureAwait(false);
            return 0;
        }
        catch (JsonException ex)
        {
            await error.WriteLineAsync($"Invalid IPC JSON for '{verb}': {ex.Message}").ConfigureAwait(false);
            return 2;
        }
    }
}
