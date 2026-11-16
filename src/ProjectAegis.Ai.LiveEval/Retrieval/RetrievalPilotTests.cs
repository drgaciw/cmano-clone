using System.Text.Json;
using Xunit;

namespace ProjectAegis.Ai.LiveEval.Retrieval;

/// <summary>
/// DRG-741 retrieval pilot gates: keyword vs offline embeddings, Track A, revision, embedded data.
/// </summary>
public sealed class RetrievalPilotTests
{
    [Fact]
    public void Pilot_offline_embedding_vs_keyword_reports_Accepted_B_thresholds()
    {
        var report = new RetrievalPilotRunner().Run();

        Assert.True(report.PaidPathAvoided);
        Assert.True(report.CorpusCount >= 40);
        Assert.Equal(42, report.Gate.QueryCount);
        Assert.False(report.Gate.Provisional);
        Assert.True(report.RevisionInvalidated);
        Assert.NotEqual(report.PreviousSourceHash, report.NewSourceHash);
        Assert.True(report.EmbeddedInstructionTreatedAsData);
        Assert.Contains("Track A", report.TrackADenyReason, StringComparison.OrdinalIgnoreCase);

        Assert.True(
            report.Gate.PassesDeltaRecall,
            "ΔRecall@5=" + report.Gate.DeltaRecallAt5.ToString("0.000")
            + " keyword=" + report.Gate.KeywordRecallAt5.ToString("0.000")
            + " embedding=" + report.Gate.EmbeddingRecallAt5.ToString("0.000"));
        Assert.True(
            report.Gate.PassesCitationPrecision,
            "CitationPrecision@5=" + report.Gate.EmbeddingCitationPrecisionAt5.ToString("0.000"));
        Assert.True(report.Gate.Passes, report.Gate.Notes);

        var markdown = RetrievalPilotRunner.FormatMarkdown(report);
        Assert.Contains("offline-embedding", markdown, StringComparison.Ordinal);
        Assert.Contains("ΔRecall@5", markdown, StringComparison.Ordinal);
    }

    [Fact]
    public void TrackA_sample_is_refused_and_corpus_has_no_db3()
    {
        var root = RetrievalFixturePaths.ResolveRoot();
        var reason = RetrievalCorpusLoader.RefuseTrackASample(root);
        Assert.False(string.IsNullOrWhiteSpace(reason));

        var corpus = RetrievalCorpusLoader.LoadCorpus(root);
        Assert.DoesNotContain(corpus, e => e.Id.Contains(".db3", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(corpus, e => e.Text.Contains(".db3", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(corpus, e => e.Track.Equals("A", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Embedded_instruction_document_is_indexed_as_data()
    {
        var root = RetrievalFixturePaths.ResolveRoot();
        var corpus = RetrievalCorpusLoader.LoadCorpus(root);
        var embedded = Assert.Single(corpus, e => e.Id == "doc-embedded-ignore");
        Assert.Equal("embedded-instructions", embedded.SourceClass);
        Assert.Contains("IGNORE PREVIOUS", embedded.Text, StringComparison.OrdinalIgnoreCase);

        var synonyms = RetrievalCorpusLoader.LoadSynonyms(root);
        var embedding = new OfflineEmbeddingRetriever(corpus, synonyms);
        var hits = embedding.Search("document that tries to jailbreak the system prompt", 5);
        Assert.Contains(hits, h => h.SourceId == "doc-embedded-ignore");
    }

    [Fact]
    public void Revision_bump_invalidates_prior_source_hash()
    {
        var root = RetrievalFixturePaths.ResolveRoot();
        var (invalidated, oldHash, newHash) = RevisionAwareIndex.DemonstrateFromFixtures(root);
        Assert.True(invalidated);
        Assert.NotEqual(oldHash, newHash);

        using var bump = JsonDocument.Parse(
            File.ReadAllText(Path.Combine(root, "revision-bump-ucav-blue.json")));
        Assert.Equal(2, bump.RootElement.GetProperty("revision").GetInt32());
        Assert.Equal(
            newHash,
            bump.RootElement.GetProperty("sourceHash").GetString(),
            StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void PaidProviderGate_still_blocks_paid_mode_for_retrieval_pilot()
    {
        Assert.Throws<InvalidOperationException>(
            () => PaidProviderGate.EnsureFakeOrThrow(LiveEvalMode.PaidProvider));
    }
}