namespace ProjectAegis.Ai.Host.Catalog;

/// <summary>Evidence → PlatformDesignAssistant brief → write-gate Propose overloads.</summary>
public interface ICatalogEvidenceProposePipeline
{
    /// <summary>Validate evidence, build brief, draft, and stage via evidence-aware Propose overloads.</summary>
    CatalogEvidenceProposeResult Propose(CatalogEvidenceProposeRequest request);
}
