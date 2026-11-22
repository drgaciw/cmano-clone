using System.Globalization;
using ProjectAegis.Delegation.UnityAdapter.Bridge;

namespace ProjectAegis.Delegation.UnityAdapter.Presentation;

/// <summary>Detail view-model for a selected OSINT staging proposal or log-only item (DRG-253).</summary>
public sealed record OsintStagingDetailState(
    string ItemId,
    string Title,
    string ConfidenceText,
    double Confidence,
    string TrlAndStateText,
    string CitationUrl,
    string Snippet,
    IReadOnlyList<string> Citations,
    bool HasCitation,
    bool IsLogOnly,
    bool CanApprove,
    bool CanReject,
    string? ApproveBlockedReason);

/// <summary>Read-only panel projection for OSINT staging review (ADR-010 §2–3).</summary>
public sealed record OsintStagingPanelState(
    IReadOnlyList<OsintStagingProposalItem> Proposals,
    IReadOnlyList<OsintStagingProposalItem> LogOnly,
    OsintStagingDetailState? SelectedDetail,
    string StatusSummary,
    string SourceKind,
    string Fingerprint);

/// <summary>Pure projection for OSINT staging citations, confidence, and log-only gating (DRG-253).</summary>
public static class OsintStagingProjection
{
    public const string ReasonLogOnlyBelowThreshold = "LOG_ONLY_BELOW_THRESHOLD";
    public const string ReasonMissingCitation = "MISSING_CITATION";

    public static OsintStagingPanelState Project(
        OsintStagingFeed feed,
        string? selectedItemId = null,
        double minimumConfidence = OsintStagingBridge.DefaultConfidenceThreshold)
    {
        if (feed is null)
        {
            throw new ArgumentNullException(nameof(feed));
        }

        var selected = ResolveSelection(feed, selectedItemId);
        var detail = selected is null ? null : BuildDetail(selected, minimumConfidence);
        var statusSummary = string.Format(
            CultureInfo.InvariantCulture,
            "OSINT Staging: {0} proposal(s), {1} log-only ({2}) | threshold >= {3:F2}",
            feed.Proposals.Count,
            feed.LogOnly.Count,
            feed.SourceKind,
            minimumConfidence);
        var fingerprint = string.Format(
            CultureInfo.InvariantCulture,
            "{0}:{1}:{2}:{3}:{4}:{5}",
            feed.SourceKind,
            feed.Proposals.Count,
            feed.LogOnly.Count,
            selected?.ItemId ?? "none",
            detail?.CanApprove,
            detail?.ApproveBlockedReason ?? "ok");

        return new OsintStagingPanelState(
            feed.Proposals,
            feed.LogOnly,
            detail,
            statusSummary,
            feed.SourceKind,
            fingerprint);
    }

    public static string FormatProposalRow(OsintStagingProposalItem item)
    {
        if (item is null)
        {
            return string.Empty;
        }

        var cite = string.IsNullOrWhiteSpace(item.CitationUrl) ? "(no citation)" : item.CitationUrl;
        return string.Format(
            CultureInfo.InvariantCulture,
            "{0} | conf={1:F2} | TRL-{2} | {3}",
            item.SensorId,
            item.Confidence,
            item.TrlLevel,
            cite);
    }

    public static string FormatLogOnlyRow(OsintStagingProposalItem item)
    {
        if (item is null)
        {
            return string.Empty;
        }

        var cite = string.IsNullOrWhiteSpace(item.CitationUrl) ? "(no citation)" : item.CitationUrl;
        return string.Format(
            CultureInfo.InvariantCulture,
            "[LOG-ONLY] {0} | conf={1:F2} | {2}",
            item.SensorId,
            item.Confidence,
            cite);
    }

    private static OsintStagingProposalItem? ResolveSelection(OsintStagingFeed feed, string? selectedItemId)
    {
        if (!string.IsNullOrWhiteSpace(selectedItemId))
        {
            foreach (var item in feed.Proposals)
            {
                if (string.Equals(item.ItemId, selectedItemId, StringComparison.Ordinal)
                    || string.Equals(item.BatchId, selectedItemId, StringComparison.Ordinal))
                {
                    return item;
                }
            }

            foreach (var item in feed.LogOnly)
            {
                if (string.Equals(item.ItemId, selectedItemId, StringComparison.Ordinal)
                    || string.Equals(item.BatchId, selectedItemId, StringComparison.Ordinal))
                {
                    return item;
                }
            }
        }

        if (feed.Proposals.Count > 0)
        {
            return feed.Proposals[0];
        }

        if (feed.LogOnly.Count > 0)
        {
            return feed.LogOnly[0];
        }

        return null;
    }

    private static OsintStagingDetailState BuildDetail(OsintStagingProposalItem item, double minimumConfidence)
    {
        var citations = new List<string>();
        if (!string.IsNullOrWhiteSpace(item.CitationUrl))
        {
            citations.Add(item.CitationUrl);
        }

        if (item.AdditionalCitations is not null)
        {
            foreach (var extra in item.AdditionalCitations)
            {
                if (!string.IsNullOrWhiteSpace(extra) && !citations.Contains(extra, StringComparer.Ordinal))
                {
                    citations.Add(extra);
                }
            }
        }

        var hasCitation = citations.Count > 0 && !string.IsNullOrWhiteSpace(item.Snippet);
        if (!hasCitation && citations.Count > 0 && !string.IsNullOrWhiteSpace(item.CitationUrl))
        {
            hasCitation = true;
        }

        if (string.IsNullOrWhiteSpace(item.CitationUrl) && string.IsNullOrWhiteSpace(item.Snippet))
        {
            hasCitation = false;
        }

        var isLogOnly = item.IsLogOnly || !IsConfidencePermitted(item.Confidence, minimumConfidence);
        string? blockedReason = null;
        if (isLogOnly)
        {
            blockedReason = ReasonLogOnlyBelowThreshold;
        }
        else if (!hasCitation)
        {
            blockedReason = ReasonMissingCitation;
        }

        var canApprove = blockedReason is null;
        var canReject = !isLogOnly;
        var confidenceText = string.Format(
            CultureInfo.InvariantCulture,
            "CONFIDENCE: {0:F2} (min {1:F2})",
            item.Confidence,
            minimumConfidence);
        var trlStateText = string.Format(
            CultureInfo.InvariantCulture,
            "TRL {0} | {1} | {2}",
            item.TrlLevel,
            item.ReviewState,
            item.SourceFactId);
        var title = isLogOnly
            ? $"LOG-ONLY: {item.SensorId} ({item.BatchId})"
            : $"{item.SensorId} ({item.BatchId})";

        return new OsintStagingDetailState(
            item.ItemId,
            title,
            confidenceText,
            item.Confidence,
            trlStateText,
            item.CitationUrl,
            item.Snippet,
            citations,
            hasCitation,
            isLogOnly,
            canApprove,
            canReject,
            blockedReason);
    }

    private static bool IsConfidencePermitted(double confidence, double minimumConfidence)
    {
        if (double.IsNaN(confidence) || double.IsInfinity(confidence) || confidence < 0.0 || confidence > 1.0)
        {
            return false;
        }

        if (double.IsNaN(minimumConfidence) || double.IsInfinity(minimumConfidence))
        {
            return false;
        }

        var min = Math.Clamp(minimumConfidence, 0.0, 1.0);
        return confidence >= min;
    }
}
