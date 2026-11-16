using System.Text.Json;

namespace ProjectAegis.Ai.LiveEval.Retrieval;

/// <summary>
/// Demonstrates revision-aware invalidation: a changed source hash drops the stale entry.
/// </summary>
public sealed class RevisionAwareIndex
{
    private readonly Dictionary<string, RetrievalIndexEntry> _byId =
        new(StringComparer.Ordinal);

    /// <summary>
    /// Upserts an entry; returns whether a prior revision was invalidated.
    /// </summary>
    public bool Upsert(RetrievalIndexEntry entry, out string? invalidatedHash)
    {
        ArgumentNullException.ThrowIfNull(entry);
        invalidatedHash = null;
        if (_byId.TryGetValue(entry.Id, out var existing))
        {
            if (string.Equals(existing.SourceHash, entry.SourceHash, StringComparison.OrdinalIgnoreCase)
                && existing.Revision == entry.Revision)
            {
                return false;
            }

            invalidatedHash = existing.SourceHash;
            _byId[entry.Id] = entry;
            return true;
        }

        _byId[entry.Id] = entry;
        return false;
    }

    /// <summary>
    /// Returns the current entry for an id, if present.
    /// </summary>
    public bool TryGet(string id, out RetrievalIndexEntry? entry)
        => _byId.TryGetValue(id, out entry);

    /// <summary>
    /// Loads the revision-bump fixture and shows stale hash invalidation against the corpus entry.
    /// </summary>
    public static (bool Invalidated, string OldHash, string NewHash) DemonstrateFromFixtures(string fixtureRoot)
    {
        var corpus = RetrievalCorpusLoader.LoadCorpus(fixtureRoot);
        var baseline = corpus.First(e => e.Id == "doc-ucav-blue");
        var index = new RevisionAwareIndex();
        index.Upsert(baseline, out _);

        var bumpPath = Path.Combine(fixtureRoot, "revision-bump-ucav-blue.json");
        using var doc = JsonDocument.Parse(File.ReadAllText(bumpPath));
        if (!RetrievalCorpusLoader.TryIndexDocument(doc.RootElement, out var bumped, out var deny)
            || bumped is null)
        {
            throw new InvalidOperationException("revision bump fixture refused: " + deny);
        }

        var invalidated = index.Upsert(bumped, out var oldHash);
        if (!invalidated || oldHash is null)
        {
            throw new InvalidOperationException("Expected revision bump to invalidate prior hash.");
        }

        if (!index.TryGet(bumped.Id, out var current) || current is null)
        {
            throw new InvalidOperationException("Missing entry after upsert.");
        }

        return (true, oldHash, current.SourceHash);
    }
}