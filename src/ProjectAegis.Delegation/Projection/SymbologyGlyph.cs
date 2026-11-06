namespace ProjectAegis.Delegation.Projection;

/// <summary>Resolved profile glyph for one map symbol (presentation-only).</summary>
public sealed record SymbologyGlyph(
    string SymbolKey,
    string Affiliation,
    string UnicodeGlyph,
    string UssFrameId,
    string Sidc,
    string IconText);
