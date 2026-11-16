using ProjectAegis.Data.PlatformAssistant;
using ProjectAegis.Data.WriteGate;

namespace ProjectAegis.Ai.Host.Catalog;

/// <summary>Outcome of evidence-fed platform design propose (never auto-approved).</summary>
public sealed record CatalogEvidenceProposeResult(
    CatalogEvidenceOutcome Outcome,
    IReadOnlyList<CatalogEvidenceDiagnostic> Diagnostics,
    PlatformDesignBrief? Brief,
    PlatformDesignProposal? Proposal,
    string? PlatformBatchId,
    string? DamageBatchId,
    string? MobilityBatchId,
    IReadOnlyList<CatalogEvidenceCitation> Citations,
    bool SubmitAllowedWithoutHuman = false);
