namespace ProjectAegis.Ai.Contracts;

/// <summary>Host generation outcome for an AI artifact. Explicit failure statuses are required.</summary>
public enum GenerationStatus
{
    Succeeded = 0,
    TimedOut = 1,
    Cancelled = 2,
    MalformedOutput = 3,
    ProviderUnavailable = 4,
    RejectedCitation = 5,
    RejectedNumber = 6,
}
