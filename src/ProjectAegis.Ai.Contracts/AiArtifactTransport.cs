namespace ProjectAegis.Ai.Contracts;

/// <summary>
/// Process-boundary transport DTO for Unity / CLI clients (ADR-010 presentation client).
/// Contains only envelope metadata plus opaque content bytes/string — no provider SDK types.
/// </summary>
public sealed record AiArtifactTransport(
    ArtifactEnvelope Envelope,
    string ContentMediaType,
    string? ContentText,
    IReadOnlyList<string>? Warnings);
