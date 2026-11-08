namespace ProjectAegis.Delegation.Projection;

/// <summary>Map legend presentation model for the selected symbology profile (S126-06 W2-SYM-03).</summary>
public sealed record SymbologyLegend(
    SymbologyProfile Profile,
    string ProfileDisplayName,
    string StandardsEdition,
    string NtdsSubset,
    string Disclaimer,
    bool CertificationClaimed,
    string AffiliationExpansionStatus,
    string CivilianProfileStatus,
    IReadOnlyList<string> Deviations,
    IReadOnlyList<SymbologyLegendRow> Rows);

/// <summary>One legend row: glyph and SIDC for a key / affiliation pair.</summary>
public sealed record SymbologyLegendRow(
    string SymbolKey,
    string Affiliation,
    string Glyph,
    string Sidc,
    string Label);
