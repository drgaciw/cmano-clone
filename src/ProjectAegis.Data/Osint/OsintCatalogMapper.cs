using ProjectAegis.Data.Catalog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ProjectAegis.Data.Osint;

/// <summary>
/// Maps OsintDiscoveryRecord (from digest/connector) to CatalogSensorBinding for propose via write gate.
/// Sprint 19: basic mapping for OSINT-sourced sensors. S22-07: TL routing from proposedTrl/targetDoc.
/// </summary>
public static class OsintCatalogMapper
{
    public const string BranchTagPrefix = "branch:doc-";

    /// <summary>Maps proposal TRL to staged <see cref="CatalogSensorBinding.TrlLevel"/> (1–9).</summary>
    public static int ResolveTrlLevel(int proposedTrl) => Math.Clamp(proposedTrl, 1, 9);

    /// <summary>Maps target doc gate (09 near-future / 10 speculative) to a branch tag on staged rows.</summary>
    public static string ResolveBranchTag(string targetDoc)
    {
        var normalized = NormalizeTargetDoc(targetDoc);
        return BranchTagPrefix + normalized;
    }

    /// <summary>Encodes doc routing metadata for doc 09/10 provenance gates.</summary>
    public static string ResolveSourceFactId(string targetDoc) => $"osint:{NormalizeTargetDoc(targetDoc)}";

    public static CatalogSensorBinding ToSensorBinding(OsintDiscoveryRecord record, string platformId = "osint-platform")
    {
        if (record == null) throw new ArgumentNullException(nameof(record));

        var sensorId = "osint-" + record.CanonicalId.Replace(" ", "-").ToLowerInvariant();
        var basePd = Math.Max(0.1, Math.Min(0.95, record.RelevanceScore));

        return new CatalogSensorBinding(
            PlatformId: platformId,
            SensorId: sensorId,
            BasePd: basePd,
            SourceFactId: ResolveSourceFactId(record.TargetDoc),
            Confidence: record.RelevanceScore,
            ImportBatchId: ResolveBranchTag(record.TargetDoc),
            SourceFile: record.SourceUrl,
            ReviewState: CatalogReviewStates.Provisional,
            TrlLevel: ResolveTrlLevel(record.ProposedTrl),
            ValueTier: CatalogProvenanceTier.InterpretedValue,
            ReviewerId: "osint-digest",
            CitationRef: record.SourceUrl
        );
    }

    public static IReadOnlyList<CatalogSensorBinding> ToSensorBindings(
        IEnumerable<OsintDiscoveryRecord> records,
        string platformId = "osint-platform")
    {
        if (records == null)
        {
            return Array.Empty<CatalogSensorBinding>();
        }

        return records
            .Select(r => ToSensorBinding(r, platformId))
            .OrderBy(b => b.PlatformId, StringComparer.Ordinal)
            .ThenBy(b => b.SensorId, StringComparer.Ordinal)
            .ToArray();
    }

    private static string NormalizeTargetDoc(string targetDoc)
    {
        if (string.IsNullOrWhiteSpace(targetDoc))
        {
            return "10";
        }

        var trimmed = targetDoc.Trim();
        return trimmed switch
        {
            "09" or "9" => "09",
            "10" => "10",
            _ => trimmed
        };
    }
}