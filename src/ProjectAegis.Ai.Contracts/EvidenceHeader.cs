namespace ProjectAegis.Ai.Contracts;

/// <summary>Evidence export header (schema v1). Property order is part of the freeze contract.</summary>
public sealed record EvidenceHeader(
    int SchemaVersion,
    string BuildIdentity,
    string ScenarioId,
    string CatalogRevision,
    string OrderLogFingerprint,
    ViewingSide ViewingSide,
    bool DebriefMode,
    IReadOnlyList<string> EvidenceIds);
