using ProjectAegis.Ai.Contracts;

namespace ProjectAegis.Ai.Host.Evidence;

/// <summary>
/// Publishes an <see cref="ArtifactEnvelope"/> whose <see cref="ArtifactEnvelope.ContentHash"/>
/// matches a completed JSONL write (atomic via existing <see cref="AtomicArtifactStore"/>).
/// </summary>
public static class JsonlEvidenceEnvelopePublisher
{
    /// <summary>
    /// Writes envelope JSON through <see cref="AtomicArtifactStore.PublishAsync"/> using the
    /// JSONL content hash from <paramref name="writeResult"/>.
    /// </summary>
    public static Task PublishAsync(
        AtomicArtifactStore store,
        string envelopeFileName,
        EvidenceDocument document,
        JsonlEvidenceWriteResult writeResult,
        string providerId = "none",
        string modelId = "none",
        string promptVersion = "evidence-jsonl-1",
        GenerationStatus status = GenerationStatus.Succeeded,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentException.ThrowIfNullOrWhiteSpace(envelopeFileName);
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(writeResult);
        if (!writeResult.Published || string.IsNullOrWhiteSpace(writeResult.ContentHashSha256))
        {
            throw new InvalidOperationException(
                "Envelope publish requires a successful JSONL write with ContentHashSha256.");
        }

        var envelope = new ArtifactEnvelope(
            SchemaVersion: AiContractSchema.Version,
            BuildIdentity: document.Header.BuildIdentity,
            ScenarioId: document.Header.ScenarioId,
            CatalogRevision: document.Header.CatalogRevision,
            OrderLogFingerprint: document.Header.OrderLogFingerprint,
            ProviderId: providerId,
            ModelId: modelId,
            PromptVersion: promptVersion,
            EvidenceIds: document.Header.EvidenceIds,
            ViewingSide: document.Header.ViewingSide,
            DebriefMode: document.Header.DebriefMode,
            Status: status,
            ContentHash: writeResult.ContentHashSha256);

        return store.PublishAsync(envelopeFileName, envelope, cancellationToken);
    }
}
