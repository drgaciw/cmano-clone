using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace ProjectAegis.Ai.Host.Aar;

/// <summary>
/// Offline <see cref="IChatClient"/> that returns FIFO scripted assistant text.
/// Used for citation/number reject fixtures; never opens a network connection.
/// </summary>
public sealed class AarScriptedChatClient : IChatClient
{
    private readonly Queue<string> _responses = new();
    private int _disposed;

    /// <summary>Enqueues the next assistant text body.</summary>
    public void EnqueueAssistantText(string text)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(text);
        _responses.Enqueue(text);
    }

    /// <inheritdoc />
    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        _ = chatMessages;
        cancellationToken.ThrowIfCancellationRequested();

        if (_responses.Count == 0)
        {
            throw new InvalidOperationException("AarScriptedChatClient has no scripted responses remaining.");
        }

        var text = _responses.Dequeue();
        var response = new ChatResponse(new ChatMessage(ChatRole.Assistant, text))
        {
            ModelId = options?.ModelId ?? "aar-scripted",
            FinishReason = ChatFinishReason.Stop,
        };
        return Task.FromResult(response);
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var response = await GetResponseAsync(chatMessages, options, cancellationToken).ConfigureAwait(false);
        foreach (var update in response.ToChatResponseUpdates())
        {
            yield return update;
        }
    }

    /// <inheritdoc />
    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        ThrowIfDisposed();
        if (serviceKey is not null)
        {
            return null;
        }

        return serviceType.IsInstanceOfType(this) ? this : null;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Interlocked.Exchange(ref _disposed, 1);
        _responses.Clear();
    }

    private void ThrowIfDisposed()
    {
        if (Volatile.Read(ref _disposed) != 0)
        {
            throw new ObjectDisposedException(nameof(AarScriptedChatClient));
        }
    }
}
