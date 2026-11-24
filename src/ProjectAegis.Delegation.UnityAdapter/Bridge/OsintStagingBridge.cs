using ProjectAegis.Data.Osint;
using ProjectAegis.Data.WriteGate;

namespace ProjectAegis.Delegation.UnityAdapter.Bridge;

/// <summary>
/// Engine-free OSINT staging row DTO exposed to Unity presentation without leaking <c>ProjectAegis.Data</c> types
/// into <c>ProjectAegis.Unity.Tests</c> (DRG-253 / ADR-006 / ADR-010).
/// </summary>
public sealed record OsintStagingProposalItem(
    string ItemId,
    string BatchId,
    string PlatformId,
    string SensorId,
    double Confidence,
    string CitationUrl,
    string Snippet,
    string SourceFactId,
    string SourceFile,
    int TrlLevel,
    string ReviewState,
    bool IsPendingBatch,
    bool IsLogOnly,
    IReadOnlyList<string> AdditionalCitations);

/// <summary>Partitioned OSINT staging feed (proposals >= threshold vs sub-threshold log-only).</summary>
public sealed record OsintStagingFeed(
    IReadOnlyList<OsintStagingProposalItem> Proposals,
    IReadOnlyList<OsintStagingProposalItem> LogOnly,
    string SourceKind);

/// <summary>Outcome of a write-gate staging action routed via <see cref="OsintStagingBridge"/>.</summary>
public sealed record OsintStagingCommandResult(
    bool Succeeded,
    bool Committed,
    bool Rejected,
    string BatchId,
    IReadOnlyList<string> Errors);

/// <summary>
/// Bridge facade between Unity presentation and <see cref="OsintStagingReviewService"/> (DRG-253).
/// Does not reference <c>Presentation</c> or <c>CommandReview</c>.
/// </summary>
public static class OsintStagingBridge
{
    public const double DefaultConfidenceThreshold = OsintProposalGate.DefaultProposalConfidenceThreshold;

    public static OsintStagingFeed LoadFeed(
        string? databasePath,
        string? fixturePath = null,
        double minimumConfidence = DefaultConfidenceThreshold)
    {
        var snapshot = OsintStagingReviewService.LoadStagingSnapshot(databasePath, fixturePath, minimumConfidence);
        var isPending = string.Equals(snapshot.SourceKind, "staging-db", StringComparison.Ordinal);
        return new OsintStagingFeed(
            snapshot.Proposals.Select(r => ToItem(r, isPending, minimumConfidence)).ToArray(),
            snapshot.LogOnly.Select(r => ToItem(r, isPending, minimumConfidence)).ToArray(),
            snapshot.SourceKind);
    }

    public static OsintStagingCommandResult ProposeFixtureRecord(
        string databasePath,
        string canonicalId,
        string sourceUrl,
        string snippet,
        double relevanceScore,
        string targetDoc,
        int proposedTrl,
        string actorType,
        string actorId,
        bool markApprovedForHumanReview = false,
        long clockTicks = 0)
    {
        var record = new OsintDiscoveryRecord(
            canonicalId,
            sourceUrl,
            snippet,
            relevanceScore,
            targetDoc,
            proposedTrl,
            clockTicks);
        var result = OsintStagingReviewService.ProposeDiscovery(
            databasePath,
            record,
            actorType,
            actorId,
            "osint_facts.json",
            markApprovedForHumanReview,
            new FixedCatalogClock(clockTicks));
        return ToResult(result);
    }

    public static OsintStagingCommandResult ApproveBatch(
        string databasePath,
        string batchId,
        string actorType,
        string actorId,
        long clockTicks = 0)
    {
        var result = OsintStagingReviewService.ApproveBatch(
            databasePath,
            batchId,
            actorType,
            actorId,
            new FixedCatalogClock(clockTicks));
        return ToResult(result);
    }

    public static OsintStagingCommandResult RejectBatch(
        string databasePath,
        string batchId,
        string actorType,
        string actorId,
        string rationale = "",
        long clockTicks = 0)
    {
        var result = OsintStagingReviewService.RejectBatch(
            databasePath,
            batchId,
            actorType,
            actorId,
            rationale,
            new FixedCatalogClock(clockTicks));
        return ToResult(result);
    }

    public static OsintStagingCommandResult ApproveItem(
        string databasePath,
        OsintStagingProposalItem? item,
        string actorType,
        string actorId,
        long clockTicks = 0)
    {
        if (item is null)
        {
            return new OsintStagingCommandResult(false, false, false, string.Empty, new[] { "NO_SELECTION" });
        }

        if (item.IsLogOnly || !OsintStagingReviewService.IsConfidencePermitted(item.Confidence))
        {
            return new OsintStagingCommandResult(
                false,
                false,
                false,
                item.BatchId,
                new[] { OsintStagingReviewService.ReasonBelowConfidenceThreshold });
        }

        if (string.IsNullOrWhiteSpace(item.CitationUrl) && string.IsNullOrWhiteSpace(item.Snippet))
        {
            return new OsintStagingCommandResult(
                false,
                false,
                false,
                item.BatchId,
                new[] { OsintStagingReviewService.ReasonMissingCitation });
        }

        if (item.IsPendingBatch)
        {
            return ApproveBatch(databasePath, item.BatchId, actorType, actorId, clockTicks);
        }

        var canonical = item.SensorId.StartsWith("osint-", StringComparison.Ordinal)
            ? item.SensorId.Substring("osint-".Length)
            : item.ItemId;
        var targetDoc = item.SourceFactId.StartsWith("osint:", StringComparison.Ordinal)
            ? item.SourceFactId.Substring("osint:".Length)
            : "10";
        var record = new OsintDiscoveryRecord(
            canonical,
            item.CitationUrl,
            item.Snippet,
            item.Confidence,
            targetDoc,
            item.TrlLevel,
            clockTicks);
        var result = OsintStagingReviewService.ProposeAndApproveDiscovery(
            databasePath,
            record,
            actorType,
            actorId,
            string.IsNullOrWhiteSpace(item.SourceFile) ? "osint_facts.json" : item.SourceFile,
            new FixedCatalogClock(clockTicks));
        return ToResult(result);
    }

    private static OsintStagingProposalItem ToItem(
        OsintStagingProposalRow row,
        bool isPendingBatch,
        double minimumConfidence)
    {
        var itemId = isPendingBatch ? $"{row.BatchId}:{row.SensorId}" : row.BatchId;
        var isLogOnly = row.IsLogOnly || !OsintStagingReviewService.IsConfidencePermitted(row.Confidence, minimumConfidence);
        return new OsintStagingProposalItem(
            itemId,
            row.BatchId,
            row.PlatformId,
            row.SensorId,
            row.Confidence,
            row.CitationRef,
            row.Snippet,
            row.SourceFactId,
            row.SourceFile,
            row.TrlLevel,
            row.ReviewState,
            isPendingBatch,
            isLogOnly,
            row.AdditionalCitations);
    }

    private static OsintStagingCommandResult ToResult(OsintStagingDecisionResult result) =>
        new(result.Succeeded, result.Committed, result.Rejected, result.BatchId, result.Errors);
}
