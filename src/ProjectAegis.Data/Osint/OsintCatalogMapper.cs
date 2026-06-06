using ProjectAegis.Data.Catalog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ProjectAegis.Data.Osint;

/// <summary>
/// Maps OsintDiscoveryRecord (from digest/connector) to CatalogSensorBinding for propose via write gate.
/// Sprint 19: basic mapping for OSINT-sourced sensors. Values tuned for demo (BasePd from score, etc).
/// </summary>
public static class OsintCatalogMapper
{
    public static CatalogSensorBinding ToSensorBinding(OsintDiscoveryRecord record, string platformId = "osint-platform")
    {
        if (record == null) throw new ArgumentNullException(nameof(record));

        // Derive sensor id from canonical (sanitized)
        var sensorId = "osint-" + record.CanonicalId.Replace(" ", "-").ToLowerInvariant();

        // BasePd from relevance (clamped, example scaling)
        var basePd = Math.Max(0.1, Math.Min(0.95, record.RelevanceScore));

        return new CatalogSensorBinding(
            PlatformId: platformId,
            SensorId: sensorId,
            BasePd: basePd,
            SourceFactId: "osint-discovery",
            Confidence: record.RelevanceScore,
            ImportBatchId: "s19-osint",
            SourceFile: record.SourceUrl,
            ReviewState: CatalogReviewStates.Approved,
            TrlLevel: Math.Max(1, Math.Min(10, record.ProposedTrl)),
            ValueTier: CatalogProvenanceTier.InterpretedValue,
            ReviewerId: "osint-digest",
            CitationRef: record.SourceUrl
        );
    }

    public static IReadOnlyList<CatalogSensorBinding> ToSensorBindings(IEnumerable<OsintDiscoveryRecord> records, string platformId = "osint-platform")
    {
        return records?.Select(r => ToSensorBinding(r, platformId)).ToList() ?? new List<CatalogSensorBinding>();
    }
}
