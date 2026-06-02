namespace ProjectAegis.Delegation.Projection;

public sealed record MapPanelState(
    string TheaterLabel,
    IReadOnlyList<MapSymbolDisplayRow> Symbols);

public sealed record MapSymbolDisplayRow(
    string SymbolId,
    string Glyph,
    string Label,
    float NormalizedX,
    float NormalizedY,
    string StyleClass);