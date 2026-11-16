namespace ProjectAegis.Ai.LiveEval.Retrieval;

/// <summary>
/// Indexed retrieval source with revision-aware hash metadata (DRG-741 pilot).
/// </summary>
public sealed record RetrievalIndexEntry(
    string Id,
    string Title,
    string Text,
    int Revision,
    string SourceHash,
    IReadOnlyList<string> Concepts,
    string SourceClass,
    string Track);

/// <summary>
/// One query with expected citation sources.
/// </summary>
public sealed record RetrievalQuery(
    string Id,
    string Text,
    IReadOnlyList<string> ExpectedSourceIds,
    bool Paraphrase);

/// <summary>
/// Ranked hit from a retriever.
/// </summary>
public sealed record RetrievalHit(string SourceId, double Score);

/// <summary>
/// Aggregate metrics for a retriever over a query set.
/// </summary>
public sealed record RetrievalMetricSummary(
    string RetrieverId,
    int QueryCount,
    double RecallAt5,
    double CitationPrecisionAt5,
    IReadOnlyList<RetrievalQueryMetric> PerQuery);

/// <summary>
/// Per-query metric row.
/// </summary>
public sealed record RetrievalQueryMetric(
    string QueryId,
    double RecallAt5,
    double CitationPrecisionAt5,
    IReadOnlyList<string> TopIds);

/// <summary>
/// Decision-gate outcome for DRG-729 Accepted B Balanced.
/// </summary>
public sealed record RetrievalPilotGateResult(
    int QueryCount,
    bool Provisional,
    double KeywordRecallAt5,
    double EmbeddingRecallAt5,
    double DeltaRecallAt5,
    double EmbeddingCitationPrecisionAt5,
    bool PassesDeltaRecall,
    bool PassesCitationPrecision,
    bool Passes,
    string Notes);