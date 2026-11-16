namespace ProjectAegis.Ai.LiveEval.Retrieval;

/// <summary>
/// Fake/offline embedding pilot: deterministic concept vectors from a synonym map.
/// No paid provider, no network, no embedding API keys.
/// </summary>
public sealed class OfflineEmbeddingRetriever
{
    private readonly IReadOnlyList<(RetrievalIndexEntry Entry, float[] Vector)> _index;
    private readonly IReadOnlyDictionary<string, string> _synonyms;
    private readonly IReadOnlyList<string> _conceptAxis;

    /// <summary>
    /// Builds L2-normalized concept vectors for each corpus entry.
    /// </summary>
    public OfflineEmbeddingRetriever(
        IReadOnlyList<RetrievalIndexEntry> index,
        IReadOnlyDictionary<string, string> synonyms)
    {
        ArgumentNullException.ThrowIfNull(index);
        ArgumentNullException.ThrowIfNull(synonyms);
        _synonyms = synonyms;

        var axis = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var entry in index)
        {
            foreach (var c in entry.Concepts)
            {
                axis.Add(c);
            }
        }

        foreach (var concept in synonyms.Values)
        {
            axis.Add(concept);
        }

        _conceptAxis = axis.ToArray();
        var axisIndex = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var i = 0; i < _conceptAxis.Count; i++)
        {
            axisIndex[_conceptAxis[i]] = i;
        }

        var built = new List<(RetrievalIndexEntry, float[])>(index.Count);
        foreach (var entry in index)
        {
            built.Add((entry, Embed(entry.Title + " " + entry.Text, entry.Concepts, axisIndex)));
        }

        _index = built;
    }

    /// <summary>Stable retriever id for reports.</summary>
    public string RetrieverId => "offline-embedding";

    /// <summary>
    /// Cosine-ranks corpus vectors against the query concept embedding.
    /// </summary>
    public IReadOnlyList<RetrievalHit> Search(string query, int k)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);
        if (k <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(k));
        }

        var axisIndex = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var i = 0; i < _conceptAxis.Count; i++)
        {
            axisIndex[_conceptAxis[i]] = i;
        }

        var qVec = Embed(query, explicitConcepts: null, axisIndex);
        var scored = new List<RetrievalHit>(_index.Count);
        foreach (var (entry, vector) in _index)
        {
            var score = Dot(qVec, vector);
            if (score <= 0)
            {
                continue;
            }

            scored.Add(new RetrievalHit(entry.Id, score));
        }

        return scored
            .OrderByDescending(h => h.Score)
            .ThenBy(h => h.SourceId, StringComparer.Ordinal)
            .Take(k)
            .ToArray();
    }

    private float[] Embed(
        string text,
        IReadOnlyList<string>? explicitConcepts,
        IReadOnlyDictionary<string, int> axisIndex)
    {
        var vec = new float[_conceptAxis.Count];
        if (explicitConcepts is not null)
        {
            foreach (var c in explicitConcepts)
            {
                if (axisIndex.TryGetValue(c, out var idx))
                {
                    // Unique src_* concepts dominate topical overlap.
                    vec[idx] += c.StartsWith("src_", StringComparison.Ordinal) ? 4.0f : 1.5f;
                }
            }
        }

        var tokens = RetrievalText.Tokenize(text);
        for (var i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];
            AddSynonym(token, vec, axisIndex, weight: null);

            // Bigram phrases present in synonym map (e.g. "anti-ship").
            if (i + 1 < tokens.Count)
            {
                AddSynonym(token + "-" + tokens[i + 1], vec, axisIndex, weight: null);
                AddSynonym(token + " " + tokens[i + 1], vec, axisIndex, weight: null);
            }
        }

        Normalize(vec);
        return vec;
    }

    private void AddSynonym(
        string key,
        float[] vec,
        IReadOnlyDictionary<string, int> axisIndex,
        float? weight)
    {
        if (!_synonyms.TryGetValue(key, out var concept)
            || !axisIndex.TryGetValue(concept, out var idx))
        {
            return;
        }

        var w = weight ?? (concept.StartsWith("src_", StringComparison.Ordinal) ? 3.5f : 1.0f);
        vec[idx] += w;
    }

    private static void Normalize(float[] vec)
    {
        double sumSq = 0;
        for (var i = 0; i < vec.Length; i++)
        {
            sumSq += vec[i] * vec[i];
        }

        if (sumSq <= 0)
        {
            return;
        }

        var inv = (float)(1.0 / Math.Sqrt(sumSq));
        for (var i = 0; i < vec.Length; i++)
        {
            vec[i] *= inv;
        }
    }

    private static double Dot(float[] a, float[] b)
    {
        double sum = 0;
        for (var i = 0; i < a.Length; i++)
        {
            sum += a[i] * b[i];
        }

        return sum;
    }
}