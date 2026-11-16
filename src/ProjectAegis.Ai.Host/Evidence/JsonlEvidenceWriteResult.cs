namespace ProjectAegis.Ai.Host.Evidence;

/// <summary>Outcome of a JSONL evidence publish attempt.</summary>
/// <param name="Published">True when a JSONL artifact was atomically published.</param>
/// <param name="PublishedPath">Absolute path of the published JSONL file, or null when skipped.</param>
/// <param name="ContentHashSha256">Lowercase hex SHA-256 of published bytes, or null when skipped.</param>
/// <param name="ByteLength">Published file length in bytes (0 when skipped).</param>
/// <param name="PeakWorkingSetBytesDelta">
/// Observational process working-set high-water delta (bytes) sampled across write and validation;
/// not a GC heap delta. May be 0 under OS timing.
/// </param>
/// <param name="SkipReason">Human-readable skip reason when <paramref name="Published"/> is false.</param>
public sealed record JsonlEvidenceWriteResult(
    bool Published,
    string? PublishedPath,
    string? ContentHashSha256,
    long ByteLength,
    long PeakWorkingSetBytesDelta,
    string? SkipReason);
