namespace ProjectAegis.Delegation.Hindsight;

public sealed class NullHindsightMemoryClient : IHindsightMemoryClient
{
    public static NullHindsightMemoryClient Instance { get; } = new();

    public void RetainFireAndForget(string bankId, string content, string? context = null)
    {
    }

    public Task RetainAsync(string bankId, string content, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task<string?> ReflectAsync(
        string bankId,
        string query,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<string?>(null);
}
