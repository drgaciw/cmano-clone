using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Microsoft.Extensions.AI;
using ProjectAegis.Ai.Contracts;

namespace ProjectAegis.Ai.Host;

/// <summary>
/// Offline <see cref="IChatClient"/> with a FIFO script of deterministic responses.
/// Never opens a network connection.
/// </summary>
public sealed class FakeChatClient : IChatClient
{
    private readonly Channel<FakeChatScript> _scripts =
        Channel.CreateUnbounded<FakeChatScript>(new UnboundedChannelOptions
        {
            SingleReader = false,
            SingleWriter = false,
        });

    private int _disposed;

    /// <summary>Enqueues the next scripted response (FIFO).</summary>
    public void Enqueue(FakeChatScript script)
    {
        ThrowIfDisposed();
        if (!_scripts.Writer.TryWrite(script))
        {
            throw new InvalidOperationException("Failed to enqueue fake chat script.");
        }
    }

    /// <inheritdoc />
    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        _ = chatMessages;

        if (!_scripts.Reader.TryRead(out var script))
        {
            throw new InvalidOperationException("FakeChatClient has no scripted responses remaining.");
        }

        return await ExecuteAsync(script, options, cancellationToken).ConfigureAwait(false);
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
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        _scripts.Writer.TryComplete();
    }

    /// <summary>Maps a completed fake response to a <see cref="GenerationStatus"/>.</summary>
#pragma warning disable CS8524 // unnamed enum values; keep arms exhaustive for named FakeChatScript (no redundant _)
    public static GenerationStatus ToGenerationStatus(FakeChatScript script) => script switch
    {
        FakeChatScript.Success => GenerationStatus.Succeeded,
        FakeChatScript.MalformedOutput => GenerationStatus.MalformedOutput,
        FakeChatScript.Timeout => GenerationStatus.TimedOut,
        FakeChatScript.Cancellation => GenerationStatus.Cancelled,
        FakeChatScript.ProviderUnavailable => GenerationStatus.ProviderUnavailable,
    };
#pragma warning restore CS8524

    private static async Task<ChatResponse> ExecuteAsync(
        FakeChatScript script,
        ChatOptions? options,
        CancellationToken cancellationToken)
    {
        switch (script)
        {
            case FakeChatScript.Success:
                cancellationToken.ThrowIfCancellationRequested();
                return new ChatResponse(new ChatMessage(ChatRole.Assistant, "{\"ok\":true}"))
                {
                    ModelId = options?.ModelId ?? "fake-success",
                    FinishReason = ChatFinishReason.Stop,
                };

            case FakeChatScript.MalformedOutput:
                cancellationToken.ThrowIfCancellationRequested();
                return new ChatResponse(new ChatMessage(ChatRole.Assistant, "<<<not-json>>>"))
                {
                    ModelId = options?.ModelId ?? "fake-malformed",
                    FinishReason = ChatFinishReason.Stop,
                };

            case FakeChatScript.Timeout:
                cancellationToken.ThrowIfCancellationRequested();
                throw new TimeoutException("FakeChatClient scripted timeout.");

            case FakeChatScript.Cancellation:
                throw new OperationCanceledException("FakeChatClient scripted cancellation.", cancellationToken);

            case FakeChatScript.ProviderUnavailable:
                throw new InvalidOperationException("FakeChatClient scripted provider unavailable.");

            default:
                throw new ArgumentOutOfRangeException(nameof(script), script, "Unknown fake script.");
        }
    }

    private void ThrowIfDisposed()
    {
        if (Volatile.Read(ref _disposed) != 0)
        {
            throw new ObjectDisposedException(nameof(FakeChatClient));
        }
    }
}
