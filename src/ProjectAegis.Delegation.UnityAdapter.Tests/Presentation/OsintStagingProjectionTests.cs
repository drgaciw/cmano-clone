using NUnit.Framework;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using ProjectAegis.Delegation.UnityAdapter.Presentation;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

[TestFixture]
public sealed class OsintStagingProjectionTests
{
    [Test]
    public void Project_selected_proposal_exposes_numeric_confidence_and_citation_url_and_snippet()
    {
        var feed = new OsintStagingFeed(
            Proposals: new[]
            {
                new OsintStagingProposalItem(
                    ItemId: "hypersonic-glide-s20",
                    BatchId: "hypersonic-glide-s20",
                    PlatformId: "osint-platform",
                    SensorId: "osint-hypersonic-glide-s20",
                    Confidence: 0.81,
                    CitationUrl: "https://ex.com/hg-s20",
                    Snippet: "observed boost-glide",
                    SourceFactId: "osint:10",
                    SourceFile: "osint_facts.json",
                    TrlLevel: 7,
                    ReviewState: "approved",
                    IsPendingBatch: false,
                    IsLogOnly: false,
                    AdditionalCitations: Array.Empty<string>()),
            },
            LogOnly: Array.Empty<OsintStagingProposalItem>(),
            SourceKind: "fixture");

        var state = OsintStagingProjection.Project(feed, selectedItemId: "hypersonic-glide-s20");

        Assert.That(state.Proposals, Has.Count.EqualTo(1));
        Assert.That(state.SelectedDetail, Is.Not.Null);
        Assert.That(state.SelectedDetail!.Confidence, Is.EqualTo(0.81).Within(0.0001));
        Assert.That(state.SelectedDetail.ConfidenceText, Does.Contain("0.81"));
        Assert.That(state.SelectedDetail.HasCitation, Is.True);
        Assert.That(state.SelectedDetail.CitationUrl, Is.EqualTo("https://ex.com/hg-s20"));
        Assert.That(state.SelectedDetail.Snippet, Is.EqualTo("observed boost-glide"));
        Assert.That(state.SelectedDetail.Citations, Has.Count.GreaterThanOrEqualTo(1));
        Assert.That(state.SelectedDetail.CanApprove, Is.True);
        Assert.That(state.SelectedDetail.CanReject, Is.True);
        Assert.That(state.SelectedDetail.ApproveBlockedReason, Is.Null);
    }

    [Test]
    public void Project_sub_threshold_item_is_log_only_with_approve_disabled()
    {
        var logOnlyItem = new OsintStagingProposalItem(
            ItemId: "low-conf-example",
            BatchId: "low-conf-example",
            PlatformId: "osint-platform",
            SensorId: "osint-low-conf-example",
            Confidence: 0.40,
            CitationUrl: "https://ex.com/low",
            Snippet: "below threshold",
            SourceFactId: "osint:10",
            SourceFile: "osint_facts.json",
            TrlLevel: 4,
            ReviewState: "provisional",
            IsPendingBatch: false,
            IsLogOnly: true,
            AdditionalCitations: Array.Empty<string>());

        var feed = new OsintStagingFeed(
            Proposals: Array.Empty<OsintStagingProposalItem>(),
            LogOnly: new[] { logOnlyItem },
            SourceKind: "fixture");

        var state = OsintStagingProjection.Project(feed, selectedItemId: "low-conf-example");

        Assert.That(state.Proposals, Is.Empty);
        Assert.That(state.LogOnly, Has.Count.EqualTo(1));
        Assert.That(state.SelectedDetail, Is.Not.Null);
        Assert.That(state.SelectedDetail!.IsLogOnly, Is.True);
        Assert.That(state.SelectedDetail.CanApprove, Is.False);
        Assert.That(state.SelectedDetail.ApproveBlockedReason, Is.EqualTo("LOG_ONLY_BELOW_THRESHOLD"));
        Assert.That(state.SelectedDetail.ConfidenceText, Does.Contain("0.40"));
        Assert.That(state.SelectedDetail.CitationUrl, Is.EqualTo("https://ex.com/low"));
    }

    [Test]
    public void Project_missing_citation_disables_approve()
    {
        var uncited = new OsintStagingProposalItem(
            ItemId: "uncited",
            BatchId: "batch-1",
            PlatformId: "osint-platform",
            SensorId: "osint-uncited",
            Confidence: 0.85,
            CitationUrl: string.Empty,
            Snippet: string.Empty,
            SourceFactId: "osint:10",
            SourceFile: "osint_facts.json",
            TrlLevel: 6,
            ReviewState: "approved",
            IsPendingBatch: true,
            IsLogOnly: false,
            AdditionalCitations: Array.Empty<string>());

        var feed = new OsintStagingFeed(
            Proposals: new[] { uncited },
            LogOnly: Array.Empty<OsintStagingProposalItem>(),
            SourceKind: "staging-db");

        var state = OsintStagingProjection.Project(feed, selectedItemId: "uncited");

        Assert.That(state.SelectedDetail, Is.Not.Null);
        Assert.That(state.SelectedDetail!.HasCitation, Is.False);
        Assert.That(state.SelectedDetail.CanApprove, Is.False);
        Assert.That(state.SelectedDetail.ApproveBlockedReason, Is.EqualTo("MISSING_CITATION"));
        Assert.That(state.SelectedDetail.CanReject, Is.True);
    }

    [Test]
    public void Project_formats_proposal_and_log_only_rows_with_numeric_confidence()
    {
        var item = new OsintStagingProposalItem(
            ItemId: "railgun-demo",
            BatchId: "batch-7",
            PlatformId: "osint-platform",
            SensorId: "osint-railgun-demo",
            Confidence: 0.71,
            CitationUrl: "https://ex.com/rg-s20",
            Snippet: "speculative railgun",
            SourceFactId: "osint:10",
            SourceFile: "osint_facts.json",
            TrlLevel: 6,
            ReviewState: "approved",
            IsPendingBatch: true,
            IsLogOnly: false,
            AdditionalCitations: Array.Empty<string>());

        var proposalRow = OsintStagingProjection.FormatProposalRow(item);
        var logOnlyRow = OsintStagingProjection.FormatLogOnlyRow(item with { IsLogOnly = true, Confidence = 0.42 });

        Assert.That(proposalRow, Does.Contain("0.71"));
        Assert.That(proposalRow, Does.Contain("https://ex.com/rg-s20"));
        Assert.That(logOnlyRow, Does.Contain("LOG-ONLY"));
        Assert.That(logOnlyRow, Does.Contain("0.42"));
    }

    [Test]
    public void Project_and_ApproveItem_block_non_finite_confidence_and_non_finite_minimumConfidence()
    {
        foreach (var badConf in new[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity, -0.05, 1.05 })
        {
            var forgedItem = new OsintStagingProposalItem(
                ItemId: "forged-nf",
                BatchId: "batch-nf",
                PlatformId: "osint-platform",
                SensorId: "osint-nf",
                Confidence: badConf,
                CitationUrl: "https://ex.com/nf",
                Snippet: "non-finite test",
                SourceFactId: "osint:10",
                SourceFile: "osint_facts.json",
                TrlLevel: 7,
                ReviewState: "approved",
                IsPendingBatch: true,
                IsLogOnly: false,
                AdditionalCitations: Array.Empty<string>());

            var feed = new OsintStagingFeed(
                Proposals: new[] { forgedItem },
                LogOnly: Array.Empty<OsintStagingProposalItem>(),
                SourceKind: "staging-db");

            var state = OsintStagingProjection.Project(feed, selectedItemId: "forged-nf");
            Assert.That(state.SelectedDetail, Is.Not.Null);
            Assert.That(state.SelectedDetail!.IsLogOnly, Is.True, $"Expected IsLogOnly for confidence {badConf}");
            Assert.That(state.SelectedDetail.CanApprove, Is.False, $"Expected CanApprove=false for confidence {badConf}");
            Assert.That(state.SelectedDetail.ApproveBlockedReason, Is.EqualTo(OsintStagingProjection.ReasonLogOnlyBelowThreshold));

            var approveResult = OsintStagingBridge.ApproveItem(
                "nonexistent.db", forgedItem, "human", "reviewer-1", clockTicks: 703);
            Assert.That(approveResult.Succeeded, Is.False);
            Assert.That(approveResult.Committed, Is.False);
            Assert.That(approveResult.Errors, Does.Contain("BELOW_CONFIDENCE_THRESHOLD"));
        }

        var lowItem = new OsintStagingProposalItem(
            ItemId: "low-040",
            BatchId: "batch-040",
            PlatformId: "osint-platform",
            SensorId: "osint-040",
            Confidence: 0.40,
            CitationUrl: "https://ex.com/040",
            Snippet: "low confidence",
            SourceFactId: "osint:10",
            SourceFile: "osint_facts.json",
            TrlLevel: 6,
            ReviewState: "approved",
            IsPendingBatch: false,
            IsLogOnly: false,
            AdditionalCitations: Array.Empty<string>());
        var lowFeed = new OsintStagingFeed(
            Proposals: new[] { lowItem },
            LogOnly: Array.Empty<OsintStagingProposalItem>(),
            SourceKind: "fixture");

        var nanThresholdState = OsintStagingProjection.Project(
            lowFeed, selectedItemId: "low-040", minimumConfidence: double.NaN);
        Assert.That(nanThresholdState.SelectedDetail, Is.Not.Null);
        Assert.That(nanThresholdState.SelectedDetail!.IsLogOnly, Is.True);
        Assert.That(nanThresholdState.SelectedDetail.CanApprove, Is.False);
        Assert.That(nanThresholdState.SelectedDetail.ApproveBlockedReason, Is.EqualTo(OsintStagingProjection.ReasonLogOnlyBelowThreshold));
    }
}
