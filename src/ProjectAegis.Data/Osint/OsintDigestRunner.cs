using System;
using System.Collections.Generic;
using System.Linq;

namespace ProjectAegis.Data.Osint;

/// <summary>
/// Headless deterministic digest runner for OSINT discoveries (Sprint 19 req 05).
/// Reuses OsintProposalGate for partitioning. Pure (no wall-clock in core path).
/// </summary>
public sealed class OsintDigestRunner
{
    private readonly double _proposalThreshold;

    public OsintDigestRunner(double proposalThreshold = 0.65)
    {
        _proposalThreshold = proposalThreshold;
    }

    /// <summary>
    /// Run digest over discoveries. Returns (proposals above threshold, log-only below).
    /// Input is sorted stably for determinism.
    /// </summary>
    public (OsintDiscoveryRecord[] Proposals, OsintDiscoveryRecord[] LogOnly) Run(IEnumerable<OsintDiscoveryRecord> discoveries)
    {
        if (discoveries == null)
        {
            return (Array.Empty<OsintDiscoveryRecord>(), Array.Empty<OsintDiscoveryRecord>());
        }

        // Stable sort for determinism (match gate: SourceUrl then CanonicalId)
        var sorted = discoveries
            .OrderBy(d => d.SourceUrl, StringComparer.Ordinal)
            .ThenBy(d => d.CanonicalId, StringComparer.Ordinal)
            .ToArray();

        // Delegate to gate, passing our threshold (gate supports minimumConfidence param)
        return OsintProposalGate.Partition(sorted, _proposalThreshold);
    }
}
