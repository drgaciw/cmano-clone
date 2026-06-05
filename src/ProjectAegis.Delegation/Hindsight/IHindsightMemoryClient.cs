namespace ProjectAegis.Delegation.Hindsight;

/// <summary>Sidecar memory API (retain/recall/reflect). Never called from deterministic tick replay paths.</summary>
public interface IHindsightMemoryClient
{
    void RetainFireAndForget(string bankId, string content, string? context = null);

    Task RetainAsync(string bankId, string content, CancellationToken cancellationToken = default);

    Task<string?> ReflectAsync(
        string bankId,
        string query,
        CancellationToken cancellationToken = default);
}
