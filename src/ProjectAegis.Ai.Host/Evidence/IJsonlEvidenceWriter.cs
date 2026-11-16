using ProjectAegis.Ai.Contracts;

namespace ProjectAegis.Ai.Host.Evidence;

/// <summary>
/// Streams <see cref="EvidenceDocument"/> as JSON Lines (header + entries) with atomic publish.
/// Does not mutate order logs or call <c>OnScenarioFinalized</c>.
/// </summary>
public interface IJsonlEvidenceWriter
{
    /// <summary>
    /// Writes JSONL under <paramref name="artifactRoot"/> using temp + validate + rename.
    /// When <paramref name="enabled"/> is false, returns a skipped result and leaves no file.
    /// </summary>
    Task<JsonlEvidenceWriteResult> WriteAsync(
        EvidenceDocument document,
        string artifactRoot,
        string fileName,
        bool enabled = true,
        int? maxEvidenceBytes = null,
        CancellationToken cancellationToken = default);
}
