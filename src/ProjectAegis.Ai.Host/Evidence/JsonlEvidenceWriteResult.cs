namespace ProjectAegis.Ai.Host.Evidence;

/// <summary>Outcome of a JSONL evidence publish attempt.</summary>
public sealed record JsonlEvidenceWriteResult(
    bool Published,
    string? PublishedPath,
    string? ContentHashSha256,
    long ByteLength,
    long PeakWorkingSetBytesDelta,
    string? SkipReason);
