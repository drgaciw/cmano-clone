namespace ProjectAegis.Data.PlatformAssistant;

/// <summary>
/// Curator brief for a future/hypothetical platform. Output is always a proposal —
/// never a direct catalog write (ADR-011 / CatalogWriteGate).
/// </summary>
public sealed record PlatformDesignBrief(
    string PlatformId,
    string DisplayName,
    string Domain = "surface",
    string RoleWeight = "standard",
    string Concept = "",
    bool WhatIf = true,
    IReadOnlyList<string>? PeerPlatformIds = null);
