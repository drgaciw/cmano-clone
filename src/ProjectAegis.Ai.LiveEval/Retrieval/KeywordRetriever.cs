namespace ProjectAegis.Ai.LiveEval.Retrieval;

/// <summary>
/// Existing-style keyword / token-overlap lookup (baseline for DRG-741).
/// </summary>
public sealed class KeywordRetriever
{
    private readonly IReadOnlyList<RetrievalIndexEntry> _index;

    /// <summary>
    /// Creates a keyword retriever over an already-gated corpus.
    /// </summary>
    public KeywordRetriever(IReadOnlyList<RetrievalIndexEntry> index)
    {
        _index = index ?? throw new ArgumentNullException(nameof(index));
    }

    /// <summary>Stable retriever id for reports.</summary>
    public string RetrieverId => "keyword";

    /// <summary>
    /// Ranks corpus entries by query-token overlap against title+text.
    /// </summary>
    public IReadOnlyList<RetrievalHit> Search(string query, int k)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);
        if (k <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(k));
        }

        var qTokens = RetrievalText.Tokenize(query);
        if (qTokens.Count == 0)
        {
            return Array.Empty<RetrievalHit>();
        }

        var qSet = new HashSet<string>(qTokens, StringComparer.Ordinal);
        var scored = new List<RetrievalHit>(_index.Count);
        foreach (var entry in _index)
        {
            var docTokens = RetrievalText.Tokenize(entry.Title + " " + entry.Text);
            if (docTokens.Count == 0)
            {
                continue;
            }

            var overlap = 0;
            foreach (var t in docTokens)
            {
                if (qSet.Contains(t))
                {
                    overlap++;
                }
            }

            if (overlap == 0)
            {
                continue;
            }

            // Jaccard-ish: overlap / (|q| + |doc| - overlap) favors exact phrase docs.
            var union = qSet.Count + docTokens.Count - overlap;
            var score = union <= 0 ? 0.0 : (double)overlap / union;
            scored.Add(new RetrievalHit(entry.Id, score));
        }

        return scored
            .OrderByDescending(h => h.Score)
            .ThenBy(h => h.SourceId, StringComparer.Ordinal)
            .Take(k)
            .ToArray();
    }
}