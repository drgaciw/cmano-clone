using System.Globalization;
using System.Text.Json;
using NUnit.Framework;

namespace ProjectAegis.Ai.LiveEval.Retrieval;

/// <summary>
/// DRG-741 retrieval pilot gates: keyword vs offline embeddings, Track A, revision, embedded data.
/// </summary>
public sealed class RetrievalPilotTests
{
    [Test]
    public void Pilot_offline_embedding_vs_keyword_reports_Accepted_B_thresholds()
    {
        var report = new RetrievalPilotRunner().Run();

        Assert.That(report.PaidPathAvoided, Is.True);
        Assert.That(report.CorpusCount, Is.GreaterThanOrEqualTo(40));
        Assert.That(report.Gate.QueryCount, Is.EqualTo(42));
        Assert.That(report.Gate.Provisional, Is.False);
        Assert.That(report.RevisionInvalidated, Is.True);
        Assert.That(report.NewSourceHash, Is.Not.EqualTo(report.PreviousSourceHash));
        Assert.That(report.EmbeddedInstructionTreatedAsData, Is.True);
        Assert.That(report.TrackADenyReason, Does.Contain("Track A").IgnoreCase);

        Assert.That(
            report.Gate.PassesDeltaRecall,
            Is.True,
            "ΔRecall@5=" + report.Gate.DeltaRecallAt5.ToString("0.000", CultureInfo.InvariantCulture)
            + " keyword=" + report.Gate.KeywordRecallAt5.ToString("0.000", CultureInfo.InvariantCulture)
            + " embedding=" + report.Gate.EmbeddingRecallAt5.ToString("0.000", CultureInfo.InvariantCulture));
        Assert.That(
            report.Gate.PassesCitationPrecision,
            Is.True,
            "CitationPrecision@5=" + report.Gate.EmbeddingCitationPrecisionAt5.ToString("0.000", CultureInfo.InvariantCulture));
        Assert.That(report.Gate.Passes, Is.True, report.Gate.Notes);

        var markdown = RetrievalPilotRunner.FormatMarkdown(report);
        Assert.That(markdown, Does.Contain("offline-embedding"));
        Assert.That(markdown, Does.Contain("ΔRecall@5"));
    }

    [Test]
    public void TrackA_sample_is_refused_and_corpus_has_no_db3()
    {
        var root = RetrievalFixturePaths.ResolveRoot();
        var reason = RetrievalCorpusLoader.RefuseTrackASample(root);
        Assert.That(string.IsNullOrWhiteSpace(reason), Is.False);

        var corpus = RetrievalCorpusLoader.LoadCorpus(root);
        Assert.That(
            corpus.Any(e => e.Id.Contains(".db3", StringComparison.OrdinalIgnoreCase)),
            Is.False);
        Assert.That(
            corpus.Any(e => e.Text.Contains(".db3", StringComparison.OrdinalIgnoreCase)),
            Is.False);
        Assert.That(
            corpus.Any(e => e.Track.Equals("A", StringComparison.OrdinalIgnoreCase)),
            Is.False);
    }

    [Test]
    public void Embedded_instruction_document_is_indexed_as_data()
    {
        var root = RetrievalFixturePaths.ResolveRoot();
        var corpus = RetrievalCorpusLoader.LoadCorpus(root);
        var embedded = corpus.Single(e => string.Equals(e.Id, "doc-embedded-ignore", StringComparison.Ordinal));
        Assert.That(embedded.SourceClass, Is.EqualTo("embedded-instructions"));
        Assert.That(embedded.Text, Does.Contain("IGNORE PREVIOUS").IgnoreCase);

        var synonyms = RetrievalCorpusLoader.LoadSynonyms(root);
        var embedding = new OfflineEmbeddingRetriever(corpus, synonyms);
        var hits = embedding.Search("document that tries to jailbreak the system prompt", 5);
        Assert.That(
            hits.Any(h => string.Equals(h.SourceId, "doc-embedded-ignore", StringComparison.Ordinal)),
            Is.True);
    }

    [Test]
    public void Revision_bump_invalidates_prior_source_hash()
    {
        var root = RetrievalFixturePaths.ResolveRoot();
        var (invalidated, oldHash, newHash) = RevisionAwareIndex.DemonstrateFromFixtures(root);
        Assert.That(invalidated, Is.True);
        Assert.That(newHash, Is.Not.EqualTo(oldHash));

        using var bump = JsonDocument.Parse(
            File.ReadAllText(Path.Combine(root, "revision-bump-ucav-blue.json")));
        Assert.That(bump.RootElement.GetProperty("revision").GetInt32(), Is.EqualTo(2));
        Assert.That(
            bump.RootElement.GetProperty("sourceHash").GetString(),
            Is.EqualTo(newHash).IgnoreCase);
    }

    [Test]
    public void PaidProviderGate_still_blocks_paid_mode_for_retrieval_pilot()
    {
        Assert.Throws<InvalidOperationException>(
            () => PaidProviderGate.EnsureFakeOrThrow(LiveEvalMode.PaidProvider));
    }
}