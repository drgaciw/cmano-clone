namespace ProjectAegis.Delegation.Projection;

/// <summary>Resolved APP-6 map glyph with unicode fallback and USS atlas frame id (ADR-007 Phase C).</summary>
public sealed record App6MapGlyphResolution(
    string UnicodeGlyph,
    string UssFrameId,
    string Sidc);