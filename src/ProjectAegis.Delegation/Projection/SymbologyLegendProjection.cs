namespace ProjectAegis.Delegation.Projection;

/// <summary>Builds the deterministic map legend, including the non-certification disclaimer.</summary>
public static class SymbologyLegendProjection
{
    /// <summary>Disclaimer shown on every selectable profile legend (S126-06 W2-SYM-03).</summary>
    public const string NotCertifiedDisclaimer =
        "Symbology subset — not certified against NATO APP-6 or MIL-STD-2525. Presentation only.";

    /// <summary>W3-SYM-03 freeze: affiliation expansion stays on hold.</summary>
    public const string AffiliationExpansionStatus =
        "Affiliation expansion HOLD (W2-SYM-05): Friendly / Hostile / Neutral / Unknown only.";

    /// <summary>CIV profile status until the DRG-232 scope decision is recorded.</summary>
    public const string CivilianProfileStatus = "Civilian-friendly profile pending scope decision (DRG-232).";

    private static readonly string[] LegacyAffiliations = ["Friendly", "Hostile", "Neutral", "Suspect", "Pending"];

    public static SymbologyLegend Build(SymbologyProfile profile)
    {
        if (!SymbologyProfileCatalog.IsSelectable(profile))
        {
            throw new ArgumentException(SymbologyProfileCatalog.CivilianPendingReason, nameof(profile));
        }

        return profile == SymbologyProfile.MilitaryTactical ? BuildMilitary() : BuildLegacy();
    }

    private static SymbologyLegend BuildMilitary()
    {
        var rows = new List<SymbologyLegendRow>();
        foreach (var definition in SymbolKeyRegistry.All)
        {
            foreach (var affiliation in MilitarySymbology.SupportedAffiliations)
            {
                var glyph = MilitarySymbology.Resolve(affiliation, definition.Key);
                rows.Add(new SymbologyLegendRow(
                    definition.Key,
                    affiliation,
                    glyph.UnicodeGlyph,
                    glyph.Sidc,
                    $"{affiliation} {definition.Category} [{glyph.IconText}]"));
            }
        }

        return new SymbologyLegend(
            SymbologyProfile.MilitaryTactical,
            SymbologyProfileCatalog.DisplayName(SymbologyProfile.MilitaryTactical),
            $"{MilitarySymbology.App6Edition} / {MilitarySymbology.MilStd2525Edition} (15-character SIDC subset)",
            MilitarySymbology.NtdsSubset,
            NotCertifiedDisclaimer,
            CertificationClaimed: false,
            AffiliationExpansionStatus,
            CivilianProfileStatus,
            MilitarySymbology.Deviations,
            rows);
    }

    private static SymbologyLegend BuildLegacy()
    {
        var rows = new List<SymbologyLegendRow>(LegacyAffiliations.Length + 1);
        foreach (var affiliation in LegacyAffiliations)
        {
            var resolution = App6Sidc.ResolveMapGlyph(affiliation);
            rows.Add(new SymbologyLegendRow(
                SymbolKeyRegistry.GenericUnknown,
                affiliation,
                resolution.UnicodeGlyph,
                resolution.Sidc,
                $"{affiliation} unit"));
        }

        rows.Add(new SymbologyLegendRow(
            SymbolKeyRegistry.GenericUnknown,
            "Unknown",
            App6Sidc.FallbackGlyph,
            App6Sidc.FallbackSidc,
            "Unknown fallback"));

        return new SymbologyLegend(
            SymbologyProfile.Legacy,
            SymbologyProfileCatalog.DisplayName(SymbologyProfile.Legacy),
            "APP-6 / MIL-STD-2525C affiliation placeholder (ADR-007 Phase C)",
            "None",
            NotCertifiedDisclaimer,
            CertificationClaimed: false,
            AffiliationExpansionStatus,
            CivilianProfileStatus,
            Array.Empty<string>(),
            rows);
    }
}
