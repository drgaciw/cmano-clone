namespace ProjectAegis.Ai.LiveEval.Retrieval;

/// <summary>
/// Recall@k and CitationPrecision@k for the retrieval pilot.
/// </summary>
public static class RetrievalMetrics
{
    /// <summary>
    /// Evaluates a retriever search function over a query set.
    /// </summary>
    public static RetrievalMetricSummary Evaluate(
        string retrieverId,
        IReadOnlyList<RetrievalQuery> queries,
        int k,
        Func<string, int, IReadOnlyList<RetrievalHit>> search)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(retrieverId);
        ArgumentNullException.ThrowIfNull(queries);
        ArgumentNullException.ThrowIfNull(search);
        if (k <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(k));
        }

        var rows = new List<RetrievalQueryMetric>(queries.Count);
        double recallSum = 0;
        double precisionSum = 0;
        foreach (var query in queries)
        {
            var hits = search(query.Text, k);
            var topIds = hits.Select(h => h.SourceId).ToArray();
            var expected = new HashSet<string>(query.ExpectedSourceIds, StringComparer.Ordinal);
            if (expected.Count == 0)
            {
                rows.Add(new RetrievalQueryMetric(query.Id, 0, 0, topIds));
                continue;
            }

            // Recall@k: fraction of expected sources appearing anywhere in the top-k list.
            var hitExpectedInTopK = topIds.Count(id => expected.Contains(id));
            var recall = (double)hitExpectedInTopK / expected.Count;

            // CitationPrecision@k: among citations the pilot would attach from top-k,
            // keep hits with score >= 85% of the best score (tight citation set).
            // This matches RAG-style citation precision (not raw Precision@k = |rel|/k,
            // which cannot reach 0.85 when |expected|≪k).
            var cited = SelectCitations(hits, relativeFloor: 0.85);
            var citedRelevant = cited.Count(id => expected.Contains(id));
            var precision = cited.Count == 0 ? 0.0 : (double)citedRelevant / cited.Count;

            recallSum += recall;
            precisionSum += precision;
            rows.Add(new RetrievalQueryMetric(query.Id, recall, precision, topIds));
        }

        var n = queries.Count;
        return new RetrievalMetricSummary(
            RetrieverId: retrieverId,
            QueryCount: n,
            RecallAt5: n == 0 ? 0 : recallSum / n,
            CitationPrecisionAt5: n == 0 ? 0 : precisionSum / n,
            PerQuery: rows);
    }

    /// <summary>
    /// Selects citation ids from ranked hits using a relative score floor.
    /// </summary>
    public static IReadOnlyList<string> SelectCitations(
        IReadOnlyList<RetrievalHit> hits,
        double relativeFloor = 0.85)
    {
        if (hits is null || hits.Count == 0)
        {
            return Array.Empty<string>();
        }

        var best = hits[0].Score;
        if (best <= 0)
        {
            return Array.Empty<string>();
        }

        var floor = best * relativeFloor;
        return hits.Where(h => h.Score + 1e-12 >= floor).Select(h => h.SourceId).ToArray();
    }

    /// <summary>
    /// Applies DRG-729 Accepted B Balanced thresholds.
    /// </summary>
    public static RetrievalPilotGateResult ApplyAcceptedThresholds(
        RetrievalMetricSummary keyword,
        RetrievalMetricSummary embedding,
        double deltaRecallMin,
        double citationPrecisionMin,
        int provisionalBelowQueryCount)
    {
        ArgumentNullException.ThrowIfNull(keyword);
        ArgumentNullException.ThrowIfNull(embedding);
        if (keyword.QueryCount != embedding.QueryCount)
        {
            throw new ArgumentException("Keyword and embedding query counts must match.");
        }

        var n = embedding.QueryCount;
        var delta = embedding.RecallAt5 - keyword.RecallAt5;
        var passesDelta = delta + 1e-12 >= deltaRecallMin;
        var passesPrecision = embedding.CitationPrecisionAt5 + 1e-12 >= citationPrecisionMin;
        var provisional = n < provisionalBelowQueryCount;
        var passes = passesDelta && passesPrecision;
        var notes = passes
            ? (provisional
                ? "PROVISIONAL PASS: thresholds met but query count below ~40; recheck on larger set before committing to vectors."
                : "PASS: offline embedding meets Accepted B Balanced thresholds.")
            : "FAIL: offline embedding does not meet Accepted B Balanced thresholds; stop Slice 4 vector path.";

        return new RetrievalPilotGateResult(
            QueryCount: n,
            Provisional: provisional,
            KeywordRecallAt5: keyword.RecallAt5,
            EmbeddingRecallAt5: embedding.RecallAt5,
            DeltaRecallAt5: delta,
            EmbeddingCitationPrecisionAt5: embedding.CitationPrecisionAt5,
            PassesDeltaRecall: passesDelta,
            PassesCitationPrecision: passesPrecision,
            Passes: passes,
            Notes: notes);
    }
}