namespace ProjectAegis.Data.Catalog;

/// <summary>Req-21 Phase B: platform signature profile (sorted by platform_id).</summary>
public sealed record CatalogSignature(
    string PlatformId,
    double RcsBandDbsm = 0,
    double IrSignature = 0,
    double AcousticSignatureDb = 0,
    double MagneticSignature = 0,
    string ReviewState = CatalogReviewStates.Provisional,
    int TrlLevel = 9,
    string ValueTier = CatalogProvenanceTier.GameplayAbstraction,
    string CitationRef = "");