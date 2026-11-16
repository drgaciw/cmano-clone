using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ProjectAegis.Ai.LiveEval.Retrieval;

/// <summary>
/// Loads public retrieval fixtures, refusing Track A / <c>*.db3</c> sources before index.
/// </summary>
public static class RetrievalCorpusLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    /// <summary>
    /// Loads corpus documents that pass the Track A gate.
    /// </summary>
    public static IReadOnlyList<RetrievalIndexEntry> LoadCorpus(string fixtureRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fixtureRoot);
        var corpusDir = Path.Combine(fixtureRoot, "corpus");
        if (!Directory.Exists(corpusDir))
        {
            throw new DirectoryNotFoundException("Missing corpus directory: " + corpusDir);
        }

        var entries = new List<RetrievalIndexEntry>();
        foreach (var path in Directory.EnumerateFiles(corpusDir, "*.json").OrderBy(p => p, StringComparer.Ordinal))
        {
            var raw = File.ReadAllText(path);
            using var doc = JsonDocument.Parse(raw);
            if (!TryMaterialize(doc.RootElement, out var entry, out var denyReason) || entry is null)
            {
                throw new InvalidOperationException(
                    "Corpus file refused or invalid (" + path + "): " + denyReason);
            }

            entries.Add(entry);
        }

        return entries;
    }

    /// <summary>
    /// Attempts to index a single JSON document; Track A / db3 metadata is refused.
    /// </summary>
    public static bool TryIndexDocument(
        JsonElement root,
        out RetrievalIndexEntry? entry,
        out string denyReason)
        => TryMaterialize(root, out entry, out denyReason);

    /// <summary>
    /// Loads the refuse-sample document and asserts it is denied.
    /// </summary>
    public static string RefuseTrackASample(string fixtureRoot)
    {
        var path = Path.Combine(fixtureRoot, "track-a-refuse.json");
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        if (TryMaterialize(doc.RootElement, out _, out var denyReason))
        {
            throw new InvalidOperationException("Track A sample was incorrectly accepted for indexing.");
        }

        return denyReason;
    }

    /// <summary>
    /// Loads queries.json.
    /// </summary>
    public static (IReadOnlyList<RetrievalQuery> Queries, int K, double DeltaRecall, double CitationPrecision, int ProvisionalBelow)
        LoadQueries(string fixtureRoot)
    {
        var path = Path.Combine(fixtureRoot, "queries.json");
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        var root = doc.RootElement;
        var k = root.TryGetProperty("k", out var kEl) ? kEl.GetInt32() : 5;
        var thresholds = root.GetProperty("thresholds");
        var delta = thresholds.GetProperty("deltaRecallAt5").GetDouble();
        var precision = thresholds.GetProperty("citationPrecisionAt5").GetDouble();
        var provisionalBelow = thresholds.GetProperty("provisionalBelowQueryCount").GetInt32();

        var queries = new List<RetrievalQuery>();
        foreach (var q in root.GetProperty("queries").EnumerateArray())
        {
            var expected = q.GetProperty("expectedSourceIds")
                .EnumerateArray()
                .Select(e => e.GetString() ?? string.Empty)
                .Where(s => s.Length > 0)
                .ToArray();
            queries.Add(new RetrievalQuery(
                Id: q.GetProperty("id").GetString() ?? string.Empty,
                Text: q.GetProperty("text").GetString() ?? string.Empty,
                ExpectedSourceIds: expected,
                Paraphrase: q.TryGetProperty("paraphrase", out var p) && p.GetBoolean()));
        }

        return (queries, k, delta, precision, provisionalBelow);
    }

    /// <summary>
    /// Loads synonym → concept map for offline embeddings.
    /// </summary>
    public static IReadOnlyDictionary<string, string> LoadSynonyms(string fixtureRoot)
    {
        var path = Path.Combine(fixtureRoot, "synonyms.json");
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var prop in doc.RootElement.GetProperty("map").EnumerateObject())
        {
            map[prop.Name] = prop.Value.GetString() ?? prop.Name;
        }

        return map;
    }

    /// <summary>
    /// Computes the canonical source hash used by the pilot index.
    /// </summary>
    public static string ComputeSourceHash(string text, int revision)
    {
        var bytes = Encoding.UTF8.GetBytes(revision.ToString(System.Globalization.CultureInfo.InvariantCulture) + "\n" + text);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static bool TryMaterialize(
        JsonElement root,
        out RetrievalIndexEntry? entry,
        out string denyReason)
    {
        entry = null;
        denyReason = string.Empty;

        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (root.TryGetProperty("metadata", out var meta) && meta.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in meta.EnumerateObject())
            {
                metadata[prop.Name] = prop.Value.ValueKind == JsonValueKind.String
                    ? prop.Value.GetString() ?? string.Empty
                    : prop.Value.ToString();
            }
        }

        var track = root.TryGetProperty("track", out var trackEl)
            ? trackEl.GetString() ?? "B"
            : "B";
        metadata["track"] = track;

        var id = root.GetProperty("id").GetString() ?? string.Empty;
        var title = root.GetProperty("title").GetString() ?? string.Empty;
        var text = root.GetProperty("text").GetString() ?? string.Empty;
        metadata["id"] = id;
        metadata["title"] = title;
        metadata["text"] = text;

        if (root.TryGetProperty("sourceClass", out var sc))
        {
            metadata["sourceClass"] = sc.GetString() ?? string.Empty;
        }

        if (!TrackASourceGate.TryAllowPublicSource(metadata, out denyReason))
        {
            return false;
        }

        if (string.Equals(track, "A", StringComparison.OrdinalIgnoreCase))
        {
            denyReason = "Track A tagged source refused for retrieval index: " + id;
            return false;
        }

        if (text.Contains(".db3", StringComparison.OrdinalIgnoreCase)
            || id.Contains(".db3", StringComparison.OrdinalIgnoreCase))
        {
            denyReason = "db3 path signal refused for retrieval index: " + id;
            return false;
        }

        var revision = root.GetProperty("revision").GetInt32();
        var sourceHash = root.GetProperty("sourceHash").GetString() ?? string.Empty;
        var expectedHash = ComputeSourceHash(text, revision);
        if (!string.Equals(sourceHash, expectedHash, StringComparison.OrdinalIgnoreCase))
        {
            denyReason = "sourceHash mismatch for " + id;
            return false;
        }

        var concepts = root.GetProperty("concepts")
            .EnumerateArray()
            .Select(e => e.GetString() ?? string.Empty)
            .Where(s => s.Length > 0)
            .ToArray();

        entry = new RetrievalIndexEntry(
            Id: id,
            Title: title,
            Text: text,
            Revision: revision,
            SourceHash: sourceHash.ToLowerInvariant(),
            Concepts: concepts,
            SourceClass: root.GetProperty("sourceClass").GetString() ?? string.Empty,
            Track: track);
        return true;
    }
}