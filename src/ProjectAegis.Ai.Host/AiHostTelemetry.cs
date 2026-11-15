namespace ProjectAegis.Ai.Host;

using ProjectAegis.Ai.Contracts;

/// <summary>
/// Default host telemetry. Never records secrets or full prompt bodies.
/// </summary>
public sealed class AiHostTelemetry
{
    private readonly List<string> _events = new();

    /// <summary>In-memory event lines for tests (no PII / no prompts).</summary>
    public IReadOnlyList<string> Events => _events;

    /// <summary>Records a generation outcome without prompt or secret payloads.</summary>
    public void RecordGeneration(string providerId, string modelId, GenerationStatus status)
    {
        _events.Add($"generation provider={providerId} model={modelId} status={status}");
    }

    /// <summary>Records an artifact publish without content bodies.</summary>
    public void RecordPublish(string fileName, GenerationStatus status)
    {
        _events.Add($"publish file={fileName} status={status}");
    }
}
