namespace ProjectAegis.Ai.Contracts;

/// <summary>Complete side-filtered evidence document for host consumption (schema v1).</summary>
public sealed record EvidenceDocument(
    EvidenceHeader Header,
    IReadOnlyList<EvidenceEntry> Entries);
