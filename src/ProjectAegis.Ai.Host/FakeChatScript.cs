namespace ProjectAegis.Ai.Host;

/// <summary>Scripted offline response kinds for <see cref="FakeChatClient"/>.</summary>
public enum FakeChatScript
{
    Success = 0,
    MalformedOutput = 1,
    Timeout = 2,
    Cancellation = 3,
    ProviderUnavailable = 4,
}
