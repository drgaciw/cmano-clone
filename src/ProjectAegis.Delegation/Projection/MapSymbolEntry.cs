namespace ProjectAegis.Delegation.Projection;

using ProjectAegis.Sim.Engage;

/// <summary>Tactical map symbol for C2 placeholder (doc 20 / ADR-007).</summary>
public sealed record MapSymbolEntry(
    string SymbolId,
    string Affiliation,
    string ShapeGlyph,
    string Label,
    float NormalizedX,
    float NormalizedY,
    bool IsDestroyed,
    string? App6Sidc = null,
    string? App6UssFrameId = null,
    CombatDomain? Domain = null);