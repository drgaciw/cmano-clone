namespace ProjectAegis.Ai.Contracts;

/// <summary>
/// One evidence row projected from an order-log entry. <see cref="EvidenceId"/> uses
/// <c>{fingerprint-prefix}:{SequenceId}</c>. Text fields are untrusted data.
/// </summary>
public sealed record EvidenceEntry(
    string EvidenceId,
    ulong SequenceId,
    EvidenceEntryKind Kind,
    double SimTime,
    ViewingSide ViewingSide,
    string? UnitId,
    string? Summary,
    IReadOnlyDictionary<string, string>? Attributes);
