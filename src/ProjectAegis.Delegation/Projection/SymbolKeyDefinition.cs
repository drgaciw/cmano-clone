namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// Canonical symbol key row (S126-03 W2-SYM-01; DRG-231 MIL-AC-02 manifest fields).
/// </summary>
/// <param name="Key">Stable presentation-only key, e.g. <c>naval.surface.combatant</c>.</param>
/// <param name="Domain">Semantic domain (<c>naval</c> or <c>generic</c>).</param>
/// <param name="Category">Platform category known to the observer.</param>
/// <param name="BattleDimension">MIL-STD-2525C SIDC position 3 (S surface, U subsurface, Z unknown).</param>
/// <param name="FunctionId">MIL-STD-2525C SIDC positions 5–10.</param>
/// <param name="IconText">Text icon drawn inside the frame.</param>
/// <param name="FallbackKey">Key used when this entry cannot be rendered.</param>
/// <param name="SourceReference">Standard edition / section the mapping is taken from.</param>
public sealed record SymbolKeyDefinition(
    string Key,
    string Domain,
    string Category,
    char BattleDimension,
    string FunctionId,
    string IconText,
    string FallbackKey,
    string SourceReference);
