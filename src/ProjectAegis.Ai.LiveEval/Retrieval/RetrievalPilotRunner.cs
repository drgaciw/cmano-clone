using System.Globalization;
using System.Text;

namespace ProjectAegis.Ai.LiveEval.Retrieval;

/// <summary>
/// End-to-end DRG-741 pilot: keyword vs offline embeddings on one fixture query set.
/// </summary>
public sealed class RetrievalPilotRunner
{
    /// <summary>
    /// Runs the pilot against fixtures under <paramref name="fixtureRoot"/>.
    /// </summary>
    public RetrievalPilotReport Run(string? fixtureRoot = null)
    {
        var root = fixtureRoot ?? RetrievalFixturePaths.ResolveRoot();
        PaidProviderGate.EnsureFakeOrThrow(LiveEvalMode.Fake);

        var trackADeny = RetrievalCorpusLoader.RefuseTrackASample(root);
        var corpus = RetrievalCorpusLoader.LoadCorpus(root);
        var synonyms = RetrievalCorpusLoader.LoadSynonyms(root);
        var (queries, k, deltaMin, precisionMin, provisionalBelow) =
            RetrievalCorpusLoader.LoadQueries(root);

        var keyword = new KeywordRetriever(corpus);
        var embedding = new OfflineEmbeddingRetriever(corpus, synonyms);

        var keywordSummary = RetrievalMetrics.Evaluate(
            keyword.RetrieverId,
            queries,
            k,
            keyword.Search);
        var embeddingSummary = RetrievalMetrics.Evaluate(
            embedding.RetrieverId,
            queries,
            k,
            embedding.Search);

        var gate = RetrievalMetrics.ApplyAcceptedThresholds(
            keywordSummary,
            embeddingSummary,
            deltaMin,
            precisionMin,
            provisionalBelow);

        var revision = RevisionAwareIndex.DemonstrateFromFixtures(root);

        // Embedded-instruction doc must remain retrievable as data (not executed).
        var embeddedHits = embedding.Search("jailbreak the system prompt", k);
        var embeddedAsData = embeddedHits.Any(h => h.SourceId == "doc-embedded-ignore");

        return new RetrievalPilotReport(
            FixtureRoot: root,
            CorpusCount: corpus.Count,
            TrackADenyReason: trackADeny,
            Keyword: keywordSummary,
            Embedding: embeddingSummary,
            Gate: gate,
            RevisionInvalidated: revision.Invalidated,
            PreviousSourceHash: revision.OldHash,
            NewSourceHash: revision.NewHash,
            EmbeddedInstructionTreatedAsData: embeddedAsData,
            PaidPathAvoided: true);
    }

    /// <summary>
    /// Formats a markdown table suitable for PR / Linear comments.
    /// </summary>
    public static string FormatMarkdown(RetrievalPilotReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        var sb = new StringBuilder();
        sb.AppendLine("## DRG-741 retrieval pilot results");
        sb.AppendLine();
        sb.AppendLine("| Retriever | Recall@5 | CitationPrecision@5 | N |");
        sb.AppendLine("| --- | ---: | ---: | ---: |");
        sb.AppendLine(
            "| keyword | "
            + F(report.Keyword.RecallAt5)
            + " | "
            + F(report.Keyword.CitationPrecisionAt5)
            + " | "
            + report.Gate.QueryCount
            + " |");
        sb.AppendLine(
            "| offline-embedding | "
            + F(report.Embedding.RecallAt5)
            + " | "
            + F(report.Embedding.CitationPrecisionAt5)
            + " | "
            + report.Gate.QueryCount
            + " |");
        sb.AppendLine();
        sb.AppendLine("| Gate (Accepted B Balanced) | Value |");
        sb.AppendLine("| --- | --- |");
        sb.AppendLine("| ΔRecall@5 (need ≥ +0.10) | " + F(report.Gate.DeltaRecallAt5) + " ("
            + (report.Gate.PassesDeltaRecall ? "pass" : "fail") + ") |");
        sb.AppendLine("| CitationPrecision@5 (need ≥ 0.85) | "
            + F(report.Gate.EmbeddingCitationPrecisionAt5) + " ("
            + (report.Gate.PassesCitationPrecision ? "pass" : "fail") + ") |");
        sb.AppendLine("| Query count | " + report.Gate.QueryCount
            + (report.Gate.Provisional ? " (**provisional** — N &lt; ~40)" : " (not provisional)") + " |");
        sb.AppendLine("| Overall | "
            + (report.Gate.Passes
                ? (report.Gate.Provisional ? "PROVISIONAL PASS" : "PASS")
                : "FAIL")
            + " |");
        sb.AppendLine("| Paid path avoided | " + (report.PaidPathAvoided ? "yes" : "no") + " |");
        sb.AppendLine("| Track A refused | `" + report.TrackADenyReason.Replace('`', '\'') + "` |");
        sb.AppendLine("| Revision invalidation | "
            + (report.RevisionInvalidated ? "yes" : "no")
            + " (`" + report.PreviousSourceHash[..12] + "…` → `"
            + report.NewSourceHash[..12] + "…`) |");
        sb.AppendLine("| Embedded-instruction as data | "
            + (report.EmbeddedInstructionTreatedAsData ? "yes" : "no") + " |");
        sb.AppendLine();
        sb.AppendLine(report.Gate.Notes);
        return sb.ToString();
    }

    private static string F(double v)
        => v.ToString("0.000", CultureInfo.InvariantCulture);
}

/// <summary>
/// Full pilot report payload.
/// </summary>
public sealed record RetrievalPilotReport(
    string FixtureRoot,
    int CorpusCount,
    string TrackADenyReason,
    RetrievalMetricSummary Keyword,
    RetrievalMetricSummary Embedding,
    RetrievalPilotGateResult Gate,
    bool RevisionInvalidated,
    string PreviousSourceHash,
    string NewSourceHash,
    bool EmbeddedInstructionTreatedAsData,
    bool PaidPathAvoided);